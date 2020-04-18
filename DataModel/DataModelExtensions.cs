using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public static class DataModelExtensions
	{
		public static DataModelArray ToDataModelArray<T>(this IEnumerable<T> enumerable, Func<T, DataModelValue> selector)
		{
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			var dataModelArray = enumerable is ICollection<T> collection ? new DataModelArray(collection.Count) : new DataModelArray();

			foreach (var item in enumerable)
			{
				dataModelArray.Add(selector(item));
			}

			return dataModelArray;
		}

		public static DataModelArray ToDataModelArray<T>(this IEnumerable<T> enumerable) => enumerable.ToDataModelArray(arg => DataModelValue.FromObject(arg));

		public static DataModelObject ToDataModelObject<T>(this IEnumerable<T> enumerable, Func<T, string> keySelector, Func<T, DataModelValue> valueSelector)
		{
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			var dataModelObject = new DataModelObject();

			foreach (var item in enumerable)
			{
				dataModelObject[keySelector(item)] = valueSelector(item);
			}

			return dataModelObject;
		}

		public static DataModelObject ToDataModelObject<T>(this IEnumerable<T> enumerable, Func<T, string> keySelector) =>
				enumerable.ToDataModelObject(keySelector, arg => DataModelValue.FromObject(arg));
	}
}