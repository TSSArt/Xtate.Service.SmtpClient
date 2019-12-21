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
		private readonly object _value;
		private readonly long   _int64;

		public DataModelValue(DataModelObject value)
		{
			Type = value != null ? DataModelValueType.Object : DataModelValueType.Null;
			_value = value;
			_int64 = 0;
		}

		public DataModelValue(DataModelArray value)
		{
			Type = value != null ? DataModelValueType.Array : DataModelValueType.Null;
			_value = value;
			_int64 = 0;
		}

		public DataModelValue(string value)
		{
			Type = value != null ? DataModelValueType.String : DataModelValueType.Null;
			_value = value;
			_int64 = 0;
		}

		public DataModelValue(double value)
		{
			Type = DataModelValueType.Number;
			_value = null;
			_int64 = BitConverter.DoubleToInt64Bits(value);
		}

		public DataModelValue(DateTime value)
		{
			Type = DataModelValueType.DateTime;
			_value = null;
			_int64 = value.Ticks + ((long) value.Kind << 62);
		}

		public DataModelValue(bool value)
		{
			Type = DataModelValueType.Boolean;
			_value = null;
			_int64 = value ? 1 : 0;
		}

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

		public static readonly DataModelValue Undefined = default;

		public static readonly DataModelValue Null = new DataModelValue((string) null);

		public DataModelObject AsObject()
		{
			if (Type == DataModelValueType.Object)
			{
				return (DataModelObject) _value;
			}

			throw new InvalidOperationException("DataModelValue is not DataModelObject");
		}

		public DataModelObject AsObjectOrEmpty()
		{
			if (Type == DataModelValueType.Object)
			{
				return (DataModelObject) _value;
			}

			return DataModelObject.Empty;
		}

		public DataModelArray AsArray()
		{
			if (Type == DataModelValueType.Array)
			{
				return (DataModelArray) _value;
			}

			throw new InvalidOperationException("DataModelValue is not DataModelArray");
		}

		public DataModelArray AsArrayOrEmpty()
		{
			if (Type == DataModelValueType.Array)
			{
				return (DataModelArray) _value;
			}

			return DataModelArray.Empty;
		}

		public string AsString()
		{
			if (Type == DataModelValueType.String)
			{
				return (string) _value;
			}

			throw new InvalidOperationException("DataModelValue is not String");
		}

		public string AsStringOrDefault()
		{
			if (Type == DataModelValueType.String)
			{
				return (string) _value;
			}

			return null;
		}

		public double AsNumber()
		{
			if (Type == DataModelValueType.Number)
			{
				return BitConverter.Int64BitsToDouble(_int64);
			}

			throw new InvalidOperationException("DataModelValue is not Number");
		}

		public double? AsNumberOrDefault()
		{
			if (Type == DataModelValueType.Number)
			{
				return BitConverter.Int64BitsToDouble(_int64);
			}

			return null;
		}

		public bool AsBoolean()
		{
			if (Type == DataModelValueType.Boolean)
			{
				return _int64 != 0;
			}

			throw new InvalidOperationException("DataModelValue is not Boolean");
		}

		public bool? AsBooleanOrDefault()
		{
			if (Type == DataModelValueType.Boolean)
			{
				return _int64 != 0;
			}

			return null;
		}

		public DateTime AsDateTime()
		{
			if (Type == DataModelValueType.DateTime)
			{
				return new DateTime(_int64 & 0x3FFFFFFFFFFFFFFF, (DateTimeKind) ((_int64 >> 62) & 3));
			}

			throw new InvalidOperationException("DataModelValue is not DateTime");
		}

		public DateTime? AsDateTimeOrDefault()
		{
			if (Type == DataModelValueType.DateTime)
			{
				return new DateTime(_int64 & 0x3FFFFFFFFFFFFFFF, (DateTimeKind) ((_int64 >> 62) & 3));
			}

			return null;
		}

		public override bool Equals(object obj) => obj is DataModelValue other && Equals(other);

		public override int GetHashCode() => (_value != null ? _value.GetHashCode() : 0) + _int64.GetHashCode();

		public static bool operator ==(DataModelValue left, DataModelValue right) => left.Equals(right);

		public static bool operator !=(DataModelValue left, DataModelValue right) => !left.Equals(right);

		public DataModelValue DeepClone(bool isReadOnly = false)
		{
			switch (Type)
			{
				case DataModelValueType.Undefined:
				case DataModelValueType.Null:
				case DataModelValueType.String:
				case DataModelValueType.Number:
				case DataModelValueType.DateTime:
				case DataModelValueType.Boolean:
					return this;

				case DataModelValueType.Object: return new DataModelValue(AsObject().DeepClone(isReadOnly));
				case DataModelValueType.Array: return new DataModelValue(AsArray().DeepClone(isReadOnly));
				default: throw new ArgumentOutOfRangeException();
			}
		}

		public static DataModelValue FromContent(string content, ContentType contentType) => new DataModelValue(content);

		public static DataModelValue FromInlineContent(string content) => new DataModelValue(content);

		public static DataModelValue FromObject(object value)
		{
			if (value == null)
			{
				return Null;
			}

			switch (System.Type.GetTypeCode(value.GetType()))
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
					return new DataModelValue(Convert.ToDouble(value, NumberFormatInfo.InvariantInfo));
				case TypeCode.Boolean: return new DataModelValue((bool) value);
				case TypeCode.DateTime: return new DataModelValue((DateTime) value);
				case TypeCode.String: return new DataModelValue((string) value);
				case TypeCode.Object when value is DataModelValue dataModelValue:
					return dataModelValue;
				case TypeCode.Object when value is DataModelObject dataModelObject:
					return new DataModelValue(dataModelObject);
				case TypeCode.Object when value is DataModelArray dataModelArray:
					return new DataModelValue(dataModelArray);
				case TypeCode.Object when value is IDictionary<string, object> dictionary:
					return CreateDataModelObject(dictionary);
				case TypeCode.Object when value is IEnumerable array:
					return CreateDataModelArray(array);
				default: throw new ArgumentException(message: "Unsupported object type", nameof(value));
			}
		}

		private static DataModelValue CreateDataModelObject(IDictionary<string, object> dictionary)
		{
			var obj = new DataModelObject();

			foreach (var pair in dictionary)
			{
				obj[pair.Key] = FromObject(pair.Value);
			}

			return new DataModelValue(obj);
		}

		private static DataModelValue CreateDataModelArray(IEnumerable array)
		{
			var arr = new DataModelArray();

			foreach (var val in array)
			{
				arr.Add(FromObject(val));
			}

			return new DataModelValue(arr);
		}

		public static DataModelValue FromEvent(IEvent @event)
		{
			if (@event == null) throw new ArgumentNullException(nameof(@event));

			var eventObject = new DataModelObject
							  {
									  ["name"] = new DataModelValue(EventName.ToName(@event.NameParts)),
									  ["type"] = new DataModelValue(GetTypeString(@event.Type)),
									  ["sendid"] = new DataModelValue(@event.SendId),
									  ["origin"] = new DataModelValue(@event.Origin?.ToString()),
									  ["origintype"] = new DataModelValue(@event.OriginType?.ToString()),
									  ["invokeid"] = new DataModelValue(@event.InvokeId),
									  ["data"] = @event.Data.DeepClone(true)
							  };

			eventObject.Freeze();

			return new DataModelValue(eventObject);

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

		public static DataModelValue FromException(Exception exception)
		{
			if (exception == null) throw new ArgumentNullException(nameof(exception));

			var exceptionData = new DataModelObject
								{
										["message"] = new DataModelValue(exception.Message),
										["typeName"] = new DataModelValue(exception.GetType().Name),
										["source"] = new DataModelValue(exception.Source),
										["typeFullName"] = new DataModelValue(exception.GetType().FullName),
										["stackTrace"] = new DataModelValue(exception.StackTrace),
										["text"] = new DataModelValue(exception.ToString())
								};

			exceptionData.Freeze();

			return new DataModelValue(exceptionData);
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
						DataModelValueType.Object => AsObject().ToString(format: "JSON", CultureInfo.InvariantCulture),
						DataModelValueType.Array => AsArray().ToString(format: "JSON", CultureInfo.InvariantCulture),
						DataModelValueType.Number => AsNumber().ToString(format: "G17", NumberFormatInfo.InvariantInfo),
						DataModelValueType.DateTime => AsDateTime().ToString(format: "O", DateTimeFormatInfo.InvariantInfo),
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
						case var ctrl when ctrl < 32 || ctrl >= 128:
							sb.Append("\\u").Append(((int) c).ToString(format: "X4", CultureInfo.InvariantCulture));
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

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this, Dynamic.CreateMetaObject);

		private class Dynamic : DynamicObject
		{
			private static readonly IDynamicMetaObjectProvider Instance = new Dynamic(default);

			private static readonly ConstructorInfo ConstructorInfo = typeof(Dynamic).GetConstructor(new[] { typeof(DataModelValue) });

			private readonly DataModelValue _value;

			public Dynamic(DataModelValue value) => _value = value;

			public static DynamicMetaObject CreateMetaObject(Expression expression)
			{
				var newExpression = Expression.New(ConstructorInfo, Expression.Convert(expression, typeof(DataModelValue)));
				return Instance.GetMetaObject(newExpression);
			}

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
					result = _value.AsArray()[convertible.ToInt32(NumberFormatInfo.InvariantInfo)].ToObject();

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
					_value.AsArray()[convertible.ToInt32(NumberFormatInfo.InvariantInfo)] = FromObject(value);

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
						result = Convert.ChangeType(_value.AsNumber(), typeCode, NumberFormatInfo.InvariantInfo);
						return true;
				}

				if (binder.Type == typeof(DataModelObject))
				{
					result = _value.AsObject();

					return true;
				}

				if (binder.Type == typeof(DataModelArray))
				{
					result = _value.AsArray();

					return true;
				}

				result = null;

				return false;
			}
		}
	}
}