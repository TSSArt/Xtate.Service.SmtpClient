using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Reflection;
using System.Text;

namespace TSSArt.StateMachine
{
	public enum DataModelValueType
	{
		Undefined,
		Null,
		String,
		Object,
		Array,
		Number,
		DateTime,
		Boolean
	}

	public readonly struct DataModelValue : IObject, IEquatable<DataModelValue>, IFormattable, IDynamicMetaObjectProvider
	{
		private static readonly DataModelValue UndefinedWritable = new DataModelValue();
		private static readonly DataModelValue UndefinedReadonly = new DataModelValue(true);

		private static readonly DataModelValue NullWritable = new DataModelValue((string) null);
		private static readonly DataModelValue NullReadonly = new DataModelValue((string) null, isReadOnly: true);

		private readonly object _value;
		private readonly long   _int64;

		private DataModelValue(bool isReadOnly = false)
		{
			Type = DataModelValueType.Undefined;
			_value = null;
			_int64 = 0;
			IsReadOnly = isReadOnly;
		}

		public DataModelValue(DataModelObject value, bool isReadOnly = false)
		{
			Type = value != null ? DataModelValueType.Object : DataModelValueType.Null;
			_value = value;
			_int64 = 0;
			IsReadOnly = isReadOnly;
		}

		public DataModelValue(DataModelArray value, bool isReadOnly = false)
		{
			Type = value != null ? DataModelValueType.Array : DataModelValueType.Null;
			_value = value;
			_int64 = 0;
			IsReadOnly = isReadOnly;
		}

		public DataModelValue(string value, bool isReadOnly = false)
		{
			Type = value != null ? DataModelValueType.String : DataModelValueType.Null;
			_value = value;
			_int64 = 0;
			IsReadOnly = isReadOnly;
		}

		public DataModelValue(double value, bool isReadOnly = false)
		{
			Type = DataModelValueType.Number;
			_value = null;
			_int64 = BitConverter.DoubleToInt64Bits(value);
			IsReadOnly = isReadOnly;
		}

		public DataModelValue(DateTime value, bool isReadOnly = false)
		{
			Type = DataModelValueType.DateTime;
			_value = null;
			_int64 = value.Ticks + ((long) value.Kind << 62);
			IsReadOnly = isReadOnly;
		}

		public DataModelValue(bool value, bool isReadOnly = false)
		{
			Type = DataModelValueType.Boolean;
			_value = null;
			_int64 = value ? 1 : 0;
			IsReadOnly = isReadOnly;
		}

		public bool IsReadOnly { get; }

		public DataModelValueType Type { get; }

		public bool Equals(DataModelValue other) => Equals(_value, other._value) && _int64 == other._int64 && Type == other.Type;

		public object ToObject()
		{
			return Type switch
			{
					DataModelValueType.Undefined => (object) null,
					DataModelValueType.Null => null,
					DataModelValueType.String => AsString(),
					DataModelValueType.Object => AsObject(),
					DataModelValueType.Array => AsArray(),
					DataModelValueType.Number => AsNumber(),
					DataModelValueType.DateTime => AsDateTime(),
					DataModelValueType.Boolean => AsBoolean(),
					_ => throw new ArgumentOutOfRangeException()
			};
		}

		public static DataModelValue Undefined(bool isReadOnly = false) => isReadOnly ? UndefinedReadonly : UndefinedWritable;
		public static DataModelValue Null(bool isReadOnly = false)      => isReadOnly ? NullReadonly : NullWritable;

		public DataModelObject AsObject()
		{
			if (Type == DataModelValueType.Object)
			{
				return (DataModelObject) _value;
			}

			throw new InvalidOperationException("DataModelValue is not DataModelObject");
		}

		public DataModelArray AsArray()
		{
			if (Type == DataModelValueType.Array)
			{
				return (DataModelArray) _value;
			}

			throw new InvalidOperationException("DataModelValue is not DataModelArray");
		}

		public string AsString()
		{
			if (Type == DataModelValueType.String)
			{
				return (string) _value;
			}

			throw new InvalidOperationException("DataModelValue is not String");
		}

		public double AsNumber()
		{
			if (Type == DataModelValueType.Number)
			{
				return BitConverter.Int64BitsToDouble(_int64);
			}

			throw new InvalidOperationException("DataModelValue is not Number");
		}

		public bool AsBoolean()
		{
			if (Type == DataModelValueType.Boolean)
			{
				return _int64 != 0;
			}

			throw new InvalidOperationException("DataModelValue is not Boolean");
		}

		public DateTime AsDateTime()
		{
			if (Type == DataModelValueType.DateTime)
			{
				return new DateTime(_int64 & 0x3FFFFFFFFFFFFFFF, (DateTimeKind) ((_int64 >> 62) & 3));
			}

			throw new InvalidOperationException("DataModelValue is not DateTime");
		}

		public override bool Equals(object obj) => obj is DataModelValue other && Equals(other);

		public override int GetHashCode() => (_value != null ? _value.GetHashCode() : 0) + _int64.GetHashCode();

		public DataModelValue DeepClone(bool isReadOnly = false)
		{
			switch (Type)
			{
				case DataModelValueType.Undefined: return new DataModelValue(isReadOnly);
				case DataModelValueType.Null: return new DataModelValue((string) null, isReadOnly);
				case DataModelValueType.String: return new DataModelValue(AsString(), isReadOnly);
				case DataModelValueType.Number: return new DataModelValue(AsNumber(), isReadOnly);
				case DataModelValueType.DateTime: return new DataModelValue(AsDateTime(), isReadOnly);
				case DataModelValueType.Boolean: return new DataModelValue(AsBoolean(), isReadOnly);
				case DataModelValueType.Object: return new DataModelValue(AsObject().DeepClone(isReadOnly), isReadOnly);
				case DataModelValueType.Array: return new DataModelValue(AsArray().DeepClone(isReadOnly), isReadOnly);
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public static DataModelValue FromContent(string content, ContentType contentType, bool isReadOnly = false) => new DataModelValue(content, isReadOnly);

		public static DataModelValue FromInlineContent(string content, bool isReadOnly = false) => new DataModelValue(content, isReadOnly);

		public static DataModelValue FromObject(object obj, bool isReadOnly = false)
		{
			if (obj == null)
			{
				return Null(isReadOnly);
			}

			switch (System.Type.GetTypeCode(obj.GetType()))
			{
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return new DataModelValue(Convert.ToDouble(obj), isReadOnly);
				case TypeCode.Boolean: return new DataModelValue((bool) obj, isReadOnly);
				case TypeCode.DateTime: return new DataModelValue((DateTime) obj, isReadOnly);
				case TypeCode.String: return new DataModelValue((string) obj, isReadOnly);
				case TypeCode.Object when obj is DataModelValue dataModelValue:
					return dataModelValue;
				case TypeCode.Object when obj is DataModelObject dataModelObject:
					return new DataModelValue(dataModelObject, isReadOnly);
				case TypeCode.Object when obj is DataModelArray dataModelArray:
					return new DataModelValue(dataModelArray, isReadOnly);
				case TypeCode.Object when obj is IDictionary<string, object> dictionary:
					return CreateDataModelObject(dictionary, isReadOnly);
				case TypeCode.Object when obj is IEnumerable array:
					return CreateDataModelArray(array, isReadOnly);
				default: throw new ArgumentException(message: "Unsupported object type", nameof(obj));
			}
		}

		private static DataModelValue CreateDataModelObject(IDictionary<string, object> dictionary, bool isReadOnly)
		{
			var obj = new DataModelObject();

			foreach (var pair in dictionary)
			{
				obj.SetInternal(pair.Key, FromObject(pair.Value, isReadOnly));
			}

			if (isReadOnly)
			{
				obj.Freeze();
			}

			return new DataModelValue(obj, isReadOnly);
		}

		private static DataModelValue CreateDataModelArray(IEnumerable array, bool isReadOnly)
		{
			var arr = new DataModelArray();

			foreach (var val in array)
			{
				arr.Add(FromObject(val, isReadOnly));
			}

			if (isReadOnly)
			{
				arr.Freeze();
			}

			return new DataModelValue(arr, isReadOnly);
		}

		public static DataModelValue FromEvent(IEvent @event, bool isReadOnly = false)
		{
			var eventObject = new DataModelObject
							  {
									  ["name"] = new DataModelValue(string.Join(separator: '.', @event.NameParts), isReadOnly),
									  ["type"] = new DataModelValue(GetTypeString(@event.Type), isReadOnly),
									  ["sendid"] = new DataModelValue(@event.SendId, isReadOnly),
									  ["origin"] = new DataModelValue(@event.Origin?.ToString(), isReadOnly),
									  ["origintype"] = new DataModelValue(@event.OriginType?.ToString(), isReadOnly),
									  ["invokeid"] = new DataModelValue(@event.InvokeId, isReadOnly),
									  ["data"] = @event.Data.DeepClone(isReadOnly)
							  };

			if (isReadOnly)
			{
				eventObject.Freeze();
			}

			return new DataModelValue(eventObject, isReadOnly);

			static string GetTypeString(EventType eventType)
			{
				switch (eventType)
				{
					case EventType.Platform: return "platform";
					case EventType.Internal: return "internal";
					case EventType.External: return "external";
					default: throw new ArgumentOutOfRangeException(nameof(eventType), eventType, message: null);
				}
			}
		}

		public static DataModelValue FromException(Exception exception, bool isReadOnly = false)
		{
			var exceptionData = new DataModelObject
								{
										["typeName"] = new DataModelValue(exception.GetType().Name, isReadOnly),
										["typeFullName"] = new DataModelValue(exception.GetType().FullName, isReadOnly),
										["message"] = new DataModelValue(exception.Message, isReadOnly),
										["text"] = new DataModelValue(exception.ToString(), isReadOnly)
								};

			if (isReadOnly)
			{
				exceptionData.Freeze();
			}

			return new DataModelValue(exceptionData, isReadOnly);
		}

		public override string ToString() => (ToObject() ?? string.Empty).ToString();

		public string ToString(string format) => ToString(format, formatProvider: null);

		public string ToString(string format, IFormatProvider formatProvider)
		{
			if (format == "JSON")
			{
				return Type switch
				{
						DataModelValueType.Undefined => "null",
						DataModelValueType.Null => "null",
						DataModelValueType.String => ToJsonString(AsString()),
						DataModelValueType.Object => AsObject().ToString(format: "JSON"),
						DataModelValueType.Array => AsArray().ToString(format: "JSON"),
						DataModelValueType.Number => AsNumber().ToString(format: "G17", CultureInfo.InvariantCulture),
						DataModelValueType.DateTime => AsDateTime().ToString(format: "O", CultureInfo.InvariantCulture),
						DataModelValueType.Boolean => (AsBoolean() ? "true" : "false"),
						_ => throw new ArgumentOutOfRangeException()
				};
			}

			var obj = ToObject();
			if (obj is IFormattable formattable)
			{
				return formattable.ToString(format, formatProvider);
			}

			return (obj ?? string.Empty).ToString();

			static string ToJsonString(string str)
			{
				if (str == null) throw new ArgumentNullException(nameof(str));

				var sb = new StringBuilder(str.Length + 2);
				sb.Append('\"');
				foreach (var c in str)
				{
					switch (c)
					{
						case '\\':
							sb.Append("\\\\");
							break;
						case '"':
							sb.Append("\\\"");
							break;
						case '/':
							sb.Append("\\/");
							break;
						case '\b':
							sb.Append("\\b");
							break;
						case '\t':
							sb.Append("\\t");
							break;
						case '\n':
							sb.Append("\\n");
							break;
						case '\f':
							sb.Append("\\f");
							break;
						case '\r':
							sb.Append("\\r");
							break;
						case var ctrl when char.IsControl(ctrl):
							sb.AppendFormat("\\u").Append(((int) c).ToString("X4"));
							break;
						default:
							sb.Append(c);
							break;
					}
				}

				sb.Append('\"');

				return sb.ToString();
			}
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this);

		private class MetaObject : DynamicMetaObject
		{
			private static readonly Dynamic         DynamicInstance        = new Dynamic(default);
			private static readonly ConstructorInfo DynamicConstructorInfo = typeof(Dynamic).GetConstructor(new[] { typeof(DataModelValue) });

			private readonly DynamicMetaObject _obj;

			public MetaObject(Expression expression, DataModelValue val) : base(expression, BindingRestrictions.Empty, val)
			{
				var newExpression = Expression.New(DynamicConstructorInfo, Expression.Convert(expression, typeof(DataModelValue)));
				_obj = DynamicInstance.GetMetaObject(newExpression);
			}

			public override DynamicMetaObject BindGetMember(GetMemberBinder binder) => _obj.BindGetMember(binder);

			public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) => _obj.BindSetMember(binder, value);

			public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder) => _obj.BindDeleteMember(binder);

			public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes) => _obj.BindGetIndex(binder, indexes);

			public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value) => _obj.BindSetIndex(binder, indexes, value);

			public override DynamicMetaObject BindDeleteIndex(DeleteIndexBinder binder, DynamicMetaObject[] indexes) => _obj.BindDeleteIndex(binder, indexes);

			public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) => _obj.BindInvokeMember(binder, args);

			public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args) => _obj.BindInvoke(binder, args);

			public override DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args) => _obj.BindCreateInstance(binder, args);

			public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder) => _obj.BindUnaryOperation(binder);

			public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg) => _obj.BindBinaryOperation(binder, arg);

			public override DynamicMetaObject BindConvert(ConvertBinder binder) => _obj.BindConvert(binder);
		}

		private class Dynamic : DynamicObject
		{
			private readonly DataModelValue _value;

			public Dynamic(DataModelValue value) => _value = value;

			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				if (_value.Type == DataModelValueType.Object)
				{
					result = _value.AsObject()[binder.Name].ToObject();

					return true;
				}

				result = null;

				return false;
			}

			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				if (_value.Type == DataModelValueType.Object)
				{
					_value.AsObject()[binder.Name] = FromObject(value);

					return true;
				}

				return false;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
			{
				if (indexes.Length == 1 && indexes[0] is string key && _value.Type == DataModelValueType.Object)
				{
					result = _value.AsObject()[key].ToObject();

					return true;
				}

				if (indexes.Length == 1 && indexes[0] is IConvertible convertible && _value.Type == DataModelValueType.Array)
				{
					result = _value.AsArray()[convertible.ToInt32(null)].ToObject();

					return true;
				}

				result = null;

				return false;
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
			{
				if (indexes.Length == 1 && indexes[0] is string key && _value.Type == DataModelValueType.Object)
				{
					_value.AsObject()[key] = FromObject(value);

					return true;
				}

				if (indexes.Length == 1 && indexes[0] is IConvertible convertible && _value.Type == DataModelValueType.Array)
				{
					_value.AsArray()[convertible.ToInt32(null)] = FromObject(value);

					return true;
				}

				return false;
			}

			public override bool TryConvert(ConvertBinder binder, out object result)
			{
				var typeCode = System.Type.GetTypeCode(binder.Type);
				switch (typeCode)
				{
					case TypeCode.Boolean:
						result = _value.AsBoolean();
						return true;

					case TypeCode.DateTime:
						result = _value.AsDateTime();
						return true;

					case TypeCode.String:
						result = _value.AsString();
						return true;

					case TypeCode.Byte:
					case TypeCode.Decimal:
					case TypeCode.Double:
					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.SByte:
					case TypeCode.Single:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
						result = Convert.ChangeType(_value.AsNumber(), typeCode);
						return true;
				}

				result = null;

				return false;
			}
		}
	}
}