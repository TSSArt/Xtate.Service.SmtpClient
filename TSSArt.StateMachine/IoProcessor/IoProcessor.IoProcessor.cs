using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed partial class IoProcessor : IIoProcessor
	{
		private static readonly Uri InternalTarget = new Uri(uriString: "#_internal", UriKind.Relative);

		private readonly Dictionary<Uri, IEventProcessor> _eventProcessors  = new Dictionary<Uri, IEventProcessor>(UriComparer.Instance);
		private readonly Dictionary<Uri, IServiceFactory> _serviceFactories = new Dictionary<Uri, IServiceFactory>(UriComparer.Instance);
		private          ImmutableArray<IEventProcessor>  _ioProcessors;

		ImmutableArray<IEventProcessor> IIoProcessor.GetIoProcessors() => _ioProcessors;

		async ValueTask IIoProcessor.StartInvoke(string sessionId, InvokeData data, CancellationToken token)
		{
			_context.ValidateSessionId(sessionId, out var service);

			if (!_serviceFactories.TryGetValue(data.Type, out var factory))
			{
				throw new ApplicationException("Invalid type");
			}

			var serviceCommunication = new ServiceCommunication(service, EventProcessorId, data.InvokeId, data.InvokeUniqueId);
			var invokedService = await factory.StartService(data.Source, data.RawContent, data.Content, data.Parameters, serviceCommunication, token).ConfigureAwait(false);

			await _context.AddService(sessionId, data.InvokeId, data.InvokeUniqueId, invokedService).ConfigureAwait(false);

			CompleteAsync();

			async void CompleteAsync()
			{
				try
				{
					var result = await invokedService.Result.ConfigureAwait(false);

					var nameParts = EventName.GetDoneInvokeNameParts(data.InvokeId);
					var @event = new EventObject(EventType.External, nameParts, result, sendId: null, data.InvokeId, data.InvokeUniqueId);
					await service.Send(@event, token: default).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					var @event = new EventObject(EventType.External, EventName.ErrorExecution, DataModelValue.FromException(ex), sendId: null, data.InvokeId, data.InvokeUniqueId);
					await service.Send(@event, token: default).ConfigureAwait(false);
				}
				finally
				{
					invokedService = await _context.TryCompleteService(sessionId, data.InvokeId).ConfigureAwait(false);

					if (invokedService != null)
					{
						await DisposeInvokedService(invokedService).ConfigureAwait(false);
					}
				}
			}
		}

		async ValueTask IIoProcessor.CancelInvoke(string sessionId, string invokeId, CancellationToken token)
		{
			_context.ValidateSessionId(sessionId, out _);

			var service = await _context.TryRemoveService(sessionId, invokeId).ConfigureAwait(false);

			if (service != null)
			{
				await service.Destroy(token).ConfigureAwait(false);

				await DisposeInvokedService(service).ConfigureAwait(false);
			}
		}

		bool IIoProcessor.IsInvokeActive(string sessionId, string invokeId, string invokeUniqueId) =>
				_context.TryGetService(sessionId, invokeId, out var pair) && pair.InvokeUniqueId == invokeUniqueId;

		async ValueTask<SendStatus> IIoProcessor.DispatchEvent(string sessionId, IOutgoingEvent @event, bool skipDelay, CancellationToken token)
		{
			if (@event == null) throw new ArgumentNullException(nameof(@event));

			_context.ValidateSessionId(sessionId, out _);

			var eventProcessor = GetEventProcessor(@event.Type);

			if (eventProcessor == this)
			{
				if (@event.Target == InternalTarget)
				{
					if (@event.DelayMs != 0)
					{
						throw new ApplicationException("Internal events can't be delayed");
					}

					return SendStatus.ToInternalQueue;
				}
			}

			if (!skipDelay && @event.DelayMs != 0)
			{
				return SendStatus.ToSchedule;
			}

			await eventProcessor.Dispatch(sessionId, @event, token).ConfigureAwait(false);

			return SendStatus.Sent;
		}

		ValueTask IIoProcessor.ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token)
		{
			_context.ValidateSessionId(sessionId, out _);

			if (!_context.TryGetService(sessionId, invokeId, out var pair))
			{
				throw new ApplicationException("Invalid InvokeId");
			}

			return pair.Service?.Send(@event, token) ?? default;
		}

		private void IoProcessorInit(ImmutableArray<IEventProcessor> eventProcessors, ImmutableArray<IServiceFactory> serviceFactories)
		{
			var eventProcessorsCount = !eventProcessors.IsDefaultOrEmpty ? eventProcessors.Length + 1 : 1;
			var ioProcessorsBuilder = ImmutableArray.CreateBuilder<IEventProcessor>(eventProcessorsCount);

			ioProcessorsBuilder.Add(this);
			AddEventProcessor(this);

			if (!eventProcessors.IsDefaultOrEmpty)
			{
				foreach (var eventProcessor in eventProcessors)
				{
					ioProcessorsBuilder.Add(eventProcessor);
					AddEventProcessor(eventProcessor);
				}
			}

			_ioProcessors = ioProcessorsBuilder.MoveToImmutable();

			AddServiceFactory(this);

			if (!serviceFactories.IsDefaultOrEmpty)
			{
				foreach (var serviceFactory in serviceFactories)
				{
					AddServiceFactory(serviceFactory);
				}
			}

			foreach (var eventProcessor in _ioProcessors)
			{
				if (eventProcessor != this)
				{
					eventProcessor.RegisterEventConsumer(this);
				}
			}
		}

		private void AddEventProcessor(IEventProcessor eventProcessor)
		{
			_eventProcessors.Add(eventProcessor.Id, eventProcessor);

			var aliasId = eventProcessor.AliasId;
			if (aliasId != null)
			{
				_eventProcessors.Add(aliasId, eventProcessor);
			}
		}

		private void AddServiceFactory(IServiceFactory serviceFactory)
		{
			_serviceFactories.Add(serviceFactory.TypeId, serviceFactory);

			var aliasId = serviceFactory.AliasTypeId;
			if (aliasId != null)
			{
				_serviceFactories.Add(aliasId, serviceFactory);
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

		private IEventProcessor GetEventProcessor(Uri type)
		{
			if (type == null)
			{
				return this;
			}

			if (_eventProcessors.TryGetValue(type, out var eventProcessor))
			{
				return eventProcessor;
			}

			throw new ApplicationException("Invalid Type");
		}
	}
}