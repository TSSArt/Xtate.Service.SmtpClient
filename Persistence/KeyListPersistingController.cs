using System;
using System.Buffers.Binary;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class KeyListPersistingController<T> : IDisposable where T : IEntity
	{
		private readonly Bucket               _bucket;
		private readonly KeyList<T>           _keyList;
		private readonly Dictionary<int, int> _records = new Dictionary<int, int>();

		public KeyListPersistingController(Bucket bucket, KeyList<T> keyList, Dictionary<int, IEntity> entityMap)
		{
			if (entityMap == null) throw new ArgumentNullException(nameof(entityMap));
			_bucket = bucket;
			_keyList = keyList ?? throw new ArgumentNullException(nameof(keyList));

			while (true)
			{
				var recordBucket = bucket.Nested(_records.Count);

				if (!recordBucket.TryGet(Key.Id, out int documentId) || !recordBucket.TryGet(Key.IdList, out var bytes))
				{
					break;
				}

				var list = new List<T>(bytes.Length / 4);
				for (var i = 0; i < list.Count; i ++)
				{
					var itemDocumentId = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(i * 4, length: 4).Span);
					list.Add(entityMap[itemDocumentId].As<T>());
				}

				_records.Add(documentId, _records.Count);
				keyList.Set(entityMap[documentId], list.AsReadOnly());
			}

			keyList.Changed += OnChanged;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool dispose)
		{
			if (dispose)
			{
				_keyList.Changed -= OnChanged;
			}
		}

		private void OnChanged(KeyList<T>.ChangedAction action, IEntity entity, ImmutableArray<T> list)
		{
			if (action != KeyList<T>.ChangedAction.Set)
			{
				throw new ArgumentOutOfRangeException(nameof(action), action, message: null);
			}

			Span<byte> bytes = stackalloc byte[list.Count * 4];
			for (var i = 0; i < list.Count; i ++)
			{
				BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(i * 4, length: 4), list[i].As<IDocumentId>().DocumentId);
			}

			var documentId = entity.As<IDocumentId>().DocumentId;
			if (!_records.TryGetValue(documentId, out var record))
			{
				record = _records.Count;
				_records.Add(documentId, record);
				_bucket.Nested(record).Add(Key.Id, record);
			}

			_bucket.Nested(record).Add(Key.IdList, bytes);
		}

		private enum Key
		{
			Id,
			IdList
		}
	}
}