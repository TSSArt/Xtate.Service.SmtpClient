using System;
using System.Collections;
using System.Collections./**/Immutable;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Reflection;

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

	[DebuggerTypeProxy(typeof(DebugView))]
	[DebuggerDisplay(value: "{ToObject()} ({Type})")]
	public readonly struct DataModelValue : IObject, IEquatable<DataModelValue>, IFormattable, IDynamicMetaObjectProvider
	{
		private static readonly object NullValue     = new object();
		private static readonly object NumberValue   = new object();
		private static readonly object DateTimeValue = new object();
		private static readonly object BooleanValue  = new object();

		private readonly object _value;
		private readonly long   _int64;

		public DataModelValue(DataModelObject value)
		{
			_value = value ?? NullValue;
			_int64 = 0;
		}

		public DataModelValue(DataModelArray value)
		{
			_value = value ?? NullValue;
			_int64 = 0;
		}

		public DataModelValue(string value)
		{
			_value = value ?? NullValue;
			_int64 = 0;
		}

		public DataModelValue(double value)
		{
			_value = NumberValue;
			_int64 = BitConverter.DoubleToInt64Bits(value);
		}

		public DataModelValue(DateTime value)
		{
			_value = DateTimeValue;
			_int64 = value.Ticks + ((long) value.Kind << 62);
		}

		public DataModelValue(bool value)
		{
			_value = BooleanValue;
			_int64 = value ? 1 : 0;
		}

		public static implicit operator DataModelValue(DataModelObject val) => new DataModelValue(val);
		public static implicit operator DataModelValue(DataModelArray val)  => new DataModelValue(val);
		public static implicit operator DataModelValue(string val)          => new DataModelValue(val);
		public static implicit operator DataModelValue(double val)          => new DataModelValue(val);
		public static implicit operator DataModelValue(DateTime val)        => new DataModelValue(val);
		public static implicit operator DataModelValue(bool val)            => new DataModelValue(val);

		public static explicit operator DataModelObject(DataModelValue val) => val.AsObject();
		public static explicit operator DataModelArray(DataModelValue val)  => val.AsArray();
		public static explicit operator string(DataModelValue val)          => val.AsString();
		public static explicit operator double(DataModelValue val)          => val.AsNumber();
		public static explicit operator DateTime(DataModelValue val)        => val.AsDateTime();
		public static explicit operator bool(DataModelValue val)            => val.AsBoolean();

		public DataModelValueType Type =>
				_value switch
				{
						DataModelObject _ => DataModelValueType.Object,
						DataModelArray _ => DataModelValueType.Array,
						string _ => DataModelValueType.String,
						{ } val when val == DateTimeValue => DataModelValueType.DateTime,
						{ } val when val == NumberValue => DataModelValueType.Number,
						{ } val when val == BooleanValue => DataModelValueType.Boolean,
						{ } val when val == NullValue => DataModelValueType.Null,
						null => DataModelValueType.Undefined,
						_ => throw new ArgumentOutOfRangeException()
				};

		public bool Equals(DataModelValue other) => Equals(_value, other._value) && _int64 == other._int64;

		public bool IsUndefinedOrNull() => _value == null || _value == NullValue;

		public bool IsUndefined() => _value == null;

		public object ToObject() =>
				_value switch
				{
						DataModelObject obj => obj,
						DataModelArray arr => arr,
						string str => str,
						{ } val when val == DateTimeValue => AsDateTime(),
						{ } val when val == NumberValue => AsNumber(),
						{ } val when val == BooleanValue => AsBoolean(),
						{ } val when val == NullValue => null,
						null => null,
						_ => throw new ArgumentOutOfRangeException()
				};

		public static readonly DataModelValue Undefined = default;

		public static readonly DataModelValue Null = new DataModelValue((string) null);

		public DataModelObject AsObject() =>
				_value switch
				{
						DataModelObject obj => obj,
						{ } val when val == NullValue => null,
						_ => throw new InvalidOperationException(message: "DataModelValue is not DataModelObject")
				};

		public DataModelObject AsObjectOrEmpty() => _value is DataModelObject obj ? obj : DataModelObject.Empty;

		public DataModelArray AsArray() =>
				_value switch
				{
						DataModelArray arr => arr,
						{ } val when val == NullValue => null,
						_ => throw new InvalidOperationException(message: "DataModelValue is not DataModelArray")
				};

		public DataModelArray AsArrayOrEmpty() => _value is DataModelArray arr ? arr : DataModelArray.Empty;

		public string AsString() =>
				_value switch
				{
						string str => str,
						{ } val when val == NullValue => null,
						_ => throw new InvalidOperationException(message: "DataModelValue is not String")
				};

		public string AsStringOrDefault() => _value as string;

		public double AsNumber() =>
				_value == NumberValue
						? BitConverter.Int64BitsToDouble(_int64)
						: throw new InvalidOperationException(message: "DataModelValue is not Number");

		public double? AsNumberOrDefault() =>
				_value == NumberValue
						? BitConverter.Int64BitsToDouble(_int64)
						: (double?) null;

		public bool AsBoolean() =>
				_value == BooleanValue
						? _int64 != 0
						: throw new InvalidOperationException(message: "DataModelValue is not Boolean");

		public bool? AsBooleanOrDefault() =>
				_value == BooleanValue
						? _int64 != 0
						: (bool?) null;

		public DateTime AsDateTime() =>
				_value == DateTimeValue
						? new DateTime(_int64 & 0x3FFFFFFFFFFFFFFF, (DateTimeKind) ((_int64 >> 62) & 3))
						: throw new InvalidOperationException(message: "DataModelValue is not DateTime");

		public DateTime? AsDateTimeOrDefault() =>
				_value == DateTimeValue
						? new DateTime(_int64 & 0x3FFFFFFFFFFFFFFF, (DateTimeKind) ((_int64 >> 62) & 3))
						: (DateTime?) null;

		public override bool Equals(object obj) => obj is DataModelValue other && Equals(other);

		public override int GetHashCode() => (_value != null ? _value.GetHashCode() : 0) + _int64.GetHashCode();

		public static bool operator ==(DataModelValue left, DataModelValue right) => left.Equals(right);

		public static bool operator !=(DataModelValue left, DataModelValue right) => !left.Equals(right);

		public DataModelValue DeepClone(bool isReadOnly = false) =>
				_value switch
				{
						DataModelObject _ => new DataModelValue(AsObject().DeepClone(isReadOnly)),
						DataModelArray _ => new DataModelValue(AsArray().DeepClone(isReadOnly)),
						string _ => this,
						{ } val when val == DateTimeValue => this,
						{ } val when val == NumberValue => this,
						{ } val when val == BooleanValue => this,
						{ } val when val == NullValue => this,
						null => this,
						_ => throw new ArgumentOutOfRangeException()
				};

		internal bool IsDeepReadOnly() =>
				_value switch
				{
						DataModelObject obj => obj.IsDeepReadOnly(),
						DataModelArray arr => arr.IsDeepReadOnly(),
						string _ => true,
						{ } val when val == DateTimeValue => true,
						{ } val when val == NumberValue => true,
						{ } val when val == BooleanValue => true,
						{ } val when val == NullValue => true,
						null => true,
						_ => throw new ArgumentOutOfRangeException()
				};

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
									  ["data"] = @event.Data.DeepClone(isReadOnly: true)
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

		public override string ToString() => ToString(format: null, formatProvider: null);

		public string ToString(string format, IFormatProvider formatProvider)
		{
			var obj = ToObject();

			if (obj is IFormattable formattable)
			{
				return formattable.ToString(format, formatProvider);
			}

			return Convert.ToString(obj, formatProvider) ?? string.Empty;
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this, Dynamic.CreateMetaObject);

		private class DebugView
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly DataModelValue _dataModelValue;

			public DebugView(DataModelValue dataModelValue) => _dataModelValue = dataModelValue;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public object Value => _dataModelValue.ToObject();
		}

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
				if (_value._value is DataModelObject obj)
				{
					result = obj[binder.Name].ToObject();

					return true;
				}

				result = null;

				return false;
			}

			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				if (_value._value is DataModelObject obj)
				{
					obj[binder.Name] = FromObject(value);

					return true;
				}

				return false;
			}

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
			{
				if (indexes.Length == 1 && indexes[0] is string key && _value._value is DataModelObject obj)
				{
					result = obj[key].ToObject();

					return true;
				}

				if (indexes.Length == 1 && indexes[0] is IConvertible convertible && _value._value is DataModelArray arr)
				{
					result = arr[convertible.ToInt32(NumberFormatInfo.InvariantInfo)].ToObject();

					return true;
				}

				result = null;

				return false;
			}

			public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
			{
				if (indexes.Length == 1 && indexes[0] is string key && _value._value is DataModelObject obj)
				{
					obj[key] = FromObject(value);

					return true;
				}

				if (indexes.Length == 1 && indexes[0] is IConvertible convertible && _value._value is DataModelArray arr)
				{
					arr[convertible.ToInt32(NumberFormatInfo.InvariantInfo)] = FromObject(value);

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