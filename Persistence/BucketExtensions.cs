using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	internal static class BucketExtensions
	{
		public static void AddEntity<T>(this in Bucket bucket, Key key, T? entity) where T : class
		{
			entity?.As<IStoreSupport>().Store(bucket.Nested(key));
		}

		public static void AddEntityList<T>(this in Bucket bucket, Key key, ImmutableArray<T> array) where T : class
		{
			if (array.IsDefaultOrEmpty)
			{
				return;
			}

			bucket.Add(key, array.Length);

			var listStorage = bucket.Nested(key);

			for (var i = 0; i < array.Length; i ++)
			{
				array[i].As<IStoreSupport>().Store(listStorage.Nested(i));
			}
		}

		public static ImmutableArray<T> RestoreList<T>(this in Bucket bucket, Key key, Func<Bucket, T?> factory) where T : class
		{
			if (!bucket.TryGet(key, out int length))
			{
				return default;
			}

			var itemsBucket = bucket.Nested(key);

			var builder = ImmutableArray.CreateBuilder<T>(length);

			for (var i = 0; i < length; i ++)
			{
				var item = factory(itemsBucket.Nested(i));

				if (item == null)
				{
					throw new StateMachinePersistenceException(Resources.Exception_Item_can_t_be_null);
				}

				builder.Add(item);
			}

			return builder.MoveToImmutable();
		}

		public static TEnum Get<TEnum>(this in Bucket bucket, Key key) where TEnum : struct, Enum =>
				bucket.TryGet(key, out TEnum value) ? value : throw new KeyNotFoundException(Res.Format(Resources.Exception_key_not_found, key));

		public static int GetInt32(this in Bucket bucket, Key key) => bucket.TryGet(key, out int value) ? value : throw new KeyNotFoundException(Res.Format(Resources.Exception_key_not_found, key));

		public static bool GetBoolean(this in Bucket bucket, Key key) =>
				bucket.TryGet(key, out bool value) ? value : throw new KeyNotFoundException(Res.Format(Resources.Exception_key_not_found, key));

		public static string? GetString(this in Bucket bucket, Key key) => bucket.TryGet(key, out string? value) ? value : null;

		public static Uri? GetUri(this in Bucket bucket, Key key) => bucket.TryGet(key, out Uri? value) ? value : null;

		public static DataModelValue GetDataModelValue(this in Bucket bucket, DataModelReferenceTracker tracker, DataModelValue baseValue)
		{
			if (tracker == null) throw new ArgumentNullException(nameof(tracker));

			var type = bucket.Get<DataModelValueType>(Key.Type);

			switch (type)
			{
				case DataModelValueType.Undefined: return default;
				case DataModelValueType.Null: return DataModelValue.Null;
				case DataModelValueType.String when bucket.TryGet(Key.Item, out string? value): return new DataModelValue(value);
				case DataModelValueType.Number when bucket.TryGet(Key.Item, out double value): return new DataModelValue(value);
				case DataModelValueType.DateTime when bucket.TryGet(Key.Item, out DateTimeOffset value): return new DataModelValue(value);
				case DataModelValueType.Boolean when bucket.TryGet(Key.Item, out bool value): return new DataModelValue(value);

				case DataModelValueType.Object when bucket.TryGet(Key.RefId, out int refId):
					var dataModelObject = baseValue.Type == DataModelValueType.Object ? baseValue.AsObject() : null;
					return DataModelValue.FromObject(tracker.GetValue(refId, type, dataModelObject));

				case DataModelValueType.Array when bucket.TryGet(Key.RefId, out int refId):
					var dataModelArray = baseValue.Type == DataModelValueType.Array ? baseValue.AsArray() : null;
					return DataModelValue.FromObject(tracker.GetValue(refId, type, dataModelArray));

				default: return Infrastructure.UnexpectedValue<DataModelValue>();
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
					bucket.Add(Key.Item, item.AsDateTimeOffset());
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

				default:
					Infrastructure.UnexpectedValue();
					break;
			}
		}
	}
}