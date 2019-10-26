using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class StateMachinePersistedContext : StateMachineContext, IPersistenceContext
	{
		private readonly OrderedSetPersistingController<StateEntityNode> _configurationController;
		private readonly DataModelObjectPersistingController             _dataModelObjectPersistingController;
		private readonly DataModelReferenceTracker                       _dataModelReferenceTracker;
		private readonly EntityQueuePersistingController<IEvent>         _externalBufferedQueuePersistingController;
		private readonly KeyListPersistingController<StateEntityNode>    _historyValuePersistingController;
		private readonly EntityQueuePersistingController<IEvent>         _internalQueuePersistingController;
		private readonly Bucket                                          _state;
		private readonly OrderedSetPersistingController<StateEntityNode> _statesToInvokeController;
		private readonly ITransactionalStorage                           _storage;

		public StateMachinePersistedContext(string stateMachineName, string sessionId, DataModelValue arguments, ITransactionalStorage storage,
											Dictionary<int, IEntity> entityMap, ILogger interpreterLogger, ExternalCommunicationWrapper externalCommunication)
				: base(stateMachineName, sessionId, arguments, interpreterLogger, externalCommunication)
		{
			_storage = storage;
			var bucket = new Bucket(storage);

			_configurationController = new OrderedSetPersistingController<StateEntityNode>(bucket.Nested(StorageSection.Configuration), Configuration, entityMap);
			_statesToInvokeController = new OrderedSetPersistingController<StateEntityNode>(bucket.Nested(StorageSection.StatesToInvoke), StatesToInvoke, entityMap);
			_dataModelReferenceTracker = new DataModelReferenceTracker(bucket.Nested(StorageSection.DataModelReferences));
			_dataModelObjectPersistingController = new DataModelObjectPersistingController(bucket.Nested(StorageSection.DataModel), _dataModelReferenceTracker, DataModel);
			_historyValuePersistingController = new KeyListPersistingController<StateEntityNode>(bucket.Nested(StorageSection.HistoryValue), HistoryValue, entityMap);
			_internalQueuePersistingController = new EntityQueuePersistingController<IEvent>(bucket.Nested(StorageSection.InternalQueue), InternalQueue, EventCreator);
			_externalBufferedQueuePersistingController = new EntityQueuePersistingController<IEvent>(bucket.Nested(StorageSection.ExternalBufferedQueue), ExternalBufferedQueue, EventCreator);
			_state = bucket.Nested(StorageSection.StateBag);
		}

		public override IPersistenceContext PersistenceContext => this;

		public void ClearState(int key) => _state.RemoveSubtree(key);

		public int GetState(int key) => _state.TryGet(key, out int value) ? value : 0;

		public int GetState(int key, int subKey) => _state.Nested(key).TryGet(subKey, out int value) ? value : 0;

		public void SetState(int key, int value) => _state.Add(key, value);

		public void SetState(int key, int subKey, int value) => _state.Nested(key).Add(subKey, value);

		public ValueTask CheckPoint(int level, CancellationToken token) => _storage.CheckPoint(level, token);

		public ValueTask Shrink(CancellationToken token) => _storage.Shrink(token);

		private static IEvent EventCreator(Bucket bucket) => new EventObject(bucket);

		private void DisposeControllers()
		{
			_externalBufferedQueuePersistingController.Dispose();
			_internalQueuePersistingController.Dispose();
			_historyValuePersistingController.Dispose();
			_dataModelObjectPersistingController.Dispose();
			_dataModelReferenceTracker.Dispose();
			_statesToInvokeController.Dispose();
			_configurationController.Dispose();
		}

		public override void Dispose()
		{
			_storage.Dispose();

			DisposeControllers();

			base.Dispose();
		}

		public override async ValueTask DisposeAsync()
		{
			await _storage.DisposeAsync();

			DisposeControllers();

			await base.DisposeAsync();
		}

		private enum StorageSection
		{
			Configuration,
			StatesToInvoke,
			DataModel,
			DataModelReferences,
			ExternalBufferedQueue,
			InternalQueue,
			HistoryValue,
			StateBag
		}
	}
}