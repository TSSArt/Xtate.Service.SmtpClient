using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

		public static ImmutableArray<T> AsListOf<T>(this ImmutableArray<IEntity> list)
		{
			return !list.IsDefault ? ImmutableArray.CreateRange(list, item => item.As<T>()) : default;
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