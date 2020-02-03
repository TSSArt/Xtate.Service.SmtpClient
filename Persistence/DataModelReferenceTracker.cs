﻿using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public sealed class DataModelReferenceTracker : IDisposable
	{
		private readonly Bucket _bucket;

		private readonly Dictionary<object, Entry> _objects = new Dictionary<object, Entry>();
		private readonly Dictionary<int, object>   _refIds  = new Dictionary<int, object>();
		private          int                       _nextRefId;

		public DataModelReferenceTracker(Bucket bucket)
		{
			_bucket = bucket;
			bucket.TryGet(Bucket.RootKey, out _nextRefId);
		}

		public void Dispose()
		{
			foreach (var entry in _objects.Values)
			{
				entry.Controller.Dispose();
			}
		}

		public object GetValue(int refId, DataModelValueType type, object baseObject)
		{
			if (_refIds.TryGetValue(refId, out var obj))
			{
				if (baseObject != null && obj != baseObject)
				{
					throw new InvalidOperationException("Objects structure mismatch");
				}

				return obj;
			}

			var bucket = _bucket.Nested(refId);
			bucket.TryGet(Key.ReadOnly, out bool isReadOnly);

			switch (type)
			{
				case DataModelValueType.Object:
					obj = baseObject ?? new DataModelObject(isReadOnly);
					_objects[obj] = new Entry { RefCount = 0, RefId = refId, Controller = ObjectControllerCreator(bucket, obj) };
					break;

				case DataModelValueType.Array:
					obj = baseObject ?? new DataModelArray(isReadOnly);
					_objects[obj] = new Entry { RefCount = 0, RefId = refId, Controller = ArrayControllerCreator(bucket, obj) };
					break;

				default: throw new ArgumentOutOfRangeException();
			}

			_refIds[refId] = obj;

			return obj;
		}

		private int GetRefId(object obj, Func<Bucket, object, DataModelPersistingController> creator, bool incrementReference)
		{
			if (!_objects.TryGetValue(obj, out var entry))
			{
				var refId = _nextRefId ++;
				_bucket.Add(Bucket.RootKey, _nextRefId);
				entry.RefCount = incrementReference ? 1 : 0;
				entry.RefId = refId;
				_refIds[refId] = obj;
				entry.Controller = creator(_bucket.Nested(refId), obj);
				_objects[obj] = entry;
			}
			else if (incrementReference)
			{
				entry.RefCount ++;
				_objects[obj] = entry;
			}

			return entry.RefId;
		}

		public int GetRefId(DataModelValue value)
		{
			switch (value.Type)
			{
				case DataModelValueType.Object: return GetRefId(value.AsObject(), ObjectControllerCreator, incrementReference: false);
				case DataModelValueType.Array: return GetRefId(value.AsArray(), ArrayControllerCreator, incrementReference: false);
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public void AddReference(DataModelValue value)
		{
			switch (value.Type)
			{
				case DataModelValueType.Object:
					GetRefId(value.AsObject(), ObjectControllerCreator, incrementReference: true);
					break;
				case DataModelValueType.Array:
					GetRefId(value.AsArray(), ArrayControllerCreator, incrementReference: true);
					break;
			}
		}

		private DataModelPersistingController ObjectControllerCreator(Bucket bucket, object obj) => new DataModelObjectPersistingController(bucket, this, (DataModelObject) obj);
		private DataModelPersistingController ArrayControllerCreator(Bucket bucket, object obj)  => new DataModelArrayPersistingController(bucket, this, (DataModelArray) obj);

		public void RemoveReference(DataModelValue value)
		{
			switch (value.Type)
			{
				case DataModelValueType.Object:
					Remove(value.AsObject());
					break;
				case DataModelValueType.Array:
					Remove(value.AsArray());
					break;
			}

			void Remove(object obj)
			{
				if (_objects.TryGetValue(obj, out var entry))
				{
					if (-- entry.RefCount <= 0)
					{
						entry.Controller.Dispose();
						_bucket.RemoveSubtree(entry.RefId);
						_objects.Remove(obj);
						_refIds.Remove(entry.RefId);
					}
					else
					{
						_objects[obj] = entry;
					}
				}
			}
		}

		private struct Entry
		{
			public int                           RefId;
			public int                           RefCount;
			public DataModelPersistingController Controller;
		}
	}
}