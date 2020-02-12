using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal static class BucketExtensions
	{
		public static void AddEntity<T>(this in Bucket bucket, Key key, T entity) where T : IEntity
		{
			if (entity != null)
			{
				entity.As<IStoreSupport>().Store(bucket.Nested(key));
			}
		}

		public static void AddEntityList<T>(this in Bucket bucket, Key key, ImmutableArray<T> array) where T : IEntity
		{
			if (array.IsDefaultOrEmpty)
			{
				return;
			}

			bucket.Add(key, array.Length);

			var listStorage = bucket.Nested(key);

			for (var i = 0; i < array.Length; i ++)
			{
				var entity = array[i];
				if (entity != null)
				{
					entity.As<IStoreSupport>().Store(listStorage.Nested(i));
				}
			}
		}

		public static void AddDictionary(this in Bucket bucket, Key key, ImmutableDictionary<string, string> dictionary)
		{
			if (dictionary == null)
			{
				return;
			}

			bucket.Add(key, dictionary.Count);

			var dicStorage = bucket.Nested(key);

			var index = 0;
			foreach (var pair in dictionary)
			{
				var pairStorage = dicStorage.Nested(index ++);
				pairStorage.Add(Key.Key, pair.Key);
				pairStorage.Add(Key.Value, pair.Value);
			}
		}

		public static ImmutableDictionary<string, string> RestoreDictionary(this in Bucket bucket, Key key)
		{
			if (!bucket.TryGet(key, out int length))
			{
				return null;
			}

			var itemsBucket = bucket.Nested(key);

			var builder = ImmutableDictionary.CreateBuilder<string, string>();

			for (var i = 0; i < length; i ++)
			{
				var pair = itemsBucket.Nested(i);
				builder[pair.GetString(Key.Key)] = pair.GetString(Key.Value);
			}

			return builder.ToImmutable();
		}

		public static ImmutableArray<T> RestoreList<T>(this in Bucket bucket, Key key, Func<Bucket, T> factory)
		{
			if (!bucket.TryGet(key, out int length))
			{
				return default;
			}

			var itemsBucket = bucket.Nested(key);

			var builder = ImmutableArray.CreateBuilder<T>(length);

			for (var i = 0; i < length; i ++)
			{
				builder.Add(factory(itemsBucket.Nested(i)));
			}

			return builder.MoveToImmutable();
		}

		public static TEnum Get<TEnum>(this in Bucket bucket, Key key) where TEnum : struct, Enum =>
				bucket.TryGet(key, out TEnum value) ? value : throw new KeyNotFoundException($"'{key}' key not found");

		public static int GetInt32(this in Bucket bucket, Key key) => bucket.TryGet(key, out int value) ? value : throw new KeyNotFoundException($"'{key}' key not found");

		public static bool GetBoolean(this in Bucket bucket, Key key) => bucket.TryGet(key, out bool value) ? value : throw new KeyNotFoundException($"'{key}' key not found");

		public static string GetString(this in Bucket bucket, Key key) => bucket.TryGet(key, out string value) ? value : null;

		public static Uri GetUri(this in Bucket bucket, Key key) => bucket.TryGet(key, out Uri value) ? value : null;

		public static DataModelValue GetDataModelValue(this in Bucket bucket, DataModelReferenceTracker tracker, DataModelValue baseValue)
		{
			if (tracker == null) throw new ArgumentNullException(nameof(tracker));

			var type = bucket.Get<DataModelValueType>(Key.Type);

			switch (type)
			{
				case DataModelValueType.Undefined: return default;
				case DataModelValueType.Null: return DataModelValue.Null;
				case DataModelValueType.String when bucket.TryGet(Key.Item, out string value): return new DataModelValue(value);
				case DataModelValueType.Number when bucket.TryGet(Key.Item, out double value): return new DataModelValue(value);
				case DataModelValueType.DateTime when bucket.TryGet(Key.Item, out DateTime value): return new DataModelValue(value);
				case DataModelValueType.Boolean when bucket.TryGet(Key.Item, out bool value): return new DataModelValue(value);

				case DataModelValueType.Object when bucket.TryGet(Key.RefId, out int refId):
					var dataModelObject = baseValue.Type == DataModelValueType.Object ? baseValue.AsObject() : null;
					return DataModelValue.FromObject(tracker.GetValue(refId, type, dataModelObject));

				case DataModelValueType.Array when bucket.TryGet(Key.RefId, out int refId):
					var dataModelArray = baseValue.Type == DataModelValueType.Array ? baseValue.AsArray() : null;
					return DataModelValue.FromObject(tracker.GetValue(refId, type, dataModelArray));

				default: throw new ArgumentOutOfRangeException();
			}
		}

		public static void SetDataModelValue(this in Bucket bucket, DataModelReferenceTracker tracker, DataModelValue item)
		{
			if (tracker == null) throw new ArgumentNullException(nameof(tracker));

			bucket.Add(Key.Type, item.Type);

			switch (item.Type)
			{
				case DataModelValueType.Undefined: break;
				case DataModelValueType.Null: break;

				case DataModelValueType.String:
					bucket.Add(Key.Item, item.AsString());
					break;

				case DataModelValueType.Number:
					bucket.Add(Key.Item, item.AsNumber());
					break;

				case DataModelValueType.DateTime:
					bucket.Add(Key.Item, item.AsDateTime());
					break;

				case DataModelValueType.Boolean:
					bucket.Add(Key.Item, item.AsBoolean());
					break;

				case DataModelValueType.Object:
					bucket.Add(Key.RefId, tracker.GetRefId(item));
					break;

				case DataModelValueType.Array:
					bucket.Add(Key.RefId, tracker.GetRefId(item));
					break;

				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
}