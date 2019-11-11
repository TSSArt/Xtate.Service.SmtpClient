using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal class StateMachinePersistedController : StateMachineController, IStorageProvider
	{
		private const string ControllerStateKey = "cs";
		private const int    ScheduledEventsKey = 0;

		private readonly HashSet<ScheduledPersistedEvent> _scheduledEvents     = new HashSet<ScheduledPersistedEvent>();
		private readonly SemaphoreSlim                    _scheduledEventsLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
		private readonly CancellationToken                _stopToken;
		private readonly IStorageProvider                 _storageProvider;
		private          int                              _recordId;
		private          ITransactionalStorage            _storage;

		public StateMachinePersistedController(string sessionId, IStateMachine stateMachine, IIoProcessor ioProcessor, IStorageProvider storageProvider, TimeSpan idlePeriod,
											   in InterpreterOptions defaultOptions) : base(sessionId, stateMachine, ioProcessor, idlePeriod, defaultOptions)
		{
			_storageProvider = storageProvider;
			_stopToken = defaultOptions.StopToken;
		}

		ValueTask<ITransactionalStorage> IStorageProvider.GetTransactionalStorage(string partition, string key, CancellationToken token)
		{
			if (partition != null) throw new ArgumentException(message: "Partition argument should be null", nameof(partition));

			return _storageProvider.GetTransactionalStorage(SessionId, key, token);
		}

		ValueTask IStorageProvider.RemoveTransactionalStorage(string partition, string key, CancellationToken token)
		{
			if (partition != null) throw new ArgumentException(message: "Partition argument should be null", nameof(partition));

			return _storageProvider.RemoveTransactionalStorage(SessionId, key, token);
		}

		ValueTask IStorageProvider.RemoveAllTransactionalStorage(string partition, CancellationToken token)
		{
			if (partition != null) throw new ArgumentException(message: "Partition argument should be null", nameof(partition));

			return _storageProvider.RemoveAllTransactionalStorage(SessionId, token);
		}

		public override ValueTask DisposeAsync()
		{
			_scheduledEventsLock.Dispose();

			return _storage.DisposeAsync();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_scheduledEventsLock.Dispose();

				_storage.Dispose();
			}
		}

		protected override async ValueTask Initialize()
		{
			await base.Initialize().ConfigureAwait(false);

			_storage = await _storageProvider.GetTransactionalStorage(SessionId, ControllerStateKey, _stopToken).ConfigureAwait(false);
			await LoadState(_stopToken).ConfigureAwait(false);
		}

		protected override async ValueTask ScheduleEvent(IOutgoingEvent @event, CancellationToken token)
		{
			var scheduledPersistedEvent = new ScheduledPersistedEvent(@event);

			await _scheduledEventsLock.WaitAsync(token).ConfigureAwait(false);
			try
			{
				_scheduledEvents.Add(scheduledPersistedEvent);

				if (_storage != null)
				{
					scheduledPersistedEvent.RecordId = _recordId ++;

					var rootBucket = new Bucket(_storage).Nested(ScheduledEventsKey);
					rootBucket.Add(Bucket.RootKey, _recordId);
					scheduledPersistedEvent.Store(rootBucket.Nested(scheduledPersistedEvent.RecordId));

					await _storage.CheckPoint(level: 0, token).ConfigureAwait(false);
				}
			}
			finally
			{
				_scheduledEventsLock.Release();
			}

			var _ = DelayedFire(scheduledPersistedEvent, @event.DelayMs);
		}

		protected override async ValueTask DisposeEvent(ScheduledEvent scheduledEvent, CancellationToken token)
		{
			if (scheduledEvent == null) throw new ArgumentNullException(nameof(scheduledEvent));

			var scheduledPersistedEvent = (ScheduledPersistedEvent) scheduledEvent;

			scheduledPersistedEvent.Dispose();

			await _scheduledEventsLock.WaitAsync(token).ConfigureAwait(false);
			try
			{
				_scheduledEvents.Remove(scheduledPersistedEvent);

				if (_storage != null)
				{
					var rootBucket = new Bucket(_storage).Nested(ScheduledEventsKey);
					rootBucket.RemoveSubtree(scheduledPersistedEvent.RecordId);

					await _storage.CheckPoint(level: 0, token).ConfigureAwait(false);

					await ShrinkScheduledEvents(token).ConfigureAwait(false);
				}
			}
			finally
			{
				_scheduledEventsLock.Release();
			}
		}

		private async ValueTask ShrinkScheduledEvents(CancellationToken token)
		{
			if (_scheduledEvents.Count * 2 > _recordId)
			{
				return;
			}

			_recordId = 0;
			var rootBucket = new Bucket(_storage).Nested(ScheduledEventsKey);
			rootBucket.RemoveSubtree(Bucket.RootKey);

			foreach (var scheduledEvent in _scheduledEvents)
			{
				scheduledEvent.RecordId = _recordId ++;
				scheduledEvent.Store(rootBucket.Nested(scheduledEvent.RecordId));
			}

			if (_recordId > 0)
			{
				rootBucket.Add(Bucket.RootKey, _recordId);
			}

			await _storage.CheckPoint(level: 0, token).ConfigureAwait(false);
			await _storage.Shrink(token).ConfigureAwait(false);
		}

		private async ValueTask LoadState(CancellationToken token)
		{
			var bucket = new Bucket(_storage).Nested(ScheduledEventsKey);

			bucket.TryGet(Bucket.RootKey, out _recordId);

			if (_recordId == 0)
			{
				return;
			}

			var utcNow = DateTime.UtcNow;

			await _scheduledEventsLock.WaitAsync(token).ConfigureAwait(false);
			try
			{
				for (var i = 0; i < _recordId; i ++)
				{
					var eventBucket = bucket.Nested(i);
					if (eventBucket.TryGet(Key.TypeInfo, out TypeInfo typeInfo) && typeInfo == TypeInfo.ScheduledEvent)
					{
						var scheduledEvent = new ScheduledPersistedEvent(eventBucket) { RecordId = i };

						_scheduledEvents.Add(scheduledEvent);

						var delayMs = (int) ((scheduledEvent.FireOnUtc - utcNow).Ticks / TimeSpan.TicksPerMillisecond);

						var _ = DelayedFire(scheduledEvent, delayMs > 1 ? delayMs : 1);
					}
				}
			}
			finally
			{
				_scheduledEventsLock.Release();
			}
		}

		protected class ScheduledPersistedEvent : ScheduledEvent, IStoreSupport
		{
			private readonly long _fireOnUtcTicks;

			public ScheduledPersistedEvent(IOutgoingEvent @event) : base(@event)
			{
				if (@event == null) throw new ArgumentNullException(nameof(@event));

				_fireOnUtcTicks = DateTime.UtcNow.Ticks + @event.DelayMs * TimeSpan.TicksPerMillisecond;
			}

			public ScheduledPersistedEvent(Bucket bucket) : base(RestoreEvent(bucket))
			{
				bucket.TryGet(Key.FireOn, out _fireOnUtcTicks);
			}

			public int RecordId { get; set; }

			public DateTime FireOnUtc => new DateTime(_fireOnUtcTicks, DateTimeKind.Utc);

			public void Store(Bucket bucket)
			{
				bucket.Add(Key.TypeInfo, TypeInfo.ScheduledEvent);
				bucket.Add(Key.SendId, Event.SendId);
				bucket.Add(Key.Name, EventName.ToName(Event.NameParts));
				bucket.Add(Key.Target, Event.Target);
				bucket.Add(Key.Type, Event.Type);
				bucket.Add(Key.DelayMs, Event.DelayMs);
				bucket.Add(Key.FireOn, _fireOnUtcTicks);

				if (Event.Data.Type != DataModelValueType.Undefined)
				{
					var dataBucket = bucket.Nested(Key.Data);
					using var tracker = new DataModelReferenceTracker(dataBucket.Nested(Key.DataReferences));
					dataBucket.SetDataModelValue(tracker, Event.Data);
				}
			}

			private static IOutgoingEvent RestoreEvent(Bucket bucket)
			{
				var dataBucket = bucket.Nested(Key.Data);
				using var tracker = new DataModelReferenceTracker(dataBucket.Nested(Key.DataReferences));

				return new Event
					   {
							   SendId = bucket.GetString(Key.SendId),
							   NameParts = EventName.ToParts(bucket.GetString(Key.Name)),
							   Target = bucket.GetUri(Key.Target),
							   Type = bucket.GetUri(Key.Type),
							   DelayMs = bucket.GetInt32(Key.DelayMs),
							   Data = dataBucket.GetDataModelValue(tracker, baseValue: default)
					   };
			}
		}
	}
}