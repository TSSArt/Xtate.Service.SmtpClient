using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class IoProcessorPersistedContext : IoProcessorContext
	{
		private const string IoProcessorPartition = "IoProcessor";
		private const string ContextKey           = "context";
		private const int    StateMachinesKey     = 0;
		private const int    InvokedServicesKey   = 1;

		private readonly TimeSpan                                                        _idlePeriod;
		private readonly Dictionary<(string SessionId, string InvokeId), InvokedService> _invokedServices = new Dictionary<(string SessionId, string InvokeId), InvokedService>();
		private readonly IoProcessor                                                     _ioProcessor;
		private readonly SemaphoreSlim                                                   _lockInvokedServices = new SemaphoreSlim(initialCount: 1, maxCount: 1);
		private readonly SemaphoreSlim                                                   _lockStateMachines   = new SemaphoreSlim(initialCount: 1, maxCount: 1);
		private readonly Dictionary<string, StateMachine>                                _stateMachines       = new Dictionary<string, StateMachine>();
		private readonly CancellationToken                                               _stopToken;
		private readonly IStorageProvider                                                _storageProvider;
		private          int                                                             _invokedServiceRecordId;
		private          int                                                             _stateMachineRecordId;
		private          ITransactionalStorage                                           _storage;

		public IoProcessorPersistedContext(IoProcessor ioProcessor, in IoProcessorOptions options) : base(ioProcessor, options)
		{
			_ioProcessor = ioProcessor;
			_storageProvider = options.StorageProvider ?? throw new ArgumentNullException(nameof(options.StorageProvider));
			_idlePeriod = options.SuspendIdlePeriod;
			_stopToken = options.StopToken;
		}

		public override async ValueTask Initialize()
		{
			_storage = await _storageProvider.GetTransactionalStorage(IoProcessorPartition, ContextKey, _stopToken).ConfigureAwait(false);

			await LoadStateMachines(_stopToken).ConfigureAwait(false);
			await LoadInvokedServices(_stopToken).ConfigureAwait(false);
		}

		protected override void Dispose(bool dispose)
		{
			if (dispose)
			{
				_lockInvokedServices.Dispose();
				_lockStateMachines.Dispose();

				_storage.Dispose();
			}
		}

		public override ValueTask DisposeAsync()
		{
			_lockInvokedServices.Dispose();
			_lockStateMachines.Dispose();

			return _storage.DisposeAsync();
		}

		public override async ValueTask AddService(string sessionId, string invokeId, IService service)
		{
			await _lockInvokedServices.WaitAsync(_stopToken).ConfigureAwait(false);
			try
			{
				await base.AddService(sessionId, invokeId, service).ConfigureAwait(false);

				var bucket = new Bucket(_storage).Nested(InvokedServicesKey);
				var recordId = _invokedServiceRecordId ++;

				var invokedSessionId = service is StateMachineController stateMachineController ? stateMachineController.SessionId : null;
				var invokedService = new InvokedService(sessionId, invokeId, invokedSessionId) { RecordId = recordId };
				_invokedServices.Add((sessionId, invokeId), invokedService);

				bucket.Add(Bucket.RootKey, _invokedServiceRecordId);

				invokedService.Store(bucket.Nested(recordId));

				await _storage.CheckPoint(level: 0, _stopToken).ConfigureAwait(false);
			}
			finally
			{
				_lockInvokedServices.Release();
			}
		}

		public override async ValueTask<IService> TryRemoveService(string sessionId, string invokeId)
		{
			await _lockInvokedServices.WaitAsync(_stopToken).ConfigureAwait(false);
			try
			{
				_invokedServices.Remove((sessionId, invokeId));

				var bucket = new Bucket(_storage).Nested(InvokedServicesKey);
				if (bucket.TryGet(sessionId, out int recordId))
				{
					bucket.RemoveSubtree(recordId);

					await _storage.CheckPoint(level: 0, _stopToken).ConfigureAwait(false);
				}

				await ShrinkInvokedServices(_stopToken).ConfigureAwait(false);

				return await base.TryRemoveService(sessionId, invokeId).ConfigureAwait(false);
			}
			finally
			{
				_lockInvokedServices.Release();
			}
		}

		protected override StateMachineController CreateStateMachineController(string sessionId, IStateMachine stateMachine, in InterpreterOptions options) =>
				new StateMachinePersistedController(sessionId, stateMachine, _ioProcessor, _storageProvider, _idlePeriod, options);

		public override async ValueTask<StateMachineController> CreateAndAddStateMachine(string sessionId, Uri source, DataModelValue arguments)
		{
			await _lockStateMachines.WaitAsync(_stopToken).ConfigureAwait(false);
			try
			{
				var stateMachineController = await base.CreateAndAddStateMachine(sessionId, source, arguments).ConfigureAwait(false);

				var bucket = new Bucket(_storage).Nested(StateMachinesKey);
				var recordId = _stateMachineRecordId ++;

				var stateMachine = new StateMachine(sessionId, source, arguments) { RecordId = recordId, Controller = stateMachineController };
				_stateMachines.Add(sessionId, stateMachine);

				bucket.Add(Bucket.RootKey, _stateMachineRecordId);

				stateMachine.Store(bucket.Nested(recordId));

				await _storage.CheckPoint(level: 0, _stopToken).ConfigureAwait(false);

				stateMachine.Controller = stateMachineController;

				return stateMachineController;
			}
			finally
			{
				_lockStateMachines.Release();
			}
		}

		public override async ValueTask DestroyStateMachine(string sessionId)
		{
			await _lockStateMachines.WaitAsync(_stopToken).ConfigureAwait(false);
			try
			{
				_stateMachines.Remove(sessionId);

				var bucket = new Bucket(_storage).Nested(StateMachinesKey);
				if (bucket.TryGet(sessionId, out int recordId))
				{
					bucket.RemoveSubtree(recordId);

					await _storage.CheckPoint(level: 0, _stopToken).ConfigureAwait(false);
				}

				await ShrinkStateMachines(_stopToken).ConfigureAwait(false);

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
						var stateMachine = new StateMachine(bucket) { RecordId = i };
						var stateMachineController = await base.CreateAndAddStateMachine(stateMachine.SessionId, stateMachine.Source, stateMachine.Arguments).ConfigureAwait(false);
						stateMachine.Controller = stateMachineController;

						_stateMachines.Add(stateMachine.SessionId, stateMachine);
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
						var invokedService = new InvokedService(bucket) { RecordId = i };

						if (invokedService.SessionId != null)
						{
							var stateMachine = _stateMachines[invokedService.SessionId];
							await base.AddService(invokedService.ParentSessionId, invokedService.InvokeId, stateMachine.Controller).ConfigureAwait(false);

							_invokedServices.Add((invokedService.ParentSessionId, invokedService.InvokeId), invokedService);
						}
						else if (_stateMachines.TryGetValue(invokedService.ParentSessionId, out var invokingStateMachine))
						{
							var nameParts = EventName.GetErrorInvokeNameParts((Identifier) invokedService.InvokeId);
							var controller = invokingStateMachine.Controller;
							await controller.Send(new EventObject(EventType.External, nameParts, data: default, sendId: null, invokedService.InvokeId), token: default).ConfigureAwait(false);
						}
					}
				}
			}
			finally
			{
				_lockInvokedServices.Release();
			}
		}

		private class StateMachine : IStoreSupport
		{
			public StateMachine(string sessionId, Uri source, in DataModelValue arguments)
			{
				SessionId = sessionId;
				Source = source;
				Arguments = arguments;
			}

			public StateMachine(Bucket bucket)
			{
				SessionId = bucket.GetString(Key.SessionId);
				Source = bucket.GetUri(Key.Source);

				var argBucket = bucket.Nested(Key.Data);
				using var tracker = new DataModelReferenceTracker(argBucket.Nested(Key.DataReferences));
				argBucket.GetDataModelValue(tracker, Arguments);
			}

			public string                 SessionId  { get; }
			public Uri                    Source     { get; }
			public DataModelValue         Arguments  { get; }
			public int                    RecordId   { get; set; }
			public StateMachineController Controller { get; set; }

			public void Store(Bucket bucket)
			{
				bucket.Add(Key.TypeInfo, TypeInfo.StateMachine);
				bucket.Add(Key.SessionId, SessionId);
				bucket.Add(Key.Source, Source);

				if (Arguments.Type != DataModelValueType.Undefined)
				{
					var argBucket = bucket.Nested(Key.Data);
					using var tracker = new DataModelReferenceTracker(argBucket.Nested(Key.DataReferences));
					argBucket.SetDataModelValue(tracker, Arguments);
				}
			}
		}

		private class InvokedService : IStoreSupport
		{
			public InvokedService(string parentSessionId, string invokeId, string sessionId)
			{
				ParentSessionId = parentSessionId;
				InvokeId = invokeId;
				SessionId = sessionId;
			}

			public InvokedService(Bucket bucket)
			{
				ParentSessionId = bucket.GetString(Key.ParentSessionId);
				InvokeId = bucket.GetString(Key.InvokeId);
				SessionId = bucket.GetString(Key.SessionId);
			}

			public string ParentSessionId { get; }
			public string InvokeId        { get; }
			public string SessionId       { get; }
			public int    RecordId        { get; set; }

			public void Store(Bucket bucket)
			{
				bucket.Add(Key.TypeInfo, TypeInfo.InvokedService);
				bucket.Add(Key.ParentSessionId, ParentSessionId);
				bucket.Add(Key.InvokeId, InvokeId);
				bucket.Add(Key.SessionId, SessionId);
			}
		}
	}
}