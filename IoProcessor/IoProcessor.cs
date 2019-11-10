using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public interface IIoProcessor
	{
		Uri                            GetTarget(string sessionId);
		IReadOnlyList<IEventProcessor> GetIoProcessors();
		ValueTask<SendStatus>          DispatchEvent(string sessionId, IOutgoingEvent @event, CancellationToken token);
		ValueTask                      StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue content, DataModelValue parameters, CancellationToken token);
		ValueTask                      CancelInvoke(string sessionId, string invokeId, CancellationToken token);
		bool                           IsInvokeActive(string sessionId, string invokeId);
		ValueTask                      ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token);
		ValueTask                      Log(string sessionId, string stateMachineName, string label, DataModelValue data, CancellationToken token);
		ValueTask                      Error(string sessionId, ErrorType errorType, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token);
	}

	public class IoProcessor : IEventProcessor, IServiceFactory, IIoProcessor, IDisposable, IAsyncDisposable
	{
		private static readonly Uri BaseUri                   = new Uri("scxml://local/");
		private static readonly Uri EventProcessorId          = new Uri("http://www.w3.org/TR/scxml/#SCXMLEventProcessor");
		private static readonly Uri EventProcessorAliasId     = new Uri(uriString: "scxml", UriKind.Relative);
		private static readonly Uri ServiceFactoryTypeId      = new Uri("http://www.w3.org/TR/scxml/");
		private static readonly Uri ServiceFactoryAliasTypeId = new Uri(uriString: "scxml", UriKind.Relative);
		private static readonly Uri InternalTarget            = new Uri(uriString: "#_internal", UriKind.Relative);

		private readonly IoProcessorContext               _context;
		private readonly Dictionary<Uri, IEventProcessor> _eventProcessors  = new Dictionary<Uri, IEventProcessor>();
		private readonly List<IEventProcessor>            _ioProcessors     = new List<IEventProcessor>();
		private readonly Dictionary<Uri, IServiceFactory> _serviceFactories = new Dictionary<Uri, IServiceFactory>();

		public IoProcessor(in IoProcessorOptions options)
		{
			_ioProcessors.Add(this);
			AddEventProcessor(this);

			if (options.EventProcessors != null)
			{
				foreach (var eventProcessor in options.EventProcessors)
				{
					_ioProcessors.Add(eventProcessor);
					AddEventProcessor(eventProcessor);
				}
			}

			AddServiceFactory(this);

			if (options.ServiceFactories != null)
			{
				foreach (var serviceFactory in options.ServiceFactories)
				{
					AddServiceFactory(serviceFactory);
				}
			}

			_context = options.StorageProvider != null
					? new IoProcessorPersistedContext(this, options)
					: new IoProcessorContext(this, options);
		}

		ValueTask IAsyncDisposable.DisposeAsync() => _context.DisposeAsync();

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		Uri IEventProcessor.Id => EventProcessorId;

		Uri IEventProcessor.AliasId => EventProcessorAliasId;

		private Uri GetTarget(string sessionId) => new Uri(BaseUri, sessionId);

		Uri IIoProcessor.GetTarget(string sessionId) => GetTarget(sessionId);

		Uri IEventProcessor.GetTarget(string sessionId) => GetTarget(sessionId);

		ValueTask IEventProcessor.Dispatch(Uri origin, Uri originType, IOutgoingEvent @event, CancellationToken token)
		{
			var service = _context.GetService(origin, @event.Target);

			var serviceEvent = new EventObject(EventType.External, @event, origin, originType);

			return service.Send(serviceEvent, token);
		}

		Uri IServiceFactory.TypeId => ServiceFactoryTypeId;

		Uri IServiceFactory.AliasTypeId => ServiceFactoryAliasTypeId;

		async ValueTask<IService> IServiceFactory.StartService(Uri source, DataModelValue content, DataModelValue parameters, CancellationToken token)
		{
			var sessionId = IdGenerator.NewSessionId();
			var service = await _context.CreateAndAddStateMachine(sessionId, stateMachine: null, source, content, parameters).ConfigureAwait(false);

			await service.StartAsync(token).ConfigureAwait(false);

			CompleteAsync();

			async void CompleteAsync()
			{
				await service.Result.ConfigureAwait(false);
				await _context.DestroyStateMachine(sessionId).ConfigureAwait(false);
			}

			return service;
		}

		protected virtual void Dispose(bool dispose)
		{
			if (dispose)
			{
				_context.Dispose();
			}
		}

		public ValueTask Initialize() => _context.Initialize();

		public ValueTask<DataModelValue> Execute(IStateMachine stateMachine, DataModelValue parameters = default) => Execute(stateMachine, source: null, content: default, parameters);

		public ValueTask<DataModelValue> Execute(Uri source, DataModelValue parameters = default) => Execute(stateMachine: null, source, content: default, parameters);

		public ValueTask<DataModelValue> Execute(string scxml, DataModelValue parameters = default) => Execute(stateMachine: null, source: null, new DataModelValue(scxml), parameters);

		private async ValueTask<DataModelValue> Execute(IStateMachine stateMachine, Uri source, DataModelValue content, DataModelValue parameters)
		{
			var sessionId = IdGenerator.NewSessionId();
			var service = await _context.CreateAndAddStateMachine(sessionId, stateMachine, source, content, parameters).ConfigureAwait(false);

			try
			{
				return await service.ExecuteAsync().ConfigureAwait(false);
			}
			finally
			{
				await _context.DestroyStateMachine(sessionId).ConfigureAwait(false);
			}
		}

		IReadOnlyList<IEventProcessor> IIoProcessor.GetIoProcessors() => _ioProcessors;

		async ValueTask IIoProcessor.StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue content, DataModelValue parameters, CancellationToken token)
		{
			_context.ValidateSessionId(sessionId, out var service);

			if (!_serviceFactories.TryGetValue(type, out var factory))
			{
				throw new ApplicationException("Invalid type");
			}

			var invokedService = await factory.StartService(source, content, parameters, token).ConfigureAwait(false);

			await _context.AddService(sessionId, invokeId, invokedService).ConfigureAwait(false);

			CompleteAsync();

			async void CompleteAsync()
			{
				var nameParts = EventName.GetDoneInvokeNameParts((Identifier) invokeId);

				try
				{
					var resultData = await invokedService.Result.ConfigureAwait(false);

					await service.Send(new EventObject(EventType.External, nameParts, resultData, sendId: null, invokeId), token: default).ConfigureAwait(false);
				}
				catch
				{
					//TODO:Log exception
					await service.Send(new EventObject(EventType.External, nameParts, data: default, sendId: null, invokeId), token: default).ConfigureAwait(false);
				}
				finally
				{
					invokedService = await _context.TryCompleteService(sessionId, invokeId).ConfigureAwait(false);

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

		private static ValueTask DisposeInvokedService(IService service)
		{
			if (service is IAsyncDisposable asyncDisposable)
			{
				return asyncDisposable.DisposeAsync();
			}

			if (service is IDisposable disposable)
			{
				disposable.Dispose();
			}

			return default;
		}

		bool IIoProcessor.IsInvokeActive(string sessionId, string invokeId) => _context.TryGetService(sessionId, invokeId, out _);

		async ValueTask<SendStatus> IIoProcessor.DispatchEvent(string sessionId, IOutgoingEvent @event, CancellationToken token)
		{
			if (@event == null) throw new ArgumentNullException(nameof(@event));

			_context.ValidateSessionId(sessionId, out _);

			var eventProcessor = GetEventProcessor(@event.Type);
			var origin = eventProcessor.GetTarget(sessionId);

			if (eventProcessor == this)
			{
				if (@event.Target == InternalTarget)
				{
					if (@event.DelayMs != 0)
					{
						throw new ApplicationException("Internal events can be delayed");
					}

					return SendStatus.ToInternalQueue;
				}
			}

			if (@event.DelayMs != 0)
			{
				return SendStatus.ToSchedule;
			}

			await eventProcessor.Dispatch(origin, EventProcessorId, @event, token).ConfigureAwait(false);

			return SendStatus.Sent;
		}

		ValueTask IIoProcessor.ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token)
		{
			_context.ValidateSessionId(sessionId, out _);

			if (!_context.TryGetService(sessionId, invokeId, out var service))
			{
				throw new ApplicationException("Invalid InvokeId");
			}

			return service?.Send(@event, token) ?? default;
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

		ValueTask IIoProcessor.Log(string sessionId, string stateMachineName, string label, DataModelValue data, CancellationToken token)
		{
			FormattableString formattableString = $"Name: [{stateMachineName}]. SessionId: [{sessionId}]. Label: \"{label}\". Data: {data:JSON}";

			Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());

			return default;
		}

		ValueTask IIoProcessor.Error(string sessionId, ErrorType errorType, string stateMachineName, string sourceEntityId, Exception exception, CancellationToken token)
		{
			FormattableString formattableString = $"Type: [{errorType}]. Name: [{stateMachineName}]. SessionId: [{sessionId}]. SourceEntityId: [{sourceEntityId}]. Exception: {exception}";

			Trace.TraceError(formattableString.Format, formattableString.GetArguments());

			return default;
		}
	}
}