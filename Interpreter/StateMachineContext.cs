using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Xtate
{
	internal class StateMachineContext : IStateMachineContext, IExecutionContext, ILoggerContext
	{
		private static readonly Uri InternalTarget = new Uri(uriString: "_internal", UriKind.Relative);

		private readonly ImmutableDictionary<object, object> _contextRuntimeItems;
		private readonly IDataModelValueProvider             _dataModelValueProvider;
		private readonly IExternalCommunication              _externalCommunication;
		private readonly ILogger                             _logger;
		private readonly SessionId                           _sessionId;
		private readonly string?                             _stateMachineName;
		private          DataModelObject?                    _dataModel;
		private          KeyList<StateEntityNode>?           _historyValue;
		private          IContextItems?                      _runtimeItems;

		public StateMachineContext(string? stateMachineName, SessionId sessionId, IDataModelValueProvider dataModelValueProvider, ILogger logger,
								   IExternalCommunication externalCommunication, ImmutableDictionary<object, object> contextRuntimeItems)
		{
			_stateMachineName = stateMachineName;
			_sessionId = sessionId;
			_dataModelValueProvider = dataModelValueProvider;
			_logger = logger;
			_externalCommunication = externalCommunication;
			_contextRuntimeItems = contextRuntimeItems;
		}

	#region Interface IAsyncDisposable

		public virtual ValueTask DisposeAsync() => default;

	#endregion

	#region Interface IExecutionContext

		public bool InState(IIdentifier id)
		{
			foreach (var state in Configuration)
			{
				if (IdentifierEqualityComparer.Instance.Equals(id, state.Id))
				{
					return true;
				}
			}

			return false;
		}

		public async ValueTask Send(IOutgoingEvent evt, CancellationToken token)
		{
			if (IsInternalEvent(evt) || await _externalCommunication.TrySendEvent(evt, token).ConfigureAwait(false) == SendStatus.ToInternalQueue)
			{
				InternalQueue.Enqueue(new EventObject(EventType.Internal, evt));
			}
		}

		public ValueTask Cancel(SendId sendId, CancellationToken token) => _externalCommunication.CancelEvent(sendId, token);

		public ValueTask Log(string? label, DataModelValue arguments, CancellationToken token) => _logger.ExecuteLog(this, label, arguments, token);

		public ValueTask StartInvoke(InvokeData invokeData, CancellationToken token = default) => _externalCommunication.StartInvoke(invokeData, token);

		public ValueTask CancelInvoke(InvokeId invokeId, CancellationToken token) => _externalCommunication.CancelInvoke(invokeId, token);

		public IContextItems RuntimeItems => _runtimeItems ??= new ContextItems(_contextRuntimeItems);

	#endregion

	#region Interface ILoggerContext

		SessionId? ILoggerContext.SessionId => _sessionId;

		string? ILoggerContext.StateMachineName => _stateMachineName;

	#endregion

	#region Interface IStateMachineContext

		public DataModelObject DataModel => _dataModel ??= CreateDataModel();

		public OrderedSet<StateEntityNode> Configuration { get; } = new OrderedSet<StateEntityNode>();

		public IExecutionContext ExecutionContext => this;

		public KeyList<StateEntityNode> HistoryValue => _historyValue ??= new KeyList<StateEntityNode>();

		public EntityQueue<IEvent> InternalQueue { get; } = new EntityQueue<IEvent>();

		public OrderedSet<StateEntityNode> StatesToInvoke { get; } = new OrderedSet<StateEntityNode>();

		public virtual IPersistenceContext PersistenceContext => throw new NotSupportedException();

	#endregion

		private static bool IsInternalEvent(IOutgoingEvent evt)
		{
			if (evt.Target != InternalTarget || evt.Type != null)
			{
				return false;
			}

			if (evt.DelayMs != 0)
			{
				throw new ExecutionException(Resources.Exception_Internal_events_can_t_be_delayed);
			}

			return true;
		}

		private DataModelObject CreateDataModel()
		{
			var platformLazy = new LazyValue<StateMachineContext>(GetPlatform, this);
			var ioProcessorsLazy = new LazyValue<StateMachineContext>(GetIoProcessors, this);

			var dataModel = new DataModelObject(capacity: 5);

			dataModel.SetInternal(property: @"_name", new DataModelDescriptor(new DataModelValue(_stateMachineName), DataModelAccess.ReadOnly));
			dataModel.SetInternal(property: @"_sessionid", new DataModelDescriptor(new DataModelValue(_sessionId), DataModelAccess.Constant));
			dataModel.SetInternal(property: @"_event", new DataModelDescriptor(value: default, DataModelAccess.ReadOnly));
			dataModel.SetInternal(property: @"_ioprocessors", new DataModelDescriptor(new DataModelValue(ioProcessorsLazy), DataModelAccess.Constant));
			dataModel.SetInternal(property: @"_x", new DataModelDescriptor(new DataModelValue(platformLazy), DataModelAccess.Constant));

			return dataModel;

			static DataModelValue GetPlatform(StateMachineContext context)
			{
				var valueProvider = context._dataModelValueProvider;

				var obj = new DataModelObject(isReadOnly: true, capacity: 5);

				obj.SetInternal(property: @"interpreter", new DataModelDescriptor(valueProvider.Interpreter, DataModelAccess.Constant));
				obj.SetInternal(property: @"datamodel", new DataModelDescriptor(valueProvider.DataModelHandler, DataModelAccess.Constant));
				obj.SetInternal(property: @"configuration", new DataModelDescriptor(valueProvider.Configuration, DataModelAccess.Constant));
				obj.SetInternal(property: @"host", new DataModelDescriptor(valueProvider.Host, DataModelAccess.Constant));
				obj.SetInternal(property: @"args", new DataModelDescriptor(valueProvider.Arguments, DataModelAccess.ReadOnly));

				return new DataModelValue(obj);
			}

			static DataModelValue GetIoProcessors(StateMachineContext context)
			{
				var ioProcessors = context._externalCommunication.GetIoProcessors();

				if (ioProcessors.IsDefaultOrEmpty)
				{
					return new DataModelValue(DataModelObject.Empty);
				}

				var dictionary = new DataModelObject(capacity: ioProcessors.Length);

				foreach (var ioProcessor in ioProcessors)
				{
					var locationLazy = new LazyValue<IIoProcessor, SessionId>(GetLocation, ioProcessor, context._sessionId);

					var entry = new DataModelObject(capacity: 1)
								{
										[@"location"] = new DataModelValue(locationLazy)
								};
					dictionary[ioProcessor.Id.ToString()] = new DataModelValue(entry);
				}

				dictionary.MakeDeepConstant();

				return new DataModelValue(dictionary);
			}

			static DataModelValue GetLocation(IIoProcessor ioProcessor, SessionId sessionId) => new DataModelValue(ioProcessor.GetTarget(sessionId).ToString());
		}

		private class ContextItems : IContextItems
		{
			private readonly Dictionary<object, object>          _items = new Dictionary<object, object>();
			private readonly ImmutableDictionary<object, object> _permanentItems;

			public ContextItems(ImmutableDictionary<object, object> permanentItems) => _permanentItems = permanentItems;

		#region Interface IContextItems

			public object? this[object key]
			{
				get => _permanentItems.TryGetValue(key, out var value) ? value : _items.TryGetValue(key, out value) ? value : null;
				set
				{
					if (value != null && !_permanentItems.ContainsKey(key))
					{
						_items[key] = value;
					}
				}
			}

		#endregion
		}
	}
}