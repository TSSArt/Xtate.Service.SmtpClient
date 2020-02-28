using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal sealed class IoProcessorPersistedContext : IoProcessorContext
	{
		private const string IoProcessorPartition = "IoProcessor";
		private const string ContextKey           = "context";
		private const int    StateMachinesKey     = 0;
		private const int    InvokedServicesKey   = 1;

		private readonly Dictionary<(string SessionId, string InvokeId), InvokedServiceMeta> _invokedServices = new Dictionary<(string SessionId, string InvokeId), InvokedServiceMeta>();

		private readonly Dictionary<string, StateMachineMeta> _stateMachines       = new Dictionary<string, StateMachineMeta>();
		private readonly SemaphoreSlim                        _lockInvokedServices = new SemaphoreSlim(initialCount: 1, maxCount: 1);
		private readonly SemaphoreSlim                        _lockStateMachines   = new SemaphoreSlim(initialCount: 1, maxCount: 1);
		private readonly TimeSpan                             _idlePeriod;
		private readonly IIoProcessor                         _ioProcessor;
		private readonly IStorageProvider                     _storageProvider;

		private ITransactionalStorage _storage;
		private int                   _invokedServiceRecordId;
		private int                   _stateMachineRecordId;
		private bool                  _disposed;

		public IoProcessorPersistedContext(IIoProcessor ioProcessor, in IoProcessorOptions options) 
				: base(ioProcessor, options)
		{
			_ioProcessor = ioProcessor;
			_storageProvider = options.StorageProvider ?? throw new ArgumentNullException(nameof(options.StorageProvider));
			_idlePeriod = options.SuspendIdlePeriod;
		}

		public override async ValueTask InitializeAsync(CancellationToken token)
		{
			_storage = await _storageProvider.GetTransactionalStorage(IoProcessorPartition, ContextKey, token).ConfigureAwait(false);

			await LoadStateMachines(token).ConfigureAwait(false);
			await LoadInvokedServices(token).ConfigureAwait(false);

			await base.InitializeAsync(token).ConfigureAwait(false);
		}

		public override async ValueTask DisposeAsync()
		{
			if (_disposed)
			{
				return;
			}

			await _storage.DisposeAsync().ConfigureAwait(false);

			_lockInvokedServices.Dispose();
			_lockStateMachines.Dispose();

			_disposed = true;

			await base.DisposeAsync().ConfigureAwait(false);
		}

		public override async ValueTask AddService(string sessionId, string invokeId, string invokeUniqueId, IService service)
		{
			await _lockInvokedServices.WaitAsync(StopToken).ConfigureAwait(false);
			try
			{
				await base.AddService(sessionId, invokeId, invokeUniqueId, service).ConfigureAwait(false);

				var bucket = new Bucket(_storage).Nested(InvokedServicesKey);
				var recordId = _invokedServiceRecordId ++;

				var invokedSessionId = service is StateMachineController stateMachineController ? stateMachineController.SessionId : null;
				var invokedService = new InvokedServiceMeta(sessionId, invokeId, invokeUniqueId, invokedSessionId) { RecordId = recordId };
				_invokedServices.Add((sessionId, invokeId), invokedService);

				bucket.Add(Bucket.RootKey, _invokedServiceRecordId);

				invokedService.Store(bucket.Nested(recordId));

				await _storage.CheckPoint(level: 0, StopToken).ConfigureAwait(false);
			}
			finally
			{
				_lockInvokedServices.Release();
			}
		}

		private async ValueTask RemoveInvokedService(string sessionId, string invokeId)
		{
			if (!_invokedServices.Remove((sessionId, invokeId)))
			{
				return;
			}

			var bucket = new Bucket(_storage).Nested(InvokedServicesKey);
			if (bucket.TryGet(sessionId, out int recordId))
			{
				bucket.RemoveSubtree(recordId);

				await _storage.CheckPoint(level: 0, StopToken).ConfigureAwait(false);
			}

			await ShrinkInvokedServices(StopToken).ConfigureAwait(false);
		}

		public override async ValueTask<IService> TryRemoveService(string sessionId, string invokeId)
		{
			await _lockInvokedServices.WaitAsync(StopToken).ConfigureAwait(false);
			try
			{
				await RemoveInvokedService(sessionId, invokeId).ConfigureAwait(false);

				return await base.TryRemoveService(sessionId, invokeId).ConfigureAwait(false);
			}
			finally
			{
				_lockInvokedServices.Release();
			}
		}

		public override async ValueTask<IService> TryCompleteService(string sessionId, string invokeId)
		{
			await _lockInvokedServices.WaitAsync(StopToken).ConfigureAwait(false);
			try
			{
				await RemoveInvokedService(sessionId, invokeId).ConfigureAwait(false);

				return await base.TryCompleteService(sessionId, invokeId).ConfigureAwait(false);
			}
			finally
			{
				_lockInvokedServices.Release();
			}
		}

		protected override StateMachineController CreateStateMachineController(string sessionId, IStateMachineOptions options, IStateMachine stateMachine, in InterpreterOptions defaultOptions)
		{
			return options.IsStateMachinePersistable()
					? new StateMachinePersistedController(sessionId, options, stateMachine, _ioProcessor, _storageProvider, _idlePeriod, in defaultOptions)
					: base.CreateStateMachineController(sessionId, options, stateMachine, in defaultOptions);
		}

		public override async ValueTask<StateMachineController> CreateAndAddStateMachine(string sessionId, IStateMachineOptions options, IStateMachine stateMachine, Uri source,
																						 string scxml, DataModelValue parameters, CancellationToken token)
		{
			stateMachine = await GetStateMachine(stateMachine, source, scxml, token);

			if (options == null)
			{
				stateMachine.Is(out options);
			}

			if (!options.IsStateMachinePersistable())
			{
				return await base.CreateAndAddStateMachine(sessionId, options, stateMachine, source: null, scxml: null, parameters, token).ConfigureAwait(false);
			}

			await _lockStateMachines.WaitAsync(StopToken).ConfigureAwait(false);
			try
			{
				var stateMachineController = await base.CreateAndAddStateMachine(sessionId, options, stateMachine, source: null, scxml: null, parameters, token).ConfigureAwait(false);

				var bucket = new Bucket(_storage).Nested(StateMachinesKey);
				var recordId = _stateMachineRecordId ++;

				var stateMachineMeta = new StateMachineMeta(sessionId, options) { RecordId = recordId, Controller = stateMachineController };
				_stateMachines.Add(sessionId, stateMachineMeta);

				bucket.Add(Bucket.RootKey, _stateMachineRecordId);

				stateMachineMeta.Store(bucket.Nested(recordId));

				await _storage.CheckPoint(level: 0, StopToken).ConfigureAwait(false);

				return stateMachineController;
			}
			finally
			{
				_lockStateMachines.Release();
			}
		}

		public override async ValueTask DestroyStateMachine(string sessionId)
		{
			await _lockStateMachines.WaitAsync(StopToken).ConfigureAwait(false);
			try
			{
				_stateMachines.Remove(sessionId);

				var bucket = new Bucket(_storage).Nested(StateMachinesKey);
				if (bucket.TryGet(sessionId, out int recordId))
				{
					bucket.RemoveSubtree(recordId);

					await _storage.CheckPoint(level: 0, StopToken).ConfigureAwait(false);
				}

				await ShrinkStateMachines(StopToken).ConfigureAwait(false);

				await base.DestroyStateMachine(sessionId).ConfigureAwait(false);
			}
			finally
			{
				_lockStateMachines.Release();
			}
		}

		private async ValueTask ShrinkStateMachines(CancellationToken token)
		{
			if (_stateMachines.Count * 2 > _stateMachineRecordId)
			{
				return;
			}

			_stateMachineRecordId = 0;
			var rootBucket = new Bucket(_storage).Nested(StateMachinesKey);
			rootBucket.RemoveSubtree(Bucket.RootKey);

			foreach (var stateMachine in _stateMachines.Values)
			{
				stateMachine.RecordId = _stateMachineRecordId ++;
				stateMachine.Store(rootBucket.Nested(stateMachine.RecordId));
			}

			if (_stateMachineRecordId > 0)
			{
				rootBucket.Add(Bucket.RootKey, _stateMachineRecordId);
			}

			await _storage.CheckPoint(level: 0, token).ConfigureAwait(false);
			await _storage.Shrink(token).ConfigureAwait(false);
		}

		private async ValueTask LoadStateMachines(CancellationToken token)
		{
			var bucket = new Bucket(_storage).Nested(StateMachinesKey);

			bucket.TryGet(Bucket.RootKey, out _stateMachineRecordId);

			if (_stateMachineRecordId == 0)
			{
				return;
			}

			await _lockStateMachines.WaitAsync(token).ConfigureAwait(false);
			try
			{
				for (var i = 0; i < _stateMachineRecordId; i ++)
				{
					var eventBucket = bucket.Nested(i);
					if (eventBucket.TryGet(Key.TypeInfo, out TypeInfo typeInfo) && typeInfo == TypeInfo.StateMachine)
					{
						var stateMachineMeta = new StateMachineMeta(bucket) { RecordId = i };
						var stateMachineController = await base.CreateAndAddStateMachine(stateMachineMeta.SessionId, stateMachineMeta, stateMachine: null, source: null, scxml: null,
																						 parameters: default, token: token).ConfigureAwait(false);
						stateMachineMeta.Controller = stateMachineController;

						_stateMachines.Add(stateMachineMeta.SessionId, stateMachineMeta);
					}
				}
			}
			finally
			{
				_lockStateMachines.Release();
			}
		}

		private async ValueTask ShrinkInvokedServices(CancellationToken token)
		{
			if (_invokedServices.Count * 2 > _invokedServiceRecordId)
			{
				return;
			}

			_invokedServiceRecordId = 0;
			var rootBucket = new Bucket(_storage).Nested(InvokedServicesKey);
			rootBucket.RemoveSubtree(Bucket.RootKey);

			foreach (var invokedService in _invokedServices.Values)
			{
				invokedService.RecordId = _invokedServiceRecordId ++;
				invokedService.Store(rootBucket.Nested(invokedService.RecordId));
			}

			if (_invokedServiceRecordId > 0)
			{
				rootBucket.Add(Bucket.RootKey, _invokedServiceRecordId);
			}

			await _storage.CheckPoint(level: 0, token).ConfigureAwait(false);
			await _storage.Shrink(token).ConfigureAwait(false);
		}

		private async ValueTask LoadInvokedServices(CancellationToken token)
		{
			var bucket = new Bucket(_storage).Nested(InvokedServicesKey);

			bucket.TryGet(Bucket.RootKey, out _invokedServiceRecordId);

			if (_invokedServiceRecordId == 0)
			{
				return;
			}

			await _lockInvokedServices.WaitAsync(token).ConfigureAwait(false);
			try
			{
				for (var i = 0; i < _invokedServiceRecordId; i ++)
				{
					var eventBucket = bucket.Nested(i);
					if (eventBucket.TryGet(Key.TypeInfo, out TypeInfo typeInfo) && typeInfo == TypeInfo.InvokedService)
					{
						var invokedService = new InvokedServiceMeta(bucket) { RecordId = i };

						if (invokedService.SessionId != null)
						{
							var stateMachine = _stateMachines[invokedService.SessionId];
							await base.AddService(invokedService.ParentSessionId, invokedService.InvokeId, invokedService.InvokeUniqueId, stateMachine.Controller).ConfigureAwait(false);

							_invokedServices.Add((invokedService.ParentSessionId, invokedService.InvokeId), invokedService);
						}
						else if (_stateMachines.TryGetValue(invokedService.ParentSessionId, out var invokingStateMachine))
						{
							var @event = new EventObject(EventType.External, EventName.ErrorExecution, data: default, sendId: null, invokedService.InvokeId, invokedService.InvokeUniqueId);
							await invokingStateMachine.Controller.Send(@event, token: default).ConfigureAwait(false);
						}
					}
				}
			}
			finally
			{
				_lockInvokedServices.Release();
			}
		}

		private class StateMachineMeta : IStoreSupport, IStateMachineOptions
		{
			public StateMachineMeta(string sessionId, IStateMachineOptions options)
			{
				SessionId = sessionId;

				if (options != null)
				{
					PersistenceLevel = options.PersistenceLevel;
					SynchronousEventProcessing = options.SynchronousEventProcessing;
					ExternalQueueSize = options.ExternalQueueSize;
				}
			}

			public StateMachineMeta(Bucket bucket)
			{
				SessionId = bucket.GetString(Key.SessionId);

				if (bucket.TryGet(Key.OptionPersistenceLevel, out PersistenceLevel persistenceLevel))
				{
					PersistenceLevel = persistenceLevel;
				}

				if (bucket.TryGet(Key.OptionSynchronousEventProcessing, out bool synchronousEventProcessing))
				{
					SynchronousEventProcessing = synchronousEventProcessing;
				}

				if (bucket.TryGet(Key.OptionExternalQueueSize, out int externalQueueSize))
				{
					ExternalQueueSize = externalQueueSize;
				}
			}

			public string                 SessionId                  { get; }
			public int                    RecordId                   { get; set; }
			public StateMachineController Controller                 { get; set; }
			public PersistenceLevel?      PersistenceLevel           { get; }
			public bool?                  SynchronousEventProcessing { get; }
			public int?                   ExternalQueueSize          { get; }

			public void Store(Bucket bucket)
			{
				bucket.Add(Key.TypeInfo, TypeInfo.StateMachine);
				bucket.Add(Key.SessionId, SessionId);

				if (PersistenceLevel != null)
				{
					bucket.Add(Key.OptionPersistenceLevel, PersistenceLevel.Value);
				}

				if (SynchronousEventProcessing != null)
				{
					bucket.Add(Key.OptionSynchronousEventProcessing, SynchronousEventProcessing.Value);
				}

				if (ExternalQueueSize != null)
				{
					bucket.Add(Key.OptionExternalQueueSize, ExternalQueueSize.Value);
				}
			}
		}

		private class InvokedServiceMeta : IStoreSupport
		{
			public InvokedServiceMeta(string parentSessionId, string invokeId, string invokeUniqueId, string sessionId)
			{
				ParentSessionId = parentSessionId;
				InvokeId = invokeId;
				InvokeUniqueId = invokeUniqueId;
				SessionId = sessionId;
			}

			public InvokedServiceMeta(Bucket bucket)
			{
				ParentSessionId = bucket.GetString(Key.ParentSessionId);
				InvokeId = bucket.GetString(Key.InvokeId);
				InvokeUniqueId = bucket.GetString(Key.InvokeUniqueId);
				SessionId = bucket.GetString(Key.SessionId);
			}

			public string ParentSessionId { get; }
			public string InvokeId        { get; }
			public string InvokeUniqueId  { get; }
			public string SessionId       { get; }
			public int    RecordId        { get; set; }

			public void Store(Bucket bucket)
			{
				bucket.Add(Key.TypeInfo, TypeInfo.InvokedService);
				bucket.Add(Key.ParentSessionId, ParentSessionId);
				bucket.Add(Key.InvokeId, InvokeId);
				bucket.Add(Key.InvokeUniqueId, InvokeUniqueId);
				bucket.Add(Key.SessionId, SessionId);
			}
		}
	}
}