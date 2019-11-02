using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	using InvokedServiceDictionary = ConcurrentDictionary<(string SessionId, string InvokeId), IService>;

	public class IoProcessor : IEventProcessor, IServiceFactory
	{
		private static readonly Uri BaseUri                   = new Uri("scxml://local/");
		private static readonly Uri EventProcessorId          = new Uri("http://www.w3.org/TR/scxml/#SCXMLEventProcessor");
		private static readonly Uri EventProcessorAliasId     = new Uri(uriString: "scxml", UriKind.Relative);
		private static readonly Uri ServiceFactoryTypeId      = new Uri("http://www.w3.org/TR/scxml/");
		private static readonly Uri ServiceFactoryAliasTypeId = new Uri(uriString: "scxml", UriKind.Relative);
		private static readonly Uri InternalTarget            = new Uri(uriString: "#_internal", UriKind.Relative);

		private static readonly IIdentifier ErrorIdentifier  = (Identifier) "error";
		private static readonly IIdentifier DoneIdentifier   = (Identifier) "done";
		private static readonly IIdentifier InvokeIdentifier = (Identifier) "invoke";

		private readonly Dictionary<Uri, IEventProcessor>                     _eventProcessors = new Dictionary<Uri, IEventProcessor>();
		private readonly List<IEventProcessor>                                _ioProcessors    = new List<IEventProcessor>();
		private readonly IoProcessorOptions                                   _options;
		private readonly InvokedServiceDictionary                             _serviceByInvokeId = new InvokedServiceDictionary();
		private readonly Dictionary<Uri, IServiceFactory>                     _serviceFactories  = new Dictionary<Uri, IServiceFactory>();
		private readonly ConcurrentDictionary<string, StateMachineController> _stateMachines     = new ConcurrentDictionary<string, StateMachineController>();

		public IoProcessor(in IoProcessorOptions options)
		{
			_options = options;

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
		}

		Uri IEventProcessor.Id => EventProcessorId;

		Uri IEventProcessor.AliasId => EventProcessorAliasId;

		Uri IEventProcessor.GetOrigin(string sessionId) => new Uri(BaseUri, sessionId);

		ValueTask IEventProcessor.Dispatch(Uri origin, Uri originType, IOutgoingEvent @event, CancellationToken token)
		{
			var sessionId = @event.Target.GetLeftPart(UriPartial.Path);

			ValidateSessionId(sessionId, out var service);

			var serviceEvent = new EventObject(EventType.External, @event, origin, originType);

			return service.Send(serviceEvent, token);
		}

		Uri IServiceFactory.TypeId => ServiceFactoryTypeId;

		Uri IServiceFactory.AliasTypeId => ServiceFactoryAliasTypeId;

		async ValueTask<IService> IServiceFactory.StartService(Uri source, DataModelValue arguments, CancellationToken token)
		{
			var sessionId = IdGenerator.NewSessionId();
			FillInterpreterOptions(out var options);
			options.Arguments = arguments;
			var service = new StateMachineController(sessionId, GetStateMachine(source), this, _options.SuspendIdlePeriod, options);
			_stateMachines.TryAdd(sessionId, service);

			await service.StartAsync(token).ConfigureAwait(false);
			var _ = OnCompleteAsync(service);

			return service;
		}

		private static IReadOnlyList<IIdentifier> GetDoneInvokeNameParts(IIdentifier invokeId)  => new ReadOnlyCollection<IIdentifier>(new[] { DoneIdentifier, InvokeIdentifier, invokeId });
		private static IReadOnlyList<IIdentifier> GetErrorInvokeNameParts(IIdentifier invokeId) => new ReadOnlyCollection<IIdentifier>(new[] { ErrorIdentifier, InvokeIdentifier, invokeId });

		public async ValueTask<DataModelValue> Execute(Uri source, DataModelValue arguments = default)
		{
			var sessionId = IdGenerator.NewSessionId();
			FillInterpreterOptions(out var options);
			options.Arguments = arguments;
			var service = new StateMachineController(sessionId, GetStateMachine(source), this, _options.SuspendIdlePeriod, options);
			_stateMachines.TryAdd(sessionId, service);

			try
			{
				return await service.ExecuteAsync().ConfigureAwait(false);
			}
			finally
			{
				_stateMachines.TryRemove(sessionId, out _);
			}
		}

		private async ValueTask OnCompleteAsync(StateMachineController service)
		{
			await service.Result.ConfigureAwait(false);
			_stateMachines.TryRemove(service.SessionId, out _);
		}

		public IReadOnlyList<IEventProcessor> GetIoProcessors() => _ioProcessors;

		public async ValueTask StartInvoke(string sessionId, string invokeId, Uri type, Uri source, DataModelValue data, CancellationToken token)
		{
			ValidateSessionId(sessionId, out var service);

			if (!_serviceFactories.TryGetValue(type, out var factory))
			{
				throw new ApplicationException("Invalid type");
			}

			var invokedService = await factory.StartService(source, data, token).ConfigureAwait(false);

			if (!_serviceByInvokeId.TryAdd((sessionId, invokeId), invokedService))
			{
				throw new ApplicationException("InvokeId already exists");
			}

			InvokeCompleted(service, invokedService, invokeId);
		}

		private static async void InvokeCompleted(StateMachineController parent, IService service, string invokeId)
		{
			try
			{
				var result = await service.Result.ConfigureAwait(false);

				await parent.Send(new EventObject(EventType.External, GetDoneInvokeNameParts((Identifier) invokeId), result), token: default).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				var data = DataModelValue.FromException(ex, isReadOnly: true);
				await parent.Send(new EventObject(EventType.External, GetErrorInvokeNameParts((Identifier) invokeId), data), token: default).ConfigureAwait(false);
			}
		}

		public ValueTask CancelInvoke(string sessionId, string invokeId, CancellationToken token)
		{
			ValidateSessionId(sessionId, out _);

			if (!_serviceByInvokeId.Remove((sessionId, invokeId), out var invokedService))
			{
				throw new ApplicationException("InvokeId does not exist");
			}

			return invokedService.Destroy(token);
		}

		public async ValueTask<SendStatus> DispatchEvent(string sessionId, IOutgoingEvent @event, CancellationToken token)
		{
			ValidateSessionId(sessionId, out _);

			var eventProcessor = GetEventProcessor(@event.Type);
			var origin = eventProcessor.GetOrigin(sessionId);

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

		public ValueTask ForwardEvent(string sessionId, IEvent @event, string invokeId, CancellationToken token)
		{
			ValidateSessionId(sessionId, out _);

			if (!_serviceByInvokeId.TryGetValue((sessionId, invokeId), out var service))
			{
				throw new ApplicationException("Invalid InvokeId");
			}

			return service.Send(@event, token);
		}

		private void FillInterpreterOptions(out InterpreterOptions options)
		{
			options = new InterpreterOptions
					  {
							  PersistenceLevel = _options.PersistenceLevel,
							  ResourceLoader = _options.ResourceLoader,
							  StopToken = _options.StopToken,
							  SuspendToken = _options.SuspendToken
					  };

			if (_options.DataModelHandlerFactories != null)
			{
				options.DataModelHandlerFactories = new List<IDataModelHandlerFactory>(_options.DataModelHandlerFactories);
			}
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

		private IStateMachine GetStateMachine(Uri source) => _options.StateMachineProvider.GetStateMachine(source);

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

		private void ValidateSessionId(string sessionId, out StateMachineController controller)
		{
			if (!_stateMachines.TryGetValue(sessionId, out controller))
			{
				throw new ApplicationException("Invalid SessionId");
			}
		}

		public ValueTask Log(string sessionId, string stateMachineName, string label, DataModelValue data, in CancellationToken token)
		{
			FormattableString formattableString = $"Name: [{stateMachineName}]. SessionId: [{sessionId}]. Label: \"{label}\". Data: {data:JSON}";

			Trace.TraceInformation(formattableString.Format, formattableString.GetArguments());

			return default;
		}

		public ValueTask Error(string sessionId, ErrorType errorType, string stateMachineName, string sourceEntityId, Exception exception, in CancellationToken token)
		{
			FormattableString formattableString = $"Type: [{errorType}]. Name: [{stateMachineName}]. SessionId: [{sessionId}]. SourceEntityId: [{sourceEntityId}]. Exception: {exception}";

			Trace.TraceError(formattableString.Format, formattableString.GetArguments());

			return default;
		}

		public ValueTask<ITransactionalStorage> GetTransactionalStorage(string sessionId, string name, in CancellationToken token) => throw new NotImplementedException();

		public ValueTask RemoveTransactionalStorage(string sessionId, string name, in CancellationToken token) => throw new NotImplementedException();

		public ValueTask RemoveAllTransactionalStorage(string sessionId, in CancellationToken token) => throw new NotImplementedException();
	}
}