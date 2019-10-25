using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TSSArt.StateMachine
{
	public static class AncestorProviderExtensions
	{
		public static T Base<T>(this IEntity entity)
		{
			if (entity == null)
			{
				return default;
			}

			T result = default;
			Base(entity, ref result);
			return result;
		}

		private static bool Base<T>(object entity, ref T value)
		{
			if (entity is IAncestorProvider provider && Base(provider.Ancestor, ref value))
			{
				return true;
			}

			if (entity is T val)
			{
				value = val;
				return true;
			}

			return false;
		}

		public static T As<T>(this IEntity entity)
		{
			if (entity == null)
			{
				return default;
			}

			if (entity.Is<T>(out var result))
			{
				return result;
			}

			throw new InvalidCastException($"Type '{typeof(T).Name}' can't be found in type '{entity.GetType().Name}' or in its ancestors.");
		}

		public static bool Is<T>(this IEntity entity)
		{
			object obj = entity;

			while (true)
			{
				switch (obj)
				{
					case T _:
						return true;

					case IAncestorProvider provider:
						obj = provider.Ancestor;
						break;

					default:
						return false;
				}
			}
		}

		public static bool Is<T>(this IEntity entity, out T value)
		{
			object obj = entity;

			while (true)
			{
				switch (obj)
				{
					case T val:
						value = val;
						return true;

					case IAncestorProvider provider:
						obj = provider.Ancestor;
						break;

					default:
						value = default;
						return false;
				}
			}
		}

		public static IReadOnlyList<T> AsListOf<T>(this IReadOnlyList<IEntity> list)
		{
			if (list == null)
			{
				return null;
			}

			if (list.Count == 0)
			{
				return Array.Empty<T>();
			}

			var result = new T[list.Count];
			for (var i = 0; i < result.Length; i ++)
			{
				result[i] = list[i].As<T>();
			}

			return new ReadOnlyCollection<T>(result);
		}

		public static IEnumerable<T> AsEnumerableOf<T>(this IEnumerable<IEntity> enumerable)
		{
			if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

			foreach (var item in enumerable)
			{
				yield return item.As<T>();
			}
		}
	}
}