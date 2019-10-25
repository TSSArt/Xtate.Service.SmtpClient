using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class StateMachineContext : IStateMachineContext, IExecutionContext
	{
		private readonly string  _stateMachineName;
		private readonly string  _sessionId;
		private readonly ILogger _interpreterLogger;

		private readonly ExternalCommunicationWrapper _externalCommunication;

		public StateMachineContext(string stateMachineName, string sessionId, DataModelValue arguments, ILogger interpreterLogger, ExternalCommunicationWrapper externalCommunication)
		{
			_stateMachineName = stateMachineName;
			_sessionId = sessionId;
			_interpreterLogger = interpreterLogger;
			_externalCommunication = externalCommunication;

			DataModel = CreateDataModel(stateMachineName, sessionId, arguments);
		}

		public bool InState(IIdentifier id)
		{
			var baseId = id.Base<IIdentifier>();
			return Configuration.Some(node => baseId.Equals(node.Id.Base<IIdentifier>()));
		}

		public Task Send(IEvent @event, Uri type, Uri target, int delayMs, CancellationToken token)
		{
			if (EventTarget.IsInternalTarget(target))
			{
				if (type != null)
				{
					throw new ApplicationException("Type should not be mentioned for internal event");
				}

				if (delayMs != 0)
				{
					throw new ApplicationException("Delay can't be used for internal events");
				}

				if (@event.Type != EventType.Internal || @event.InvokeId != null || @event.Origin != null || @event.OriginType != null)
				{
					throw new ApplicationException("Incorrect internal event structure");
				}

				InternalQueue.Enqueue(@event);

				return Task.CompletedTask;
			}

			return _externalCommunication.SendEvent(_sessionId, @event, type, target, delayMs, token);
		}

		public Task Cancel(string sendId, CancellationToken token)
		{
			return _externalCommunication.CancelEvent(_sessionId, sendId, token);
		}

		public Task Log(string label, object arguments, CancellationToken token)
		{
			try
			{
				return _interpreterLogger.Log(_sessionId, _stateMachineName, label, arguments, token);
			}
			catch (Exception ex)
			{
				ex.Data.Add(typeof(ErrorType), ErrorType.Platform1);
				throw;
			}
		}

		public IContextItems RuntimeItems { get; } = new ContextItems();

		public DataModelObject DataModel { get; }

		public OrderedSet<StateEntityNode> Configuration { get; } = new OrderedSet<StateEntityNode>();

		public DataModelObject DataModelHandlerObject { get; } = new DataModelObject(true);

		public IExecutionContext ExecutionContext => this;

		public EntityQueue<IEvent> ExternalBufferedQueue { get; } = new EntityQueue<IEvent>();

		public KeyList<StateEntityNode> HistoryValue { get; } = new KeyList<StateEntityNode>();

		public EntityQueue<IEvent> InternalQueue { get; } = new EntityQueue<IEvent>();

		public DataModelObject InterpreterObject { get; } = new DataModelObject(true);

		public OrderedSet<StateEntityNode> StatesToInvoke { get; } = new OrderedSet<StateEntityNode>();

		public virtual void Dispose() { }

		public virtual ValueTask DisposeAsync() => default;

		public virtual IPersistenceContext PersistenceContext => throw new NotSupportedException();

		private DataModelObject CreateDataModel(string stateMachineName, string sessionId, DataModelValue arguments)
		{
			var platform = new DataModelObject
						   {
								   ["interpreter"] = new DataModelValue(InterpreterObject, isReadOnly: true),
								   ["datamodel"] = new DataModelValue(DataModelHandlerObject, isReadOnly: true),
								   ["args"] = arguments
						   };
			platform.Freeze();

			var ioProcessors = new DataModelObject();
			foreach (var ioProcessor in _externalCommunication.GetIoProcessors(sessionId))
			{
				var ioProcessorObject = new DataModelObject { ["location"] = new DataModelValue(ioProcessor.GetLocation(sessionId).ToString(), isReadOnly: true) };
				ioProcessorObject.Freeze();
				ioProcessors[ioProcessor.Id.ToString()] = new DataModelValue(ioProcessorObject, isReadOnly: true);
			}

			ioProcessors.Freeze();

			return new DataModelObject
				   {
						   ["_name"] = new DataModelValue(stateMachineName, isReadOnly: true),
						   ["_sessionid"] = new DataModelValue(sessionId, isReadOnly: true),
						   ["_event"] = DataModelValue.Undefined(true),
						   ["_ioprocessors"] = new DataModelValue(ioProcessors, isReadOnly: true),
						   ["_x"] = new DataModelValue(platform, isReadOnly: true)
				   };
		}

		private class ContextItems : IContextItems
		{
			private readonly Dictionary<object, object> _items = new Dictionary<object, object>();

			public object this[object key]
			{
				get => _items.TryGetValue(key, out var value) ? value : null;
				set => _items[key] = value;
			}
		}
	}
}