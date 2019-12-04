using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Security;
using System.Text;

namespace TSSArt.StateMachine
{
	public sealed class DataModelObject : IDynamicMetaObjectProvider, IFormattable
	{
		public delegate void ChangedHandler(ChangedAction action, string property, DataModelValue value);

		public enum ChangedAction
		{
			Set,
			Remove
		}

		private readonly Dictionary<string, DataModelValue> _properties = new Dictionary<string, DataModelValue>();

		public DataModelObject() : this(false) { }

		public DataModelObject(bool isReadOnly) => IsReadOnly = isReadOnly;

		public bool IsReadOnly { get; private set; }

		public ICollection<string> Properties => _properties.Keys;

		public DataModelValue this[string property]
		{
			get
			{
				if (property == null) throw new ArgumentNullException(nameof(property));

				return _properties.TryGetValue(property, out var value) ? value : DataModelValue.Undefined(IsReadOnly);
			}
			set
			{
				if (property == null) throw new ArgumentNullException(nameof(property));

				if (!CanSet(property))
				{
					throw ObjectCantBeModifiedException();
				}

				SetInternal(property, value);
			}
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this, Dynamic.CreateMetaObject);

		public string ToString(string format, IFormatProvider formatProvider)
		{
			if (format == "JSON")
			{
				var sb = new StringBuilder();

				if (_properties.Count > 0)
				{
					foreach (var pair in _properties)
					{
						if (pair.Value.Type != DataModelValueType.Undefined && pair.Value.Type != DataModelValueType.Null)
						{
							sb.Append(sb.Length == 0 ? "{\r\n  " : ",\r\n  ");

							var value = pair.Value.ToString(format: "JSON", formatProvider).Replace(oldValue: "\r\n", newValue: "\r\n  ");
							sb.Append("\"").Append(pair.Key).Append("\": ").Append(value);
						}
					}

					sb.Append("\r\n}");
				}
				else
				{
					sb.Append("{}");
				}

				return sb.ToString();
			}

			return base.ToString();
		}

		public event ChangedHandler Changed;

		public void Freeze() => IsReadOnly = true;

		private static Exception ObjectCantBeModifiedException() => new SecurityException("Object can not be modified");

		internal void SetInternal(string property, DataModelValue value)
		{
			if (property == null) throw new ArgumentNullException(nameof(property));

			if (_properties.TryGetValue(property, out var oldValue))
			{
				Changed?.Invoke(ChangedAction.Remove, property, oldValue);
			}

			_properties[property] = value;

			Changed?.Invoke(ChangedAction.Set, property, value);
		}

		internal void RemoveInternal(string property)
		{
			if (property == null) throw new ArgumentNullException(nameof(property));

			if (_properties.TryGetValue(property, out var oldValue))
			{
				Changed?.Invoke(ChangedAction.Remove, property, oldValue);
			}

			_properties.Remove(property);
		}

		public bool Contains(string property)
		{
			if (property == null) throw new ArgumentNullException(nameof(property));

			return _properties.ContainsKey(property);
		}

		public bool CanSet(string property) => !IsReadOnly && !this[property].IsReadOnly;

		public bool CanRemove(string property) => !IsReadOnly && !this[property].IsReadOnly;

		public void Remove(string property)
		{
			if (!CanRemove(property))
			{
				throw ObjectCantBeModifiedException();
			}

			RemoveInternal(property);
		}

		public string ToString(string format) => ToString(format, formatProvider: null);

		public DataModelObject DeepClone(bool isReadOnly)
		{
			var clone = new DataModelObject(isReadOnly);

			foreach (var pair in _properties)
			{
				clone._properties[pair.Key] = pair.Value.DeepClone(isReadOnly);
			}

			return clone;
		}

		private class Dynamic : DynamicObject
		{
			private static readonly IDynamicMetaObjectProvider Instance = new Dynamic(default);

			private static readonly ConstructorInfo ConstructorInfo = typeof(Dynamic).GetConstructor(new[] { typeof(DataModelObject) });

			private readonly DataModelObject _obj;

			public Dynamic(DataModelObject obj) => _obj = obj;

			public static DynamicMetaObject CreateMetaObject(Expression expression)
			{
				var newExpression = Expression.New(ConstructorInfo, Expression.Convert(expression, typeof(DataModelObject)));
				return Instance.GetMetaObject(newExpression);
			}

			public override bool TryGetMember(GetMemberBinder binder, out object result)
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

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
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

			public override bool TryConvert(ConvertBinder binder, out object result)
			{
				result = _obj;

				return binder.Type == typeof(DataModelObject);
			}

			public override IEnumerable<string> GetDynamicMemberNames() => _obj.Properties;
		}
	}
}