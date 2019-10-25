using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class IoProcessor : IExternalCommunication, IEventProcessor, IServiceFactory, INotifyStateChanged
	{
		private static readonly Uri BaseUri                   = new Uri("scxml://local/");
		private static readonly Uri EventProcessorId          = new Uri("http://www.w3.org/TR/scxml/#SCXMLEventProcessor");
		private static readonly Uri EventProcessorAliasId     = new Uri(uriString: "scxml", UriKind.Relative);
		private static readonly Uri ServiceFactoryTypeId      = new Uri("http://www.w3.org/TR/scxml/");
		private static readonly Uri ServiceFactoryAliasTypeId = new Uri(uriString: "scxml", UriKind.Relative);

		private readonly Dictionary<Uri, IEventProcessor>                  _eventProcessors = new Dictionary<Uri, IEventProcessor>();
		private readonly InterpreterOptions                                _interpreterOptions;
		private readonly List<IEventProcessor>                             _ioProcessors     = new List<IEventProcessor>();
		private readonly Dictionary<Uri, IServiceFactory>                  _serviceFactories = new Dictionary<Uri, IServiceFactory>();
		private readonly IStateMachineProvider                             _stateMachineProvider;
		private readonly ConcurrentDictionary<string, StateMachineService> _stateMachines = new ConcurrentDictionary<string, StateMachineService>();
		private readonly TimeSpan                                          _suspendIdlePeriod;

		public IoProcessor(IoProcessorOptions options)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));

			_ioProcessors.Add(this);

			AddEventProcessor(this);
			AddServiceFactory(this);

			foreach (var eventProcessor in options.EventProcessors)
			{
				_ioProcessors.Add(eventProcessor);
				AddEventProcessor(eventProcessor);
			}

			foreach (var serviceFactory in options.ServiceFactories)
			{
				AddServiceFactory(serviceFactory);
			}

			_stateMachineProvider = options.StateMachineProvider;

			_interpreterOptions = CreateInterpreterOptions(options);

			_suspendIdlePeriod = options.SuspendIdlePeriod;
		}

		Uri IEventProcessor.Id => EventProcessorId;

		Uri IEventProcessor.AliasId => EventProcessorAliasId;

		Uri IEventProcessor.GetLocation(string sessionId) => new Uri(BaseUri, sessionId);

		Task IEventProcessor.Send(IEvent @event, Uri target, CancellationToken token)
		{
			var sessionId = target.GetLeftPart(UriPartial.Path);

			ValidateSessionId(sessionId, out var service);

			return service.Send(@event, token);
		}

		IReadOnlyList<IEventProcessor> IExternalCommunication.GetIoProcessors(string sessionId) => _ioProcessors;

		async Task IExternalCommunication.StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token)
		{
			ValidateSessionId(sessionId, out var service);

			if (!_serviceFactories.TryGetValue(type, out var factory))
			{
				throw new ApplicationException("Invalid type");
			}

			var invokedService = await factory.StartService(service, source, data, token).ConfigureAwait(false);

			service.RegisterService(invokeId, invokedService);
		}

		Task IExternalCommunication.CancelInvoke(string sessionId, string invokeId, CancellationToken token)
		{
			ValidateSessionId(sessionId, out var service);

			var invokedService = service.UnregisterService(invokeId);

			return invokedService.Destroy(token);
		}

		Task IExternalCommunication.SendEvent(string sessionId, IEvent @event, Uri type, Uri target, int delayMs, CancellationToken token)
		{
			ValidateSessionId(sessionId, out var service);

			if (delayMs != 0)
			{
				service.ScheduleEvent(@event, type, target, delayMs);

				return Task.CompletedTask;
			}

			var eventProcessor = GetEventProcessor(type);
			var origin = eventProcessor.GetLocation(sessionId);
			var sendEvent = new EventObject(EventType.External, @event.SendId, @event.ToString(), invokeId: null, origin, type, @event.Data);

			return eventProcessor.Send(sendEvent, target, token);
		}

		Task IExternalCommunication.ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token)
		{
			ValidateSessionId(sessionId, out var service);

			return service.ForwardEvent(invokeId, @event, token);
		}

		Task IExternalCommunication.CancelEvent(string sessionId, string sendId, CancellationToken token)
		{
			ValidateSessionId(sessionId, out var service);

			service.CancelEvent(sendId);

			return Task.CompletedTask;
		}

		Task IExternalCommunication.ReturnDoneEvent(string sessionId, DataModelValue doneData, CancellationToken token)
		{
			ValidateSessionId(sessionId, out var service);

			return service.ReturnDoneEvent(doneData, token);
		}

		Task INotifyStateChanged.OnChanged(string sessionId, StateMachineInterpreterState state)
		{
			ValidateSessionId(sessionId, out var service);

			service.OnStateChanged(state);

			return Task.CompletedTask;
		}

		Uri IServiceFactory.TypeId => ServiceFactoryTypeId;

		Uri IServiceFactory.AliasTypeId => ServiceFactoryAliasTypeId;

		Task<IService> IServiceFactory.StartService(IService parentService, Uri source, DataModelValue data, CancellationToken token) => StartStateMachine(parentService, source, data, token);

		private InterpreterOptions CreateInterpreterOptions(IoProcessorOptions options)
		{
			var interpreterOptions = new InterpreterOptions
									 {
											 ExternalCommunication = this,
											 NotifyStateChanged = this,
											 Logger = options.Logger,
											 PersistenceLevel = options.PersistenceLevel,
											 StorageProvider = options.StorageProvider,
											 ResourceLoader = options.ResourceLoader,
											 SuspendToken = options.SuspendToken,
											 StopToken = options.StopToken
									 };

			foreach (var factory in options.DataModelHandlerFactories)
			{
				interpreterOptions.DataModelHandlerFactories.Add(factory);
			}

			return interpreterOptions;
		}

		private async Task<IService> StartStateMachine(IService parentService, Uri source, DataModelValue data, CancellationToken token)
		{
			var sessionId = IdGenerator.NewSessionId();
			var service = new StateMachineService(parentService, sessionId, GetStateMachine(source), _interpreterOptions, data, this, OnStateMachineComplete, _suspendIdlePeriod);
			_stateMachines.TryAdd(sessionId, service);

			await service.StartAsync(token);

			return service;
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

		private void OnStateMachineComplete(StateMachineService service)
		{
			_stateMachines.TryRemove(service.SessionId, out _);
		}

		private IStateMachine GetStateMachine(Uri source) => _stateMachineProvider.GetStateMachine(source);

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

		private void ValidateSessionId(string sessionId, out StateMachineService service)
		{
			if (!_stateMachines.TryGetValue(sessionId, out service))
			{
				throw new ApplicationException("Invalid SessionId");
			}
		}

		public Task Start(Uri source, CancellationToken token = default) => StartStateMachine(parentService: null, source, data: default, token);
	}
}