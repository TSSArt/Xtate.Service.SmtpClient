using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	[DebuggerTypeProxy(typeof(DebugView))]
	[DebuggerDisplay(value: "Count = {_list.Count}")]
	public sealed class DataModelArray : IDynamicMetaObjectProvider, IList<DataModelValue>, IFormattable
	{
		public delegate void ChangedHandler(ChangedAction action, int index, DataModelDescriptor descriptor);

		public enum ChangedAction
		{
			Set,
			Clear,
			Insert,
			Remove,
			SetLength
		}

		public static readonly DataModelArray Empty = new DataModelArray(State.Empty);

		private readonly List<DataModelDescriptor> _list;

		private State _state;

		public DataModelArray() : this(State.Writable) { }

		public DataModelArray(bool isReadOnly) : this(isReadOnly ? State.Readonly : State.Writable) { }

		public DataModelArray(int capacity) => _list = new List<DataModelDescriptor>(capacity);

		private DataModelArray(State state)
		{
			_state = state;
			_list = new List<DataModelDescriptor>();
		}

		public DataModelArray(IEnumerable<DataModelValue> items)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));

			_list = new List<DataModelDescriptor>(items is ICollection<DataModelValue> collection ? collection.Count : 0);

			foreach (var value in items)
			{
				_list.Add(new DataModelDescriptor(value));
			}
		}

		public int Length => _list.Count;

	#region Interface ICollection<DataModelValue>

		public void Add(DataModelValue item)
		{
			if (!CanAdd(item))
			{
				throw ObjectCantBeModifiedException();
			}

			AddInternal(new DataModelDescriptor(item));
		}

		public void Clear()
		{
			if (!CanClear())
			{
				throw ObjectCantBeModifiedException();
			}

			ClearInternal();
		}

		public bool Contains(DataModelValue item) => _list.Contains(new DataModelDescriptor(item));

		public void CopyTo(DataModelValue[] array, int arrayIndex)
		{
			if (array == null) throw new ArgumentNullException(nameof(array));

			foreach (var descriptor in _list)
			{
				array[arrayIndex ++] = descriptor.Value;
			}
		}

		public bool Remove(DataModelValue item)
		{
			if (!CanRemove(item))
			{
				throw ObjectCantBeModifiedException();
			}

			return RemoveInternal(new DataModelDescriptor(item));
		}

		public bool IsReadOnly => _state != State.Writable;

		int ICollection<DataModelValue>.Count => _list.Count;

	#endregion

	#region Interface IDynamicMetaObjectProvider

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this, Dynamic.CreateMetaObject);

	#endregion

	#region Interface IEnumerable

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

	#region Interface IEnumerable<DataModelValue>

		public IEnumerator<DataModelValue> GetEnumerator()
		{
			foreach (var descriptor in _list)
			{
				yield return descriptor.Value;
			}
		}

	#endregion

	#region Interface IFormattable

		public string ToString(string? format, IFormatProvider? formatProvider)
		{
			var sb = new StringBuilder();

			sb.Append('[');
			foreach (var item in _list)
			{
				if (sb.Length > 1)
				{
					sb.Append(',');
				}

				sb.Append(item.Value.ToString(format: null, formatProvider));
			}

			sb.Append(']');

			return sb.ToString();
		}

	#endregion

	#region Interface IList<DataModelValue>

		public DataModelValue this[int index]
		{
			get
			{
				if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

				return GetDescriptor(index).Value;
			}
			set
			{
				if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

				if (!CanSet(index, value))
				{
					throw ObjectCantBeModifiedException();
				}

				SetInternal(index, new DataModelDescriptor(value));
			}
		}

		public int IndexOf(DataModelValue item) => _list.IndexOf(new DataModelDescriptor(item));

		public void Insert(int index, DataModelValue item)
		{
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

			if (!CanInsert(index, item))
			{
				throw ObjectCantBeModifiedException();
			}

			InsertInternal(index, new DataModelDescriptor(item));
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

	#endregion

		internal DataModelDescriptor GetDescriptor(int index) => index < _list.Count ? _list[index] : new DataModelDescriptor(DataModelValue.Undefined);

		public event ChangedHandler? Changed;

		public void Freeze() => _state = State.Readonly;

		internal void AddInternal(DataModelDescriptor descriptor)
		{
			if (_state == State.Empty)
			{
				throw ObjectCantBeModifiedException();
			}

			_list.Add(descriptor);

			Changed?.Invoke(ChangedAction.Set, _list.Count - 1, descriptor);
		}

		internal void ClearInternal()
		{
			if (_state == State.Empty)
			{
				throw ObjectCantBeModifiedException();
			}

			Changed?.Invoke(ChangedAction.Clear, index: default, descriptor: default);

			_list.Clear();
		}

		internal bool RemoveInternal(DataModelDescriptor descriptor)
		{
			if (_state == State.Empty)
			{
				throw ObjectCantBeModifiedException();
			}

			var index = _list.IndexOf(descriptor);

			if (index >= 0)
			{
				Changed?.Invoke(ChangedAction.Remove, index, descriptor);

				_list.RemoveAt(index);

				return true;
			}

			return false;
		}

		internal void InsertInternal(int index, DataModelDescriptor descriptor)
		{
			if (_state == State.Empty)
			{
				throw ObjectCantBeModifiedException();
			}

			if (index > Length)
			{
				SetLength(index);
			}

			_list.Insert(index, descriptor);

			Changed?.Invoke(ChangedAction.Insert, index, descriptor);
		}

		internal void RemoveAtInternal(int index)
		{
			if (_state == State.Empty)
			{
				throw ObjectCantBeModifiedException();
			}

			if (index < _list.Count)
			{
				Changed?.Invoke(ChangedAction.Remove, index, _list[index]);

				_list.RemoveAt(index);
			}
		}

		internal void SetInternal(int index, DataModelDescriptor descriptor)
		{
			if (_state == State.Empty)
			{
				throw ObjectCantBeModifiedException();
			}

			if (index < _list.Count)
			{
				Changed?.Invoke(ChangedAction.Remove, index, _list[index]);

				_list[index] = descriptor;

				Changed?.Invoke(ChangedAction.Set, index, descriptor);

				return;
			}

			if (index > _list.Count)
			{
				_list.Capacity = index + 1;
				_list.AddRange(Enumerable.Repeat(default(DataModelDescriptor), index - Length));
			}

			_list.Add(descriptor);

			Changed?.Invoke(ChangedAction.Set, index, descriptor);
		}

		private static Exception ObjectCantBeModifiedException() => new InvalidOperationException(Resources.Exception_Object_can_not_be_modified);

		public bool CanAdd(DataModelValue item)
		{
			var _ = item;

			return !IsReadOnly;
		}

		public bool CanClear() => !IsReadOnly && _list.All(i => !i.IsReadOnly);

		public bool CanRemove(DataModelValue item)
		{
			if (IsReadOnly)
			{
				return false;
			}

			var index = _list.IndexOf(new DataModelDescriptor(item));

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
			if (_state == State.Empty)
			{
				throw ObjectCantBeModifiedException();
			}

			if (value < Length)
			{
				Changed?.Invoke(ChangedAction.SetLength, value, descriptor: default);

				_list.RemoveRange(value, Length - value);
				_list.Capacity = value;
			}
			else if (value > Length)
			{
				Changed?.Invoke(ChangedAction.SetLength, value, descriptor: default);

				_list.Capacity = value;
				_list.AddRange(Enumerable.Repeat(new DataModelDescriptor(DataModelValue.Undefined), value - Length));
			}
		}

		public bool CanInsert(int index, DataModelValue item)
		{
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

			var _ = item;

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

			var _ = value;

			if (IsReadOnly)
			{
				return false;
			}

			return index >= _list.Count || !_list[index].IsReadOnly;
		}

		public DataModelArray DeepClone(bool isReadOnly)
		{
			if (isReadOnly)
			{
				if (_list.Count == 0)
				{
					return Empty;
				}

				if (IsDeepReadOnly())
				{
					return this;
				}
			}

			var clone = new DataModelArray(isReadOnly);

			foreach (var val in _list)
			{
				clone._list.Add(new DataModelDescriptor(val.Value.DeepClone(isReadOnly), isReadOnly));
			}

			return clone;
		}

		internal bool IsDeepReadOnly()
		{
			if (!IsReadOnly)
			{
				return false;
			}

			foreach (var val in _list)
			{
				if (!val.IsReadOnly)
				{
					return false;
				}

				if (!val.Value.IsDeepReadOnly())
				{
					return false;
				}
			}

			return true;
		}

		public override string ToString() => ToString(format: null, formatProvider: null);

		[DebuggerDisplay(value: "{" + nameof(_value) + "}", Name = "[{" + nameof(_index) + "}]")]
		private struct IndexValue
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly int _index;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			private readonly DataModelValue _value;

			public IndexValue(int index, DataModelDescriptor descriptor)
			{
				_index = index;
				_value = descriptor.Value;
			}
		}

		[PublicAPI]
		private class DebugView
		{
			private readonly DataModelArray _dataModelArray;

			public DebugView(DataModelArray dataModelArray) => _dataModelArray = dataModelArray;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public IndexValue[] Items => _dataModelArray._list.Select((d, i) => new IndexValue(i, d)).ToArray();
		}

		private enum State
		{
			Writable,
			Readonly,
			Empty
		}

		private class Dynamic : DynamicObject
		{
			private static readonly IDynamicMetaObjectProvider Instance = new Dynamic(default!);

			private static readonly ConstructorInfo ConstructorInfo = typeof(Dynamic).GetConstructor(new[] { typeof(DataModelArray) });

			private readonly DataModelArray _arr;

			public Dynamic(DataModelArray arr) => _arr = arr;

			public static DynamicMetaObject CreateMetaObject(Expression expression)
			{
				var newExpression = Expression.New(ConstructorInfo, Expression.Convert(expression, typeof(DataModelArray)));
				return Instance.GetMetaObject(newExpression);
			}

			public override bool TryGetMember(GetMemberBinder binder, out object? result)
			{
				if (binder == null) throw new ArgumentNullException(nameof(binder));

				if (binder.Name == @"Length" || binder.Name == @"length")
				{
					result = _arr.Length;

					return true;
				}

				result = null;

				return false;
			}

			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				if (binder == null) throw new ArgumentNullException(nameof(binder));

				if ((binder.Name == @"Length" || binder.Name == @"length") && value is IConvertible convertible)
				{
					_arr.SetLength(convertible.ToInt32(NumberFormatInfo.InvariantInfo));

					return true;
				}

				return false;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
			{
				if (indexes == null) throw new ArgumentNullException(nameof(indexes));

				if (indexes.Length == 1 && indexes[0] is IConvertible convertible)
				{
					result = _arr[convertible.ToInt32(NumberFormatInfo.InvariantInfo)].ToObject();

					return true;
				}

				result = null;

				return false;
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
			{
				if (indexes == null) throw new ArgumentNullException(nameof(indexes));

				if (indexes.Length == 1 && indexes[0] is IConvertible convertible)
				{
					_arr[convertible.ToInt32(NumberFormatInfo.InvariantInfo)] = DataModelValue.FromObject(value);

					return true;
				}

				return false;
			}

			public override bool TryConvert(ConvertBinder binder, out object? result)
			{
				if (binder.Type == typeof(DataModelArray))
				{
					result = _arr;

					return true;
				}

				if (binder.Type == typeof(DataModelValue))
				{
					result = new DataModelValue(_arr);

					return true;
				}

				result = null;

				return false;
			}
		}
	}
}