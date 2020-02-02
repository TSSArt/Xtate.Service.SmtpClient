using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class StateMachineContext : IStateMachineContext, IExecutionContext
	{
		private static readonly Uri InternalTarget = new Uri(uriString: "_internal", UriKind.Relative);

		private readonly ExternalCommunicationWrapper _externalCommunication;
		private readonly LoggerWrapper                _logger;
		private readonly string                       _stateMachineName;

		public StateMachineContext(string stateMachineName, string sessionId, DataModelValue arguments, LoggerWrapper logger, ExternalCommunicationWrapper externalCommunication)
		{
			_stateMachineName = stateMachineName;
			_logger = logger;
			_externalCommunication = externalCommunication;

			DataModel = CreateDataModel(stateMachineName, sessionId, arguments);
		}

		public bool InState(IIdentifier id)
		{
			var baseId = id.Base<IIdentifier>();
			return Configuration.Some(node => baseId.Equals(node.Id.Base<IIdentifier>()));
		}

		public async ValueTask Send(IOutgoingEvent @event, CancellationToken token)
		{
			if (IsInternalEvent(@event) || await _externalCommunication.TrySendEvent(@event, token).ConfigureAwait(false) == SendStatus.ToInternalQueue)
			{
				InternalQueue.Enqueue(new EventObject(EventType.Internal, @event));
			}
		}

		public ValueTask Cancel(string sendId, CancellationToken token) => _externalCommunication.CancelEvent(sendId, token);

		public ValueTask Log(string label, DataModelValue arguments, CancellationToken token) => _logger.Log(_stateMachineName, label, arguments, token);

		public ValueTask StartInvoke(InvokeData invokeData, CancellationToken token = default) => _externalCommunication.StartInvoke(invokeData, token);

		public ValueTask CancelInvoke(string invokeId, CancellationToken token) => _externalCommunication.CancelInvoke(invokeId, token);

		public IContextItems RuntimeItems { get; } = new ContextItems();

		public DataModelObject DataModel { get; }

		public OrderedSet<StateEntityNode> Configuration { get; } = new OrderedSet<StateEntityNode>();

		public DataModelObject DataModelHandlerObject { get; } = new DataModelObject(true);

		public IExecutionContext ExecutionContext => this;

		public EntityQueue<IEvent> ExternalBufferedQueue { get; } = new EntityQueue<IEvent>();

		public KeyList<StateEntityNode> HistoryValue { get; } = new KeyList<StateEntityNode>();

		public EntityQueue<IEvent> InternalQueue { get; } = new EntityQueue<IEvent>();

		public DataModelObject InterpreterObject { get; } = new DataModelObject(true);

		public DataModelObject ConfigurationObject { get; } = new DataModelObject(true);

		public OrderedSet<StateEntityNode> StatesToInvoke { get; } = new OrderedSet<StateEntityNode>();

		public virtual void Dispose() { }

		public virtual ValueTask DisposeAsync()
		{
			Dispose();

			return default;
		}

		public virtual IPersistenceContext PersistenceContext => throw new NotSupportedException();

		private static bool IsInternalEvent(IOutgoingEvent @event)
		{
			if (@event.Target != InternalTarget || @event.Type != null)
			{
				return false;
			}

			if (@event.DelayMs != 0)
			{
				throw new ApplicationException("Internal events can't be delayed");
			}

			return true;
		}

		private DataModelObject CreateDataModel(string stateMachineName, string sessionId, DataModelValue arguments)
		{
			var platform = new DataModelObject
						   {
								   ["interpreter"] = new DataModelValue(InterpreterObject),
								   ["datamodel"] = new DataModelValue(DataModelHandlerObject),
								   ["configuration"] = new DataModelValue(ConfigurationObject),
								   ["args"] = arguments
						   };
			platform.Freeze();

			var ioProcessors = new DataModelObject();
			foreach (var ioProcessor in _externalCommunication.GetIoProcessors())
			{
				var ioProcessorObject = new DataModelObject { ["location"] = new DataModelValue(ioProcessor.GetTarget(sessionId).ToString()) };
				ioProcessorObject.Freeze();
				ioProcessors[ioProcessor.Id.ToString()] = new DataModelValue(ioProcessorObject);
			}

			ioProcessors.Freeze();

			var dataModel = new DataModelObject();

			dataModel.SetInternal(property: "_name", new DataModelDescriptor(new DataModelValue(stateMachineName), isReadOnly: true));
			dataModel.SetInternal(property: "_sessionid", new DataModelDescriptor(new DataModelValue(sessionId), isReadOnly: true));
			dataModel.SetInternal(property: "_event", new DataModelDescriptor(value: default, isReadOnly: true));
			dataModel.SetInternal(property: "_ioprocessors", new DataModelDescriptor(new DataModelValue(ioProcessors), isReadOnly: true));
			dataModel.SetInternal(property: "_x", new DataModelDescriptor(new DataModelValue(platform), isReadOnly: true));

			return dataModel;
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