using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Security;
using System.Text;

namespace TSSArt.StateMachine
{
	public class DataModelObject : DynamicObject, IFormattable
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

		public string ToString(string format, IFormatProvider formatProvider)
		{
			if (format == "JSON")
			{
				var sb = new StringBuilder();

				foreach (var pair in _properties)
				{
					sb.Append(sb.Length == 0 ? "{ " : ", ");

					sb.Append(pair.Key).Append(" : ").Append(pair.Value.ToString(format: "JSON", formatProvider));
				}

				sb.Append(sb.Length == 0 ? "{}" : " }");

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

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = this[binder.Name].ToObject();

			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			this[binder.Name] = DataModelValue.FromObject(value);

			return true;
		}

		public override IEnumerable<string> GetDynamicMemberNames() => Properties;

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
	}
}