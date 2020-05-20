using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Xtate.Annotations;

namespace Xtate
{
	[PublicAPI]
	[DebuggerTypeProxy(typeof(DebugView))]
	[DebuggerDisplay(value: "Count = {_properties.Count}")]
	public sealed class DataModelObject : IDynamicMetaObjectProvider, IFormattable
	{
		public delegate void ChangedHandler(ChangedAction action, string property, DataModelDescriptor descriptor);

		public enum ChangedAction
		{
			Set,
			Remove
		}

		public static readonly DataModelObject Empty = new DataModelObject(DataModelAccess.Constant, capacity: 0);

		private readonly Dictionary<string, DataModelDescriptor> _properties;

		private DataModelAccess _access;

		public DataModelObject() : this(capacity: 0) { }

		public DataModelObject(int capacity) : this(DataModelAccess.Writable, capacity) { }

		internal DataModelObject(bool isReadOnly, int capacity) : this(isReadOnly ? DataModelAccess.ReadOnly : DataModelAccess.Writable, capacity) { }

		private DataModelObject(DataModelAccess access, int capacity)
		{
			_access = access;
			_properties = new Dictionary<string, DataModelDescriptor>(capacity);
		}

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

					foreach (var pair in _properties)
					{
						pair.Value.Value.MakeDeepConstant();
					}

					return;
				}

				throw new InfrastructureException(Resources.Exception_Access_can_t_be_changed);
			}
		}

		public ICollection<string> Properties => _properties.Keys;

		public int Count => _properties.Count;

		public DataModelValue this[string property]
		{
			get => GetDescriptor(property).Value;
			set => SetProperty(property, new DataModelDescriptor(value), DataModelAccess.Writable, throwOnDeny: true);
		}

	#region Interface IDynamicMetaObjectProvider

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this, Dynamic.CreateMetaObject);

	#endregion

	#region Interface IFormattable

		public string ToString(string? format, IFormatProvider? formatProvider)
		{
			var sb = new StringBuilder();

			sb.Append('(');
			foreach (var pair in _properties)
			{
				if (sb.Length > 1)
				{
					sb.Append(',');
				}

				sb.Append(pair.Key).Append('=').Append(pair.Value.Value.ToString(format: null, formatProvider));
			}

			sb.Append(')');

			return sb.ToString();
		}

	#endregion

		public void EnsureCapacity(int capacity)
		{
			if (_access == DataModelAccess.Constant)
			{
				return;
			}

#if NETSTANDARD2_1
			_properties.EnsureCapacity(capacity);
#else
			var _ = _properties;
			var __ = capacity;
#endif
		}

		public event ChangedHandler? Changed;

		public void MakeReadOnly() => Access = DataModelAccess.ReadOnly;

		public void MakeDeepConstant() => Access = DataModelAccess.Constant;

		public DataModelObject CloneAsWritable() => DeepClone(DataModelAccess.Writable);

		public DataModelObject CloneAsReadOnly() => DeepClone(DataModelAccess.ReadOnly);

		public DataModelObject AsConstant() => DeepClone(DataModelAccess.Constant);

		internal DataModelDescriptor GetDescriptor(string property) => _properties.TryGetValue(property, out var descriptor) ? descriptor : default;

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

		public bool CanSet(string property) => SetProperty(property, descriptor: default, DataModelAccess.Constant, throwOnDeny: false);

		private bool SetProperty(string property, DataModelDescriptor descriptor, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			if (_properties.TryGetValue(property, out var oldDescriptor))
			{
				if (NoAccess(oldDescriptor.Access, requestedAccess, throwOnDeny))
				{
					return false;
				}

				if (requestedAccess != DataModelAccess.Constant)
				{
					Changed?.Invoke(ChangedAction.Remove, property, oldDescriptor);
				}
			}

			if (requestedAccess != DataModelAccess.Constant)
			{
				_properties[property] = descriptor;

				Changed?.Invoke(ChangedAction.Set, property, descriptor);
			}

			return true;
		}

		public bool CanRemove(string property) => RemoveProperty(property, DataModelAccess.Constant, throwOnDeny: false);

		private bool RemoveProperty(string property, DataModelAccess requestedAccess, bool throwOnDeny)
		{
			if (NoAccess(_access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			if (!_properties.TryGetValue(property, out var oldDescriptor))
			{
				return false;
			}

			if (NoAccess(oldDescriptor.Access, requestedAccess, throwOnDeny))
			{
				return false;
			}

			if (requestedAccess != DataModelAccess.Constant)
			{
				Changed?.Invoke(ChangedAction.Remove, property, oldDescriptor);

				_properties.Remove(property);
			}

			return true;
		}

		internal bool SetInternal(string property, DataModelDescriptor descriptor, bool throwOnDeny = true) => SetProperty(property, descriptor, DataModelAccess.ReadOnly, throwOnDeny);

		internal bool RemoveInternal(string property, bool throwOnDeny = true) => RemoveProperty(property, DataModelAccess.ReadOnly, throwOnDeny);

		public bool Contains(string property) => _properties.ContainsKey(property);

		public void Remove(string property) => RemoveProperty(property, DataModelAccess.Writable, throwOnDeny: true);

		public DataModelObject DeepClone(DataModelAccess targetAccess)
		{
			Dictionary<object, object>? map = null;

			return DeepCloneWithMap(targetAccess, ref map);
		}

		internal DataModelObject DeepCloneWithMap(DataModelAccess targetAccess, ref Dictionary<object, object>? map)
		{
			if (targetAccess == DataModelAccess.Constant)
			{
				if (_properties.Count == 0)
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
				return (DataModelObject) val;
			}

			var clone = new DataModelObject(targetAccess, _properties.Count);

			map[this] = clone;

			foreach (var pair in _properties)
			{
				clone._properties[pair.Key] = new DataModelDescriptor(pair.Value.Value.DeepCloneWithMap(targetAccess, ref map), targetAccess);
			}

			return clone;
		}

		public override string ToString() => ToString(format: null, formatProvider: null);

		[DebuggerDisplay(value: "{" + nameof(_value) + "}", Name = "{" + nameof(_name) + ",nq}")]
		private struct NameValue
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly string _name;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			private readonly DataModelValue _value;

			public NameValue(string name, DataModelValue value)
			{
				_name = name;
				_value = value;
			}
		}

		[PublicAPI]
		[ExcludeFromCodeCoverage]
		private class DebugView
		{
			private readonly DataModelObject _dataModelObject;

			public DebugView(DataModelObject dataModelObject) => _dataModelObject = dataModelObject;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public NameValue[] Items =>
					_dataModelObject
							._properties.OrderBy(p => p.Key)
							.Select(p => new NameValue(p.Key, p.Value.Value))
							.ToArray();
		}

		private class Dynamic : DynamicObject
		{
			private static readonly IDynamicMetaObjectProvider Instance = new Dynamic(default!);

			private static readonly ConstructorInfo ConstructorInfo = typeof(Dynamic).GetConstructor(new[] { typeof(DataModelObject) })!;

			private readonly DataModelObject _obj;

			public Dynamic(DataModelObject obj) => _obj = obj;

			public static DynamicMetaObject CreateMetaObject(Expression expression)
			{
				var newExpression = Expression.New(ConstructorInfo, Expression.Convert(expression, typeof(DataModelObject)));
				return Instance.GetMetaObject(newExpression);
			}

			public override bool TryGetMember(GetMemberBinder binder, out object? result)
			{
				if (binder == null) throw new ArgumentNullException(nameof(binder));

				result = _obj[binder.Name].ToObject();

				return true;
			}

			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				if (binder == null) throw new ArgumentNullException(nameof(binder));

				_obj[binder.Name] = DataModelValue.FromObject(value);

				return true;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
			{
				if (indexes.Length == 1 && indexes[0] is string key)
				{
					result = _obj[key].ToObject();

					return true;
				}

				result = null;

				return false;
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
			{
				if (indexes.Length == 1 && indexes[0] is string key)
				{
					_obj[key] = DataModelValue.FromObject(value);

					return true;
				}

				return false;
			}

			public override bool TryConvert(ConvertBinder binder, out object? result)
			{
				if (binder.Type == typeof(DataModelObject))
				{
					result = _obj;

					return true;
				}

				if (binder.Type == typeof(DataModelValue))
				{
					result = new DataModelValue(_obj);

					return true;
				}

				result = default;

				return false;
			}

			public override IEnumerable<string> GetDynamicMemberNames() => _obj.Properties;
		}
	}
}