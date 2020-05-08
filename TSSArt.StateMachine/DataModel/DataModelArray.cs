using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using TSSArt.StateMachine.Annotations;

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

		public static readonly DataModelArray Empty = new DataModelArray(DataModelAccess.Constant, capacity: 0);

		private readonly List<DataModelDescriptor> _list;

		private DataModelAccess _access;

		public DataModelArray() : this(capacity: 0) { }

		public DataModelArray(int capacity) : this(DataModelAccess.Writable, capacity) { }

		internal DataModelArray(bool isReadOnly, int capacity) : this(isReadOnly ? DataModelAccess.ReadOnly : DataModelAccess.Writable, capacity) { }

		private DataModelArray(DataModelAccess access, int capacity)
		{
			_access = access;
			_list = new List<DataModelDescriptor>(capacity);
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

		public DataModelAccess Access
		{
			get => _access;

			internal set
			{
				if (value == _access)
				{
					return;
				}

				if (value == DataModelAccess.ReadOnly && _access == DataModelAccess.Writable)
				{
					_access = DataModelAccess.ReadOnly;

					return;
				}

				if (value == DataModelAccess.Constant)
				{
					_access = DataModelAccess.Constant;

					foreach (var val in _list)
					{
						val.Value.MakeDeepConstant();
					}

					return;
				}

				throw new StateMachineInfrastructureException(Resources.Exception_Access_can_t_be_changed);
			}
		}

	#region Interface ICollection<DataModelValue>

		public void Add(DataModelValue item) => AddItem(new DataModelDescriptor(item), DataModelAccess.Writable, throwOnDeny: true);

		public void Clear() => ClearItems(DataModelAccess.Writable, throwOnDeny: true);

		public bool Contains(DataModelValue item) => _list.Contains(new DataModelDescriptor(item));

		public void CopyTo(DataModelValue[] array, int arrayIndex)
		{
			if (array == null) throw new ArgumentNullException(nameof(array));

			foreach (var descriptor in _list)
			{
				array[arrayIndex ++] = descriptor.Value;
			}
		}

		public bool Remove(DataModelValue item) => RemoveItem(new DataModelDescriptor(item), DataModelAccess.Writable, throwOnDeny: true);

		bool ICollection<DataModelValue>.IsReadOnly => _access != DataModelAccess.Writable;

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
			get => GetDescriptor(index).Value;

			set => SetItem(index, new DataModelDescriptor(value), DataModelAccess.Writable, throwOnDeny: true);
		}

		public int IndexOf(DataModelValue item) => _list.IndexOf(new DataModelDescriptor(item));

		public void Insert(int index, DataModelValue item) => InsertItem(index, new DataModelDescriptor(item), DataModelAccess.Writable, throwOnDeny: true);

		public void RemoveAt(int index) => RemoveAtItem(index, DataModelAccess.Writable, throwOnDeny: true);

	#endregion

		public void EnsureCapacity(int capacity)
		{
			if (_access == DataModelAccess.Constant)
			{
				return;
			}

			if (capacity > _list.Capacity)
			{
				_list.Capacity = capacity;
			}
		}

		internal DataModelDescriptor GetDescriptor(int index) => index < _list.Count ? _list[index] : new DataModelDescriptor(DataModelValue.Undefined);

		public event ChangedHandler? Changed;

		public void MakeReadOnly() => Access = DataModelAccess.ReadOnly;

		public void MakeDeepConstant() => Access = DataModelAccess.Constant;

		public DataModelArray CloneAsWritable() => DeepClone(DataModelAccess.Writable);

		public DataModelArray CloneAsReadOnly() => DeepClone(DataModelAccess.ReadOnly);

		public DataModelArray AsConstant() => DeepClone(DataModelAccess.Constant);

		public bool CanAdd() => AddItem(descriptor: default, DataModelAccess.Constant, throwOnDeny: false);

		public bool CanClear() => ClearItems(DataModelAccess.Constant, throwOnDeny: false);

		public bool CanRemove(DataModelValue item) => RemoveItem(new DataModelDescriptor(item), DataModelAccess.Constant, throwOnDeny: false);

		public bool CanInsert(int index) => InsertItem(index, descriptor: default, DataModelAccess.Constant, throwOnDeny: false);

		public bool CanRemoveAt(int index) => RemoveAtItem(index, DataModelAccess.Constant, throwOnDeny: false);

		public bool CanSet(int index) => SetItem(index, descriptor: default, DataModelAccess.Constant, throwOnDeny: false);

		public bool CanSetLength(int value) => SetLengthItems(value, DataModelAccess.Constant, throwOnDeny: false);

		internal bool AddInternal(DataModelDescriptor descriptor, bool throwOnDeny = true) => AddItem(descriptor, DataModelAccess.ReadOnly, throwOnDeny);

		internal bool ClearInternal(bool throwOnDeny = true) => ClearItems(DataModelAccess.ReadOnly, throwOnDeny);

		internal bool RemoveInternal(DataModelDescriptor descriptor, bool throwOnDeny = true) => RemoveItem(descriptor, DataModelAccess.ReadOnly, throwOnDeny);

		internal bool InsertInternal(int index, DataModelDescriptor descriptor, bool throwOnDeny = true) => InsertItem(index, descriptor, DataModelAccess.ReadOnly, throwOnDeny);

		internal bool RemoveAtInternal(int index, bool throwOnDeny = true) => RemoveAtItem(index, DataModelAccess.ReadOnly, throwOnDeny);

		internal bool SetInternal(int index, DataModelDescriptor descriptor, bool throwOnDeny = true) => SetItem(index, descriptor, DataModelAccess.ReadOnly, throwOnDeny);

		internal bool SetLengthInternal(int value, bool throwOnDeny = true) => SetLengthItems(value, DataModelAccess.ReadOnly, throwOnDeny);

		public void SetLength(int value) => SetLengthItems(value, DataModelAccess.Writable, throwOnDeny: true);

		private bool AddItem(DataModelDescriptor descriptor, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			if (requestedAccess != DataModelAccess.Constant)
			{
				_list.Add(descriptor);

				Changed?.Invoke(ChangedAction.Set, _list.Count - 1, descriptor);
			}

			return true;
		}

		private bool ClearItems(DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			foreach (var descriptor in _list)
			{
				if (NoAccess(descriptor.Access, requestedAccess, throwOnDeny))
				{
					return false;
				}
			}

			if (requestedAccess != DataModelAccess.Constant)
			{
				Changed?.Invoke(ChangedAction.Clear, index: default, descriptor: default);

				_list.Clear();
			}

			return true;
		}

		private bool RemoveItem(DataModelDescriptor descriptor, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			var index = _list.IndexOf(descriptor);

			if (index >= 0)
			{
				for (var i = index; i < _list.Count; i ++)
				{
					if (NoAccess(_list[i].Access, requestedAccess, throwOnDeny))
					{
						return false;
					}
				}

				if (requestedAccess != DataModelAccess.Constant)
				{
					Changed?.Invoke(ChangedAction.Remove, index, descriptor);

					_list.RemoveAt(index);
				}

				return true;
			}

			return false;
		}

		private bool InsertItem(int index, DataModelDescriptor descriptor, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			for (var i = index; i < _list.Count; i ++)
			{
				if (NoAccess(_list[i].Access, requestedAccess, throwOnDeny))
				{
					return false;
				}
			}

			if (requestedAccess != DataModelAccess.Constant)
			{
				if (index > Length)
				{
					SetLength(index);
				}

				_list.Insert(index, descriptor);

				Changed?.Invoke(ChangedAction.Insert, index, descriptor);
			}

			return true;
		}

		private bool RemoveAtItem(int index, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			for (var i = index; i < _list.Count; i ++)
			{
				if (NoAccess(_list[i].Access, requestedAccess, throwOnDeny))
				{
					return false;
				}
			}

			if (index < _list.Count)
			{
				if (requestedAccess != DataModelAccess.Constant)
				{
					Changed?.Invoke(ChangedAction.Remove, index, _list[index]);

					_list.RemoveAt(index);
				}

				return true;
			}

			return false;
		}

		private bool SetItem(int index, DataModelDescriptor descriptor, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			if (index < _list.Count)
			{
				if (NoAccess(_list[index].Access, requestedAccess, throwOnDeny))
				{
					return false;
				}

				if (requestedAccess != DataModelAccess.Constant)
				{
					Changed?.Invoke(ChangedAction.Remove, index, _list[index]);

					_list[index] = descriptor;

					Changed?.Invoke(ChangedAction.Set, index, descriptor);
				}

				return true;
			}

			if (requestedAccess != DataModelAccess.Constant)
			{
				if (index > _list.Count)
				{
					_list.Capacity = index + 1;

					while (index > _list.Count)
					{
						_list.Add(default);
					}
				}

				_list.Add(descriptor);

				Changed?.Invoke(ChangedAction.Set, index, descriptor);
			}

			return true;
		}

		private bool SetLengthItems(int value, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			if (value < Length)
			{
				for (var i = value; i < _list.Count; i ++)
				{
					if (NoAccess(_list[i].Access, requestedAccess, throwOnDeny))
					{
						return false;
					}
				}

				if (requestedAccess != DataModelAccess.Constant)
				{
					Changed?.Invoke(ChangedAction.SetLength, value, descriptor: default);

					_list.RemoveRange(value, Length - value);
					_list.Capacity = value;
				}
			}
			else if (value > Length)
			{
				if (requestedAccess != DataModelAccess.Constant)
				{
					Changed?.Invoke(ChangedAction.SetLength, value, descriptor: default);

					_list.Capacity = value;

					while (value > _list.Count)
					{
						_list.Add(default);
					}
				}
			}

			return true;
		}

		private static bool NoAccess(DataModelAccess objectAccess, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (objectAccess == DataModelAccess.Writable)
			{
				return false;
			}

			if (objectAccess != DataModelAccess.Constant && requestedAccess == DataModelAccess.ReadOnly)
			{
				return false;
			}

			if (throwOnDeny)
			{
				throw new InvalidOperationException(Resources.Exception_Object_can_not_be_modified);
			}

			return true;
		}

		public DataModelArray DeepClone(DataModelAccess targetAccess)
		{
			Dictionary<object, object>? map = null;

			return DeepCloneWithMap(targetAccess, ref map);
		}

		internal DataModelArray DeepCloneWithMap(DataModelAccess targetAccess, ref Dictionary<object, object>? map)
		{
			if (targetAccess == DataModelAccess.Constant)
			{
				if (_list.Count == 0)
				{
					return Empty;
				}

				if (_access == DataModelAccess.Constant)
				{
					return this;
				}
			}

			map ??= new Dictionary<object, object>();

			if (map.TryGetValue(this, out var val))
			{
				return (DataModelArray) val;
			}

			var clone = new DataModelArray(targetAccess, _list.Count);

			map[this] = clone;

			foreach (var item in _list)
			{
				clone._list.Add(new DataModelDescriptor(item.Value.DeepCloneWithMap(targetAccess, ref map), targetAccess));
			}

			return clone;
		}

		public override string ToString() => ToString(format: null, formatProvider: null);

		[ExcludeFromCodeCoverage]
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

		[ExcludeFromCodeCoverage]
		[PublicAPI]
		private class DebugView
		{
			private readonly DataModelArray _dataModelArray;

			public DebugView(DataModelArray dataModelArray) => _dataModelArray = dataModelArray;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public IndexValue[] Items => _dataModelArray._list.Select((d, i) => new IndexValue(i, d)).ToArray();
		}

		private class Dynamic : DynamicObject
		{
			private static readonly IDynamicMetaObjectProvider Instance = new Dynamic(default!);

			private static readonly ConstructorInfo ConstructorInfo = typeof(Dynamic).GetConstructor(new[] { typeof(DataModelArray) })!;

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