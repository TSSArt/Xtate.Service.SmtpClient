using System;
using System.Collections.Generic;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	public static class DataModelExtensions
	{
		public static DataModelArray ToDataModelArray<T>(this IEnumerable<T> enumerable, Func<T, DataModelValue> selector)
		{
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			var dataModelArray = new DataModelArray(enumerable.Capacity());

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

			var dataModelObject = new DataModelObject(enumerable.Capacity());

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