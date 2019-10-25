using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TSSArt.StateMachine
{
	public abstract class ValidatedReadOnlyList<TList, T> : IReadOnlyList<T> where TList : ValidatedReadOnlyList<TList, T>, new()
	{
		private static readonly TList Empty = new TList { _items = Array.Empty<T>() };

		private IReadOnlyList<T> _items;

		public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count => _items.Count;

		public T this[int index] => _items[index];

		protected abstract Options GetOptions();

		public static TList Create<TSource>(IReadOnlyList<TSource> list, Func<TSource, T> selector)
		{
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var options = Empty.GetOptions();

			if (list == null)
			{
				if (!options.HasFlag(Options.AllowInputNull))
				{
					throw new ArgumentNullException(nameof(list));
				}

				return options.HasFlag(Options.ConvertNullToEmptyList) ? Empty : null;
			}

			if (list.Count == 0)
			{
				if (!options.HasFlag(Options.AllowInputEmptyList))
				{
					throw new ArgumentException(message: "Value cannot be an empty list.", nameof(list));
				}

				return options.HasFlag(Options.ConvertEmptyListToNull) ? null : Empty;
			}

			var items = new T[list.Count];

			if (options.HasFlag(Options.AllowNullItems))
			{
				for (var i = 0; i < items.Length; i ++)
				{
					items[i] = selector(list[i]);
				}
			}
			else
			{
				for (var i = 0; i < items.Length; i ++)
				{
					var item = selector(list[i]);

					if (item == null)
					{
						throw new ArgumentException(message: "List item cannot be null.", nameof(list));
					}

					items[i] = item;
				}
			}

			return new TList { _items = items };
		}

		public static TList Create(IReadOnlyList<T> list)
		{
			if (list is TList result)
			{
				return result;
			}

			var options = Empty.GetOptions();

			if (list == null)
			{
				if (!options.HasFlag(Options.AllowInputNull))
				{
					throw new ArgumentNullException(nameof(list));
				}

				return options.HasFlag(Options.ConvertNullToEmptyList) ? Empty : null;
			}

			if (list.Count == 0)
			{
				if (!options.HasFlag(Options.AllowInputEmptyList))
				{
					throw new ArgumentException(message: "Value cannot be an empty list.", nameof(list));
				}

				return options.HasFlag(Options.ConvertEmptyListToNull) ? null : Empty;
			}

			if (!options.HasFlag(Options.AllowNullItems))
			{
				foreach (var item in list)
				{
					if (item == null) throw new ArgumentException(message: "List item cannot be null.", nameof(list));
				}
			}

			return new TList { _items = list.ToArray() };
		}

		[Flags]
		protected enum Options
		{
			Default                = 0,
			AllowInputEmptyList    = 1,
			AllowInputNull         = 2,
			AllowNullItems         = 4,
			ConvertNullToEmptyList = 8,
			ConvertEmptyListToNull = 16,
			NonEmpty               = Default,
			NullIfEmpty            = AllowInputEmptyList | AllowInputNull | ConvertEmptyListToNull,
			EmptyIfNull            = AllowInputEmptyList | AllowInputNull | ConvertNullToEmptyList
		}
	}
}