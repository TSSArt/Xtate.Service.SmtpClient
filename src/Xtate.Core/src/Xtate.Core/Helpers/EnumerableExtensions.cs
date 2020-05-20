using System;
using System.Collections;
using System.Collections.Generic;
using TSSArt.StateMachine.Annotations;

namespace TSSArt.StateMachine
{
	internal static class EnumerableExtensions
	{
		public static int Capacity([NoEnumeration] this IEnumerable enumerable)
		{
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			return enumerable is ICollection collection ? collection.Count : 0;
		}

		public static int Capacity<T>([NoEnumeration] this IEnumerable<T> enumerable)
		{
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			return enumerable is ICollection<T> collection ? collection.Count : 0;
		}
	}
}