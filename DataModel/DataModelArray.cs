using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security;

namespace TSSArt.StateMachine
{
	public class DataModelArray : DynamicObject, IList<DataModelValue>
	{
		public delegate void ChangedHandler(ChangedAction action, int index, DataModelValue value);

		public enum ChangedAction
		{
			Set,
			Clear,
			Insert,
			Remove,
			SetLength
		}

		private readonly List<DataModelValue> _list;

		public DataModelArray() : this(false) { }

		public DataModelArray(bool isReadOnly)
		{
			IsReadOnly = isReadOnly;
			_list = new List<DataModelValue>();
		}

		public DataModelArray(int capacity) => _list = new List<DataModelValue>(capacity);

		public DataModelArray(IEnumerable<DataModelValue> items) => _list = new List<DataModelValue>(items);

		public int Length => _list.Count;

		public bool IsReadOnly { get; private set; }

		public IEnumerator<DataModelValue> GetEnumerator() => _list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Add(DataModelValue item)
		{
			if (!CanAdd(item))
			{
				throw ObjectCantBeModifiedException();
			}

			AddInternal(item);
		}

		public void Clear()
		{
			if (!CanClear())
			{
				throw ObjectCantBeModifiedException();
			}

			ClearInternal();
		}

		public bool Contains(DataModelValue item) => _list.Contains(item);

		public void CopyTo(DataModelValue[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

		public bool Remove(DataModelValue item)
		{
			if (!CanRemove(item))
			{
				throw ObjectCantBeModifiedException();
			}

			return RemoveInternal(item);
		}

		int ICollection<DataModelValue>.Count => _list.Count;

		public int IndexOf(DataModelValue item) => _list.IndexOf(item);

		public void Insert(int index, DataModelValue item)
		{
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

			if (!CanInsert(index, item))
			{
				throw ObjectCantBeModifiedException();
			}

			InsertInternal(index, item);
		}

		public void RemoveAt(int index)
		{
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

			if (!CanRemoveAt(index))
			{
				throw ObjectCantBeModifiedException();
			}

			RemoveAtInternal(index);
		}

		public DataModelValue this[int index]
		{
			get
			{
				if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

				return index < _list.Count ? _list[index] : DataModelValue.Undefined(IsReadOnly);
			}
			set
			{
				if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

				if (!CanSet(index, value))
				{
					throw ObjectCantBeModifiedException();
				}

				SetInternal(index, value);
			}
		}

		public event ChangedHandler Changed;

		public void Freeze() => IsReadOnly = true;

		internal void AddInternal(DataModelValue item)
		{
			_list.Add(item);

			Changed?.Invoke(ChangedAction.Set, _list.Count - 1, item);
		}

		internal void ClearInternal()
		{
			Changed?.Invoke(ChangedAction.Clear, index: default, value: default);

			_list.Clear();
		}

		internal bool RemoveInternal(DataModelValue item)
		{
			var index = _list.IndexOf(item);

			if (index >= 0)
			{
				Changed?.Invoke(ChangedAction.Remove, index, item);

				_list.RemoveAt(index);

				return true;
			}

			return false;
		}

		internal void InsertInternal(int index, DataModelValue item)
		{
			if (index > Length)
			{
				SetLength(index);
			}

			_list.Insert(index, item);

			Changed?.Invoke(ChangedAction.Insert, index, item);
		}

		internal void RemoveAtInternal(int index)
		{
			if (index < _list.Count)
			{
				Changed?.Invoke(ChangedAction.Remove, index, _list[index]);

				_list.RemoveAt(index);
			}
		}

		internal void SetInternal(int index, DataModelValue value)
		{
			if (index < _list.Count)
			{
				Changed?.Invoke(ChangedAction.Remove, index, _list[index]);

				_list[index] = value;

				Changed?.Invoke(ChangedAction.Set, index, value);

				return;
			}

			if (index > _list.Count)
			{
				_list.Capacity = index + 1;
				_list.AddRange(Enumerable.Repeat(DataModelValue.Undefined(), index - Length));
			}

			_list.Add(value);

			Changed?.Invoke(ChangedAction.Set, index, value);
		}

		private static Exception ObjectCantBeModifiedException() => new SecurityException("Object can not be modified");

		public bool CanAdd(DataModelValue item) => !IsReadOnly;

		public bool CanClear() => !IsReadOnly && _list.All(i => !i.IsReadOnly);

		public bool CanRemove(DataModelValue item)
		{
			if (IsReadOnly)
			{
				return false;
			}

			var index = _list.IndexOf(item);

			return index < 0 || _list.Skip(index).All(i => !i.IsReadOnly);
		}

		public bool CanSetLength(int value)
		{
			if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

			if (IsReadOnly)
			{
				return false;
			}

			return value >= Length || _list.Skip(value).All(i => !i.IsReadOnly);
		}

		public void SetLength(int value)
		{
			if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

			if (!CanSetLength(value))
			{
				throw ObjectCantBeModifiedException();
			}

			SetLengthInternal(value);
		}

		internal void SetLengthInternal(int value)
		{
			if (value < Length)
			{
				Changed?.Invoke(ChangedAction.SetLength, value, value: default);

				_list.RemoveRange(value, Length - value);
				_list.Capacity = value;
			}
			else if (value > Length)
			{
				Changed?.Invoke(ChangedAction.SetLength, value, value: default);

				_list.Capacity = value;
				_list.AddRange(Enumerable.Repeat(DataModelValue.Undefined(), value - Length));
			}
		}

		public bool CanInsert(int index, DataModelValue item)
		{
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

			return _list.Skip(index).All(i => !i.IsReadOnly);
		}

		public bool CanRemoveAt(int index)
		{
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

			if (IsReadOnly)
			{
				return false;
			}

			return _list.Skip(index).All(i => !i.IsReadOnly);
		}

		public bool CanSet(int index, DataModelValue value)
		{
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

			if (IsReadOnly)
			{
				return false;
			}

			return index >= _list.Count || !_list[index].IsReadOnly;
		}

		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			result = this[(int) indexes[0]].ToObject();

			return true;
		}

		public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
		{
			this[(int) indexes[0]] = DataModelValue.FromObject(value);

			return true;
		}
	}
}