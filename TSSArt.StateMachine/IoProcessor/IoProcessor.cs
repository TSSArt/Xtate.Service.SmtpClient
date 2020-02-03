using System;
using System.Collections./**/Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed class IoProcessor : IEventProcessor, IServiceFactory, IIoProcessor, IEventConsumer, IDisposable, IAsyncDisposable
	{
		private static readonly Uri BaseUri                   = new Uri("ioprocessor://./");
		private static readonly Uri EventProcessorId          = new Uri("http://www.w3.org/TR/scxml/#SCXMLEventProcessor");
		private static readonly Uri EventProcessorAliasId     = new Uri(uriString: "scxml", UriKind.Relative);
		private static readonly Uri ServiceFactoryTypeId      = new Uri("http://www.w3.org/TR/scxml/");
		private static readonly Uri ServiceFactoryAliasTypeId = new Uri(uriString: "scxml", UriKind.Relative);
		private static readonly Uri InternalTarget            = new Uri(uriString: "#_internal", UriKind.Relative);

		private readonly IoProcessorContext               _context;
		private readonly Dictionary<Uri, IEventProcessor> _eventProcessors  = new Dictionary<Uri, IEventProcessor>(UriComparer.Instance);
		private readonly List<IEventProcessor>            _ioProcessors     = new List<IEventProcessor>();
		private readonly Dictionary<Uri, IServiceFactory> _serviceFactories = new Dictionary<Uri, IServiceFactory>(UriComparer.Instance);

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

			if (options.EventProcessors != null)
			{
				foreach (var eventProcessor in options.EventProcessors)
				{
					eventProcessor.RegisterEventConsumer(this);
				}
			}
		}

		ValueTask IAsyncDisposable.DisposeAsync() => _context.DisposeAsync();

		public void Dispose() => _context.Dispose();

		ValueTask IEventConsumer.Dispatch(string sessionId, IEvent @event, CancellationToken token)
		{
			_context.ValidateSessionId(sessionId, out var controller);

			return controller.Send(@event, token);
		}

		Uri IEventProcessor.Id => EventProcessorId;

		Uri IEventProcessor.AliasId => EventProcessorAliasId;

		Uri IEventProcessor.GetTarget(string sessionId) => GetTarget(sessionId);

		void IEventProcessor.RegisterEventConsumer(IEventConsumer eventConsumer)
		{
			if (eventConsumer != this)
			{
				throw new InvalidOperationException();
			}
		}

		ValueTask IEventProcessor.Dispatch(string sessionId, IOutgoingEvent @event, CancellationToken token)
		{
			var service = _context.GetService(sessionId, @event.Target);

			var serviceEvent = new EventObject(EventType.External, @event, GetTarget(sessionId), EventProcessorId);

			return service.Send(serviceEvent, token);
		}

		/**/ImmutableArray<IEventProcessor> IIoProcessor.GetIoProcessors() => _ioProcessors;

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

		Uri IServiceFactory.TypeId => ServiceFactoryTypeId;

		Uri IServiceFactory.AliasTypeId => ServiceFactoryAliasTypeId;

		async ValueTask<IService> IServiceFactory.StartService(Uri source, string rawContent, DataModelValue content, DataModelValue parameters, IServiceCommunication serviceCommunication,
															   CancellationToken token)
		{
			var sessionId = IdGenerator.NewSessionId();
			var scxml = rawContent ?? content.AsStringOrDefault();
			var service = await _context.CreateAndAddStateMachine(sessionId, stateMachine: null, source, scxml, parameters).ConfigureAwait(false);

			await service.StartAsync(token).ConfigureAwait(false);

			CompleteAsync();

			async void CompleteAsync()
			{
				await service.Result.ConfigureAwait(false);
				await _context.DestroyStateMachine(sessionId).ConfigureAwait(false);
			}

			return service;
		}

		private Uri GetTarget(string sessionId) => new Uri(BaseUri, sessionId);

		public ValueTask<DataModelValue> Execute(IStateMachine stateMachine, DataModelValue parameters = default) =>
				Execute(stateMachine, source: null, scxml: default, IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> Execute(Uri source, DataModelValue parameters = default) => Execute(stateMachine: null, source, scxml: default, IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> Execute(string scxml, DataModelValue parameters = default) => Execute(stateMachine: null, source: null, scxml, IdGenerator.NewSessionId(), parameters);

		public ValueTask<DataModelValue> Execute(string sessionId, IStateMachine stateMachine, DataModelValue parameters = default) =>
				Execute(stateMachine, source: null, scxml: default, sessionId, parameters);

		public ValueTask<DataModelValue> Execute(string sessionId, Uri source, DataModelValue parameters = default) => Execute(stateMachine: null, source, scxml: default, sessionId, parameters);

		public ValueTask<DataModelValue> Execute(string sessionId, string scxml, DataModelValue parameters = default) => Execute(stateMachine: null, source: null, scxml, sessionId, parameters);

		private async ValueTask<DataModelValue> Execute(IStateMachine stateMachine, Uri source, string scxml, string sessionId, DataModelValue parameters)
		{
			if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));

			var service = await _context.CreateAndAddStateMachine(sessionId, stateMachine, source, scxml, parameters).ConfigureAwait(false);

			try
			{
				return await service.ExecuteAsync().ConfigureAwait(false);
			}
			finally
			{
				await _context.DestroyStateMachine(sessionId).ConfigureAwait(false);
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
	}
}