using System;
using System.Collections.Immutable;

namespace TSSArt.StateMachine
{
	public class OrderedSet<T>
	{
		public delegate void ChangedHandler(ChangedAction action, T item);

		public enum ChangedAction
		{
			Add,
			Clear,
			Delete
		}

		private readonly List<T> _items;

		public OrderedSet() => _items = new List<T>();

		public bool IsEmpty => _items.Count == 0;

		public event ChangedHandler Changed;

		public void Add(T item)
		{
			if (!_items.Contains(item))
			{
				_items.Add(item);

				Changed?.Invoke(ChangedAction.Add, item);
			}
		}

		public void Clear()
		{
			_items.Clear();

			Changed?.Invoke(ChangedAction.Clear, item: default);
		}

		public bool IsMember(T item) => _items.Contains(item);

		public void Delete(T item)
		{
			_items.Remove(item);

			Changed?.Invoke(ChangedAction.Delete, item);
		}

		public void Union(ImmutableArray<T> orderedSet)
		{
			if (orderedSet == null) throw new ArgumentNullException(nameof(orderedSet));

			foreach (var item in orderedSet)
			{
				Add(item);
			}
		}

		public bool HasIntersection(OrderedSet<T> orderedSet)
		{
			if (orderedSet == null) throw new ArgumentNullException(nameof(orderedSet));

			foreach (var item in orderedSet._items)
			{
				if (_items.Contains(item))
				{
					return true;
				}
			}

			return false;
		}

		public bool Some(Predicate<T> predicate) => _items.Exists(predicate);

		public bool Every(Predicate<T> predicate) => _items.TrueForAll(predicate);

		public ImmutableArray<T> ToList() => _items;

		public ImmutableArray<T> ToSortedList(IComparer<T> comparer)
		{
			var array = _items.ToArray();
			Array.Sort(array, comparer);

			return array;
		}

		public ImmutableArray<T> ToFilteredSortedList(Predicate<T> predicate, IComparer<T> comparer)
		{
			var list = _items.FindAll(predicate);
			list.Sort(comparer);

			return list;
		}

		public ImmutableArray<T> ToFilteredList(Predicate<T> predicate) => _items.FindAll(predicate);
	}
}