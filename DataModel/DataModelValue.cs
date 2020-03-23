using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Reflection;
using JetBrains.Annotations;

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

	[PublicAPI]
	[DebuggerTypeProxy(typeof(DebugView))]
	[DebuggerDisplay(value: "{ToObject()} ({Type})")]
	public readonly struct DataModelValue : IObject, IEquatable<DataModelValue>, IFormattable, IDynamicMetaObjectProvider
	{
		public static readonly DataModelValue Undefined;
		public static readonly DataModelValue Null = new DataModelValue((string?) null);

		private static readonly object NullValue     = new object();
		private static readonly object NumberValue   = new object();
		private static readonly object DateTimeValue = new object();
		private static readonly object BooleanValue  = new object();

		private readonly long    _int64;
		private readonly object? _value;

		public DataModelValue(DataModelObject? value)
		{
			_value = value ?? NullValue;
			_int64 = 0;
		}

		public DataModelValue(DataModelArray? value)
		{
			_value = value ?? NullValue;
			_int64 = 0;
		}

		public DataModelValue(string? value)
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
						_ => Infrastructure.UnexpectedValue<DataModelValueType>()
				};

	#region Interface IDynamicMetaObjectProvider

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this, Dynamic.CreateMetaObject);

	#endregion

	#region Interface IEquatable<DataModelValue>

		public bool Equals(DataModelValue other) => Equals(_value, other._value) && _int64 == other._int64;

	#endregion

	#region Interface IFormattable

		public string ToString(string? format, IFormatProvider? formatProvider)
		{
			var obj = ToObject();

			if (obj is IFormattable formattable)
			{
				return formattable.ToString(format, formatProvider);
			}

			return Convert.ToString(obj, formatProvider) ?? string.Empty;
		}

	#endregion

	#region Interface IObject

		public object? ToObject() =>
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
						_ => Infrastructure.UnexpectedValue<object>()
				};

	#endregion

		public static implicit operator DataModelValue(DataModelObject? val) => new DataModelValue(val);
		public static implicit operator DataModelValue(DataModelArray? val)  => new DataModelValue(val);
		public static implicit operator DataModelValue(string? val)          => new DataModelValue(val);
		public static implicit operator DataModelValue(double val)           => new DataModelValue(val);
		public static implicit operator DataModelValue(DateTime val)         => new DataModelValue(val);
		public static implicit operator DataModelValue(bool val)             => new DataModelValue(val);

		public static DataModelValue FromDataModelObject(DataModelObject? val) => new DataModelValue(val);
		public static DataModelValue FromDataModelArray(DataModelObject? val)  => new DataModelValue(val);
		public static DataModelValue FromString(DataModelObject? val)          => new DataModelValue(val);
		public static DataModelValue FromDouble(DataModelObject? val)          => new DataModelValue(val);
		public static DataModelValue FromDateTime(DataModelObject? val)        => new DataModelValue(val);
		public static DataModelValue FromBoolean(DataModelObject? val)         => new DataModelValue(val);

		public static explicit operator DataModelObject?(DataModelValue val) => val.AsObject();
		public static explicit operator DataModelArray?(DataModelValue val)  => val.AsArray();
		public static explicit operator string?(DataModelValue val)          => val.AsString();
		public static explicit operator double(DataModelValue val)           => val.AsNumber();
		public static explicit operator DateTime(DataModelValue val)         => val.AsDateTime();
		public static explicit operator bool(DataModelValue val)             => val.AsBoolean();

		public static DataModelObject? ToDataModelObject(DataModelValue val) => val.AsObject();
		public static DataModelArray?  ToDataModelArray(DataModelValue val)  => val.AsArray();
		public static string?          ToString(DataModelValue val)          => val.AsString();
		public static double           ToDouble(DataModelValue val)          => val.AsNumber();
		public static DateTime         ToDateTime(DataModelValue val)        => val.AsDateTime();
		public static bool             ToBoolean(DataModelValue val)         => val.AsBoolean();

		public bool IsUndefinedOrNull() => _value == null || _value == NullValue;

		public bool IsUndefined() => _value == null;

		public DataModelObject? AsObject() =>
				_value switch
				{
						DataModelObject obj => obj,
						{ } val when val == NullValue => null,
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelObject)
				};

		public DataModelObject AsObjectOrEmpty() => _value is DataModelObject obj ? obj : DataModelObject.Empty;

		public DataModelArray? AsArray() =>
				_value switch
				{
						DataModelArray arr => arr,
						{ } val when val == NullValue => null,
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelArray)
				};

		public DataModelArray AsArrayOrEmpty() => _value is DataModelArray arr ? arr : DataModelArray.Empty;

		public string? AsString() =>
				_value switch
				{
						string str => str,
						{ } val when val == NullValue => null,
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_String)
				};

		public string? AsStringOrDefault() => _value as string;

		public double AsNumber() =>
				_value == NumberValue
						? BitConverter.Int64BitsToDouble(_int64)
						: throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_Number);

		public double? AsNumberOrDefault() =>
				_value == NumberValue
						? BitConverter.Int64BitsToDouble(_int64)
						: (double?) null;

		public bool AsBoolean() =>
				_value == BooleanValue
						? _int64 != 0
						: throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_Boolean);

		public bool? AsBooleanOrDefault() =>
				_value == BooleanValue
						? _int64 != 0
						: (bool?) null;

		public DateTime AsDateTime() =>
				_value == DateTimeValue
						? new DateTime(_int64 & 0x3FFFFFFFFFFFFFFF, (DateTimeKind) ((_int64 >> 62) & 3))
						: throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DateTime);

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
						DataModelObject _ => new DataModelValue(AsObject()!.DeepClone(isReadOnly)),
						DataModelArray _ => new DataModelValue(AsArray()!.DeepClone(isReadOnly)),
						string _ => this,
						{ } val when val == DateTimeValue => this,
						{ } val when val == NumberValue => this,
						{ } val when val == BooleanValue => this,
						{ } val when val == NullValue => this,
						null => this,
						_ => Infrastructure.UnexpectedValue<DataModelValue>()
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
						_ => Infrastructure.UnexpectedValue<bool>()
				};

		public static DataModelValue FromContent(string content, ContentType contentType)
		{
			var _ = contentType;

			return new DataModelValue(content);
		}

		public static DataModelValue FromInlineContent(string content) => new DataModelValue(content);

		public static DataModelValue FromObject(object? value)
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
				default: throw new ArgumentException(Resources.Exception_Unsupported_object_type, nameof(value));
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

		public static DataModelValue FromEvent(IEvent evt)
		{
			if (evt == null) throw new ArgumentNullException(nameof(evt));

			var eventObject = new DataModelObject
							  {
									  [@"name"] = new DataModelValue(EventName.ToName(evt.NameParts)),
									  [@"type"] = new DataModelValue(GetTypeString(evt.Type)),
									  [@"sendid"] = new DataModelValue(evt.SendId),
									  [@"origin"] = new DataModelValue(evt.Origin?.ToString()),
									  [@"origintype"] = new DataModelValue(evt.OriginType?.ToString()),
									  [@"invokeid"] = new DataModelValue(evt.InvokeId),
									  [@"data"] = evt.Data.DeepClone(isReadOnly: true)
							  };

			eventObject.Freeze();

			return new DataModelValue(eventObject);

			static string GetTypeString(EventType eventType)
			{
				return eventType switch
				{
						EventType.Platform => @"platform",
						EventType.Internal => @"internal",
						EventType.External => @"external",
						_ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, message: null)
				};
			}
		}

		public static DataModelValue FromException(Exception exception)
		{
			if (exception == null) throw new ArgumentNullException(nameof(exception));

			var exceptionData = new DataModelObject
								{
										[@"message"] = new DataModelValue(exception.Message),
										[@"typeName"] = new DataModelValue(exception.GetType().Name),
										[@"source"] = new DataModelValue(exception.Source),
										[@"typeFullName"] = new DataModelValue(exception.GetType().FullName),
										[@"stackTrace"] = new DataModelValue(exception.StackTrace),
										[@"text"] = new DataModelValue(exception.ToString())
								};

			exceptionData.Freeze();

			return new DataModelValue(exceptionData);
		}

		public override string ToString() => ToString(format: null, formatProvider: null);

		private class DebugView
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly DataModelValue _dataModelValue;

			public DebugView(DataModelValue dataModelValue) => _dataModelValue = dataModelValue;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]

			// ReSharper disable once UnusedMember.Local
			public object? Value => _dataModelValue.ToObject();
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

			public override bool TryGetMember(GetMemberBinder binder, out object? result)
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

			public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
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

			public override bool TryConvert(ConvertBinder binder, out object? result)
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