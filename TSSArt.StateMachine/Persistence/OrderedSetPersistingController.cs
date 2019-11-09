using System;
using System.Collections.Generic;

namespace TSSArt.StateMachine
{
	public class OrderedSetPersistingController<TEntity> : IDisposable where TEntity : IEntity
	{
		private readonly Bucket              _bucket;
		private readonly OrderedSet<TEntity> _orderedSet;
		private          int                 _record;

		public OrderedSetPersistingController(Bucket bucket, OrderedSet<TEntity> orderedSet, Dictionary<int, IEntity> entityMap)
		{
			if (entityMap == null) throw new ArgumentNullException(nameof(entityMap));
			_bucket = bucket;
			_orderedSet = orderedSet ?? throw new ArgumentNullException(nameof(orderedSet));

			var shrink = !orderedSet.IsEmpty;
			while (true)
			{
				var recordBucket = bucket.Nested(_record);

				if (!recordBucket.TryGet(Keys.Operation, out Keys operation) ||
					!recordBucket.TryGet(Keys.DocumentId, out int documentId))
				{
					break;
				}

				switch (operation)
				{
					case Keys.Added:
						orderedSet.Add(entityMap[documentId].As<TEntity>());
						break;

					case Keys.Deleted:
						orderedSet.Delete(entityMap[documentId].As<TEntity>());
						shrink = true;
						break;
				}

				_record ++;
			}

			if (shrink)
			{
				bucket.RemoveSubtree(Bucket.RootKey);

				_record = 0;
				foreach (var entity in orderedSet.ToList())
				{
					var recordBucket = bucket.Nested(_record ++);
					recordBucket.Add(Keys.DocumentId, entity.As<IDocumentId>().DocumentId);
					recordBucket.Add(Keys.Operation, Keys.Added);
				}
			}

			orderedSet.Changed += OnChanged;
		}

		protected virtual void Dispose(bool dispose)
		{
			if (dispose)
			{
				_orderedSet.Changed -= OnChanged;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void OnChanged(OrderedSet<TEntity>.ChangedAction action, TEntity item)
		{
			switch (action)
			{
				case OrderedSet<TEntity>.ChangedAction.Add:
				{
					var bucket = _bucket.Nested(_record ++);
					bucket.Add(Keys.DocumentId, item.As<IDocumentId>().DocumentId);
					bucket.Add(Keys.Operation, Keys.Added);
					break;
				}

				case OrderedSet<TEntity>.ChangedAction.Clear:
					_record = 0;
					_bucket.RemoveSubtree(Bucket.RootKey);
					break;

				case OrderedSet<TEntity>.ChangedAction.Delete:
					if (_orderedSet.IsEmpty)
					{
						_record = 0;
						_bucket.RemoveSubtree(Bucket.RootKey);
					}
					else
					{
						var bucket = _bucket.Nested(_record ++);
						bucket.Add(Keys.DocumentId, item.As<IDocumentId>().DocumentId);
						bucket.Add(Keys.Operation, Keys.Deleted);
					}

					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(action), action, message: null);
			}
		}

		private enum Keys
		{
			DocumentId,
			Operation,
			Added,
			Deleted
		}
	}
}