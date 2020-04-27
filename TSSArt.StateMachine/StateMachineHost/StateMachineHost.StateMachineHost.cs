using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class StateMachineHost : IStateMachineHost
	{
		private static readonly Uri                             InternalTarget = new Uri(uriString: "#_internal", UriKind.Relative);
		private                 ImmutableArray<IEventProcessor> _eventProcessors;

		private ImmutableArray<IServiceFactory> _serviceFactories;

	#region Interface IStateMachineHost

		ImmutableArray<IEventProcessor> IStateMachineHost.GetIoProcessors() => _eventProcessors;

		async ValueTask IStateMachineHost.StartInvoke(string sessionId, InvokeData data, CancellationToken token)
		{
			var context = GetCurrentContext();

			context.ValidateSessionId(sessionId, out var service);

			var factory = FindServiceFactory(data.Type, data.Source);
			var serviceCommunication = new ServiceCommunication(service, EventProcessorId, data.InvokeId, data.InvokeUniqueId);
			var invokedService = await factory.StartService(service.Location, data, serviceCommunication, token).ConfigureAwait(false);

			await context.AddService(sessionId, data.InvokeId, data.InvokeUniqueId, invokedService, token).ConfigureAwait(false);

			CompleteAsync().Forget();

			async ValueTask CompleteAsync()
			{
				try
				{
					var result = await invokedService.Result.ConfigureAwait(false);

					var nameParts = EventName.GetDoneInvokeNameParts(data.InvokeId);
					var evt = new EventObject(EventType.External, nameParts, result, sendId: null, data.InvokeId, data.InvokeUniqueId);
					await service.Send(evt, token: default).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					var evt = new EventObject(EventType.External, EventName.ErrorExecution, DataConverter.FromException(ex), sendId: null, data.InvokeId, data.InvokeUniqueId);
					await service.Send(evt, token: default).ConfigureAwait(false);
				}
				finally
				{
					var invokedService2 = await context.TryCompleteService(sessionId, data.InvokeId).ConfigureAwait(false);

					if (invokedService2 != null)
					{
						await DisposeInvokedService(invokedService2).ConfigureAwait(false);
					}
				}
			}
		}

		async ValueTask IStateMachineHost.CancelInvoke(string sessionId, string invokeId, CancellationToken token)
		{
			var context = GetCurrentContext();

			context.ValidateSessionId(sessionId, out _);

			var service = await context.TryRemoveService(sessionId, invokeId).ConfigureAwait(false);

			if (service != null)
			{
				await service.Destroy(token).ConfigureAwait(false);

				await DisposeInvokedService(service).ConfigureAwait(false);
			}
		}

		bool IStateMachineHost.IsInvokeActive(string sessionId, string invokeId, string invokeUniqueId) =>
				IsCurrentContextExists(out var context) && context.TryGetService(sessionId, invokeId, out var pair) && pair.InvokeUniqueId == invokeUniqueId;

		async ValueTask<SendStatus> IStateMachineHost.DispatchEvent(string sessionId, IOutgoingEvent evt, bool skipDelay, CancellationToken token)
		{
			if (evt == null) throw new ArgumentNullException(nameof(evt));

			var context = GetCurrentContext();

			context.ValidateSessionId(sessionId, out _);

			var eventProcessor = GetEventProcessor(evt.Type, evt.Target);

			if (eventProcessor == this)
			{
				if (evt.Target == InternalTarget)
				{
					if (evt.DelayMs != 0)
					{
						throw new StateMachineProcessorException(Resources.Exception_Internal_events_can_t_be_delayed_);
					}

					return SendStatus.ToInternalQueue;
				}
			}

			if (!skipDelay && evt.DelayMs != 0)
			{
				return SendStatus.ToSchedule;
			}

			await eventProcessor.Dispatch(sessionId, evt, token).ConfigureAwait(false);

			return SendStatus.Sent;
		}

		ValueTask IStateMachineHost.ForwardEvent(string sessionId, IEvent evt, string invokeId, CancellationToken token)
		{
			var context = GetCurrentContext();

			context.ValidateSessionId(sessionId, out _);

			if (!context.TryGetService(sessionId, invokeId, out var pair))
			{
				throw new StateMachineProcessorException(Resources.Exception_Invalid_InvokeId);
			}

			return pair.Service?.Send(evt, token) ?? default;
		}

	#endregion

		private bool IsCurrentContextExists([NotNullWhen(true)] out StateMachineHostContext? context)
		{
			context = _context;

			return context != null;
		}

		private IErrorProcessor CreateErrorProcessor(string sessionId, IStateMachine? stateMachine, Uri? source, string? scxml) =>
				_options.VerboseValidation ? new DetailedErrorProcessor(sessionId, stateMachine, source, scxml) : DefaultErrorProcessor.Instance;

		private StateMachineHostContext GetCurrentContext() => _context ?? throw new InvalidOperationException(Resources.Exception_IO_Processor_has_not_been_started);

		private IServiceFactory FindServiceFactory(Uri type, Uri? source)
		{
			if (!_serviceFactories.IsDefaultOrEmpty)
			{
				foreach (var serviceFactory in _serviceFactories)
				{
					if (serviceFactory.CanHandle(type, source))
					{
						return serviceFactory;
					}
				}
			}

			throw new StateMachineProcessorException(Resources.Exception_Invalid_type);
		}

		private void StateMachineHostInit()
		{
			var factories = _options.ServiceFactories;
			var length = !factories.IsDefault ? factories.Length + 1 : 1;
			var serviceFactories = ImmutableArray.CreateBuilder<IServiceFactory>(length);

			serviceFactories.Add(this);

			if (!factories.IsDefaultOrEmpty)
			{
				foreach (var serviceFactory in factories)
				{
					serviceFactories.Add(serviceFactory);
				}
			}

			_serviceFactories = serviceFactories.MoveToImmutable();
		}

		private async ValueTask StateMachineHostStartAsync(CancellationToken token)
		{
			var factories = _options.EventProcessorFactories;
			var length = !factories.IsDefault ? factories.Length + 1 : 1;

			var eventProcessors = ImmutableArray.CreateBuilder<IEventProcessor>(length);

			eventProcessors.Add(this);

			if (!_options.EventProcessorFactories.IsDefaultOrEmpty)
			{
				foreach (var eventProcessorFactory in _options.EventProcessorFactories)
				{
					eventProcessors.Add(await eventProcessorFactory.Create(this, token).ConfigureAwait(false));
				}
			}

			_eventProcessors = eventProcessors.MoveToImmutable();
		}

		private async ValueTask StateMachineHostStopAsync()
		{
			var eventProcessors = _eventProcessors;
			_eventProcessors = default;

			if (eventProcessors.IsDefaultOrEmpty)
			{
				return;
			}

			foreach (var eventProcessor in eventProcessors)
			{
				if (eventProcessor == this)
				{
					continue;
				}

				if (eventProcessor is IAsyncDisposable asyncDisposable)
				{
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				}

				// ReSharper disable once SuspiciousTypeConversion.Global
				else if (eventProcessor is IDisposable disposable)
				{
					disposable.Dispose();
				}
			}
		}

		private static ValueTask DisposeInvokedService(IService service)
		{
			if (service is IAsyncDisposable asyncDisposable)
			{
				return asyncDisposable.DisposeAsync();
			}

			// ReSharper disable once SuspiciousTypeConversion.Global
			if (service is IDisposable disposable)
			{
				disposable.Dispose();
			}

			return default;
		}

		private IEventProcessor GetEventProcessor(Uri? type, Uri? target)
		{
			if (_eventProcessors == null)
			{
				throw new StateMachineProcessorException(Resources.Exception_StateMachineHost_stopped);
			}

			foreach (var eventProcessor in _eventProcessors)
			{
				if (eventProcessor.CanHandle(type, target))
				{
					return eventProcessor;
				}
			}

			throw new StateMachineProcessorException(Resources.Exception_Invalid_type);
		}
	}
}