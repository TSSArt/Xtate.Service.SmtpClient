using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using TSSArt.StateMachine.Annotations;

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
	public readonly struct DataModelValue : IObject, IEquatable<DataModelValue>, IFormattable, IDynamicMetaObjectProvider, IConvertible
	{
		private static readonly object NullValue    = new object();
		private static readonly object NumberValue  = new object();
		private static readonly object BooleanValue = new object();

		public static readonly DataModelValue Undefined;
		public static readonly DataModelValue Null = new DataModelValue((string?) null);

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

		public DataModelValue(int value) : this((double) value) { }

		public DataModelValue(DateTimeOffset value) => _value = DateTimeValue.GetDateTimeValue(value, out _int64);

		public DataModelValue(DateTime value) : this((DateTimeOffset) value) { }

		public DataModelValue(bool value)
		{
			_value = BooleanValue;
			_int64 = value ? 1 : 0;
		}

		public DataModelValue(ILazyValue? lazyValue)
		{
			_value = lazyValue ?? NullValue;
			_int64 = 0;
		}

		public DataModelValueType Type =>
				_value switch
				{
						null => DataModelValueType.Undefined,
						{ } val when val == NullValue => DataModelValueType.Null,
						{ } val when val == NumberValue => DataModelValueType.Number,
						{ } val when val == BooleanValue => DataModelValueType.Boolean,
						string _ => DataModelValueType.String,
						DateTimeValue _ => DataModelValueType.DateTime,
						DataModelObject _ => DataModelValueType.Object,
						DataModelArray _ => DataModelValueType.Array,
						ILazyValue lazyValue => lazyValue.Value.Type,
						_ => Infrastructure.UnexpectedValue<DataModelValueType>()
				};

	#region Interface IConvertible

		public string ToString(IFormatProvider provider) => ToString(format: null, provider);

		TypeCode IConvertible.GetTypeCode() =>
				Type switch
				{
						DataModelValueType.Undefined => TypeCode.Empty,
						DataModelValueType.Null => TypeCode.Empty,
						DataModelValueType.String => TypeCode.String,
						DataModelValueType.Object => TypeCode.Object,
						DataModelValueType.Array => TypeCode.Object,
						DataModelValueType.Number => TypeCode.Double,
						DataModelValueType.DateTime => TypeCode.Object,
						DataModelValueType.Boolean => TypeCode.Boolean,
						_ => Infrastructure.UnexpectedValue<TypeCode>()
				};

		bool IConvertible.ToBoolean(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToBoolean(AsNumber()),
						DataModelValueType.DateTime => Convert.ToBoolean(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToBoolean(AsBoolean()),
						_ => Convert.ToBoolean(ToObject(), provider)
				};

		byte IConvertible.ToByte(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToByte(AsNumber()),
						DataModelValueType.DateTime => Convert.ToByte(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToByte(AsBoolean()),
						_ => Convert.ToByte(ToObject(), provider)
				};

		char IConvertible.ToChar(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToChar(AsNumber()),
						DataModelValueType.DateTime => Convert.ToChar(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToChar(AsBoolean()),
						_ => Convert.ToChar(ToObject(), provider)
				};

		decimal IConvertible.ToDecimal(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToDecimal(AsNumber()),
						DataModelValueType.DateTime => Convert.ToDecimal(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToDecimal(AsBoolean()),
						_ => Convert.ToDecimal(ToObject(), provider)
				};

		double IConvertible.ToDouble(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToDouble(AsNumber()),
						DataModelValueType.DateTime => Convert.ToDouble(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToDouble(AsBoolean()),
						_ => Convert.ToDouble(ToObject(), provider)
				};

		short IConvertible.ToInt16(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToInt16(AsNumber()),
						DataModelValueType.DateTime => Convert.ToInt16(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToInt16(AsBoolean()),
						_ => Convert.ToInt16(ToObject(), provider)
				};

		int IConvertible.ToInt32(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToInt32(AsNumber()),
						DataModelValueType.DateTime => Convert.ToInt32(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToInt32(AsBoolean()),
						_ => Convert.ToInt32(ToObject(), provider)
				};

		long IConvertible.ToInt64(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToInt64(AsNumber()),
						DataModelValueType.DateTime => Convert.ToInt64(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToInt64(AsBoolean()),
						_ => Convert.ToInt64(ToObject(), provider)
				};

		sbyte IConvertible.ToSByte(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToSByte(AsNumber()),
						DataModelValueType.DateTime => Convert.ToSByte(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToSByte(AsBoolean()),
						_ => Convert.ToSByte(ToObject(), provider)
				};

		float IConvertible.ToSingle(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToSingle(AsNumber()),
						DataModelValueType.DateTime => Convert.ToSingle(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToSingle(AsBoolean()),
						_ => Convert.ToSingle(ToObject(), provider)
				};

		ushort IConvertible.ToUInt16(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToUInt16(AsNumber()),
						DataModelValueType.DateTime => Convert.ToUInt16(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToUInt16(AsBoolean()),
						_ => Convert.ToUInt16(ToObject(), provider)
				};

		uint IConvertible.ToUInt32(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToUInt32(AsNumber()),
						DataModelValueType.DateTime => Convert.ToUInt32(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToUInt32(AsBoolean()),
						_ => Convert.ToUInt32(ToObject(), provider)
				};

		ulong IConvertible.ToUInt64(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToUInt64(AsNumber()),
						DataModelValueType.DateTime => Convert.ToUInt64(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToUInt64(AsBoolean()),
						_ => Convert.ToUInt64(ToObject(), provider)
				};

		DateTime IConvertible.ToDateTime(IFormatProvider provider) =>
				Type switch
				{
						DataModelValueType.Number => Convert.ToDateTime(AsNumber()),
						DataModelValueType.DateTime => Convert.ToDateTime(AsDateTime()),
						DataModelValueType.Boolean => Convert.ToDateTime(AsBoolean()),
						_ => Convert.ToDateTime(ToObject(), provider)
				};

		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			if (conversionType == typeof(string))
			{
				return ToString(format: null, provider);
			}

			if (conversionType == typeof(DateTimeOffset))
			{
				return ToDateTimeOffset(this);
			}

			return Type switch
			{
					DataModelValueType.Number => ToType(AsNumber()),
					DataModelValueType.DateTime => ToType(AsDateTime()),
					DataModelValueType.Boolean => ToType(AsBoolean()),
					_ => Convert.ChangeType(ToObject(), conversionType, provider)
			};

			object ToType<T>(T val) where T : IConvertible => val.ToType(conversionType, provider);

			DateTimeOffset ToDateTimeOffset(in DataModelValue val) =>
					val.Type switch
					{
							DataModelValueType.Number => new DateTimeOffset(Convert.ToDateTime(val.AsNumber())),
							DataModelValueType.DateTime => val.AsDateTimeOffset(),
							DataModelValueType.Boolean => new DateTimeOffset(Convert.ToDateTime(val.AsBoolean())),
							_ => new DateTimeOffset(Convert.ToDateTime(val.ToObject(), provider))
					};
		}

	#endregion

	#region Interface IDynamicMetaObjectProvider

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this, Dynamic.CreateMetaObject);

	#endregion

	#region Interface IEquatable<DataModelValue>

		public bool Equals(DataModelValue other)
		{
			var val = this;
			while (val._value is ILazyValue lazyValue)
			{
				val = lazyValue.Value;
			}

			while (other._value is ILazyValue lazyValue)
			{
				other = lazyValue.Value;
			}

			return val._int64 == other._int64 && Equals(val._value, other._value);
		}

	#endregion

	#region Interface IFormattable

		public string ToString(string? format, IFormatProvider? formatProvider)
		{
			return Type switch
			{
					DataModelValueType.Number => AsNumber().ToString(format, formatProvider),
					DataModelValueType.DateTime => AsDateTimeOffset().ToString(format ?? "o", formatProvider),
					DataModelValueType.Boolean => AsBoolean().ToString(formatProvider),
					_ => ObjectToString(ToObject(), format, formatProvider)
			};

			static string ObjectToString(object? obj, string? format, IFormatProvider? formatProvider) =>
					(!string.IsNullOrEmpty(format) && obj is IFormattable formattable
							? formattable.ToString(format, formatProvider)
							: Convert.ToString(obj, formatProvider)) ?? string.Empty;
		}

	#endregion

	#region Interface IObject

		public object? ToObject() =>
				_value switch
				{
						null => null,
						{ } val when val == NullValue => null,
						{ } val when val == NumberValue => AsNumber(),
						{ } val when val == BooleanValue => AsBoolean(),
						string str => str,
						DateTimeValue val => new DateTimeOffset(new DateTime(_int64), val.Offset),
						DataModelObject obj => obj,
						DataModelArray arr => arr,
						ILazyValue lazyValue => lazyValue.Value.ToObject(),
						_ => Infrastructure.UnexpectedValue<object>()
				};

	#endregion

		public static implicit operator DataModelValue(DataModelObject? val) => FromDataModelObject(val);
		public static implicit operator DataModelValue(DataModelArray? val)  => FromDataModelArray(val);
		public static implicit operator DataModelValue(string? val)          => FromString(val);
		public static implicit operator DataModelValue(double val)           => FromDouble(val);
		public static implicit operator DataModelValue(int val)              => FromInt32(val);
		public static implicit operator DataModelValue(DateTimeOffset val)   => FromDateTimeOffset(val);
		public static implicit operator DataModelValue(DateTime val)         => FromDateTime(val);
		public static implicit operator DataModelValue(bool val)             => FromBoolean(val);

		public static DataModelValue FromDataModelObject(DataModelObject? val) => new DataModelValue(val);
		public static DataModelValue FromDataModelArray(DataModelArray? val)   => new DataModelValue(val);
		public static DataModelValue FromString(string? val)                   => new DataModelValue(val);
		public static DataModelValue FromDouble(double val)                    => new DataModelValue(val);
		public static DataModelValue FromInt32(int val)                        => new DataModelValue(val);
		public static DataModelValue FromDateTimeOffset(DateTimeOffset val)    => new DataModelValue(val);
		public static DataModelValue FromDateTime(DateTime val)                => new DataModelValue(val);
		public static DataModelValue FromBoolean(bool val)                     => new DataModelValue(val);
		public static DataModelValue FromLazyValue(ILazyValue val)             => new DataModelValue(val);

		public bool IsUndefinedOrNull() => _value == null || _value == NullValue;

		public bool IsUndefined() => _value == null;

		public DataModelObject AsObject() =>
				_value switch
				{
						DataModelObject obj => obj,
						ILazyValue val => val.Value.AsObject(),
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelObject)
				};

		public DataModelObject? AsNullableObject() =>
				_value switch
				{
						DataModelObject obj => obj,
						{ } val when val == NullValue => null,
						ILazyValue val => val.Value.AsNullableObject(),
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelObject)
				};

		public DataModelObject AsObjectOrEmpty() =>
				_value switch
				{
						DataModelObject obj => obj,
						ILazyValue val => val.Value.AsObjectOrEmpty(),
						_ => DataModelObject.Empty
				};

		public DataModelArray AsArray() =>
				_value switch
				{
						DataModelArray arr => arr,
						ILazyValue val => val.Value.AsArray(),
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelArray)
				};

		public DataModelArray? AsNullableArray() =>
				_value switch
				{
						DataModelArray arr => arr,
						{ } val when val == NullValue => null,
						ILazyValue val => val.Value.AsNullableArray(),
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelArray)
				};

		public DataModelArray AsArrayOrEmpty() =>
				_value switch
				{
						DataModelArray arr => arr,
						ILazyValue val => val.Value.AsArrayOrEmpty(),
						_ => DataModelArray.Empty
				};

		public string AsString() =>
				_value switch
				{
						string str => str,
						ILazyValue val => val.Value.AsString(),
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_String)
				};

		public string? AsNullableString() =>
				_value switch
				{
						string str => str,
						{ } val when val == NullValue => null,
						ILazyValue val => val.Value.AsNullableString(),
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_String)
				};

		public string? AsStringOrDefault() =>
				_value switch
				{
						string str => str,
						ILazyValue val => val.Value.AsStringOrDefault(),
						_ => null
				};

		public double AsNumber() =>
				_value == NumberValue
						? BitConverter.Int64BitsToDouble(_int64)
						: _value is ILazyValue val
								? val.Value.AsNumber()
								: throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_Number);

		public double? AsNumberOrDefault() =>
				_value == NumberValue
						? BitConverter.Int64BitsToDouble(_int64)
						: _value is ILazyValue val
								? val.Value.AsNumberOrDefault()
								: null;

		public int AsInteger() => (int) AsNumber();

		public int? AsIntegerOrDefault() => (int?) AsNumberOrDefault()!;

		public bool AsBoolean() =>
				_value == BooleanValue
						? _int64 != 0
						: _value is ILazyValue val
								? val.Value.AsBoolean()
								: throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_Boolean);

		public bool? AsBooleanOrDefault() =>
				_value == BooleanValue
						? _int64 != 0
						: _value is ILazyValue val
								? val.Value.AsBooleanOrDefault()
								: null;

		public DateTimeOffset AsDateTimeOffset() =>
				_value switch
				{
						DateTimeValue val => val.GetDateTimeOffset(_int64),
						ILazyValue lazyVal => lazyVal.Value.AsDateTimeOffset(),
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DateTime)
				};

		public DateTimeOffset? AsDateTimeOffsetOrDefault() =>
				_value switch
				{
						DateTimeValue val => val.GetDateTimeOffset(_int64),
						ILazyValue lazyVal => lazyVal.Value.AsDateTimeOffsetOrDefault(),
						_ => null
				};

		public DateTime AsDateTime() => AsDateTimeOffset().UtcDateTime;

		public DateTime? AsDateTimeOrDefault() => AsDateTimeOffsetOrDefault()?.UtcDateTime;

		public override bool Equals(object obj) => obj is DataModelValue other && Equals(other);

		public override int GetHashCode() => (_value != null ? _value.GetHashCode() : 0) + _int64.GetHashCode();

		public static bool operator ==(DataModelValue left, DataModelValue right) => left.Equals(right);

		public static bool operator !=(DataModelValue left, DataModelValue right) => !left.Equals(right);

		public DataModelValue CloneAsWritable()
		{
			Dictionary<object, object>? map = null;

			return DeepCloneWithMap(DataModelAccess.Writable, ref map);
		}

		public DataModelValue CloneAsReadOnly()
		{
			Dictionary<object, object>? map = null;

			return DeepCloneWithMap(DataModelAccess.ReadOnly, ref map);
		}

		public DataModelValue AsConstant()
		{
			Dictionary<object, object>? map = null;

			return DeepCloneWithMap(DataModelAccess.Constant, ref map);
		}

		internal DataModelValue DeepCloneWithMap(DataModelAccess targetAccess, ref Dictionary<object, object>? map) =>
				_value switch
				{
						DataModelObject obj => new DataModelValue(obj.DeepCloneWithMap(targetAccess, ref map)),
						DataModelArray arr => new DataModelValue(arr.DeepCloneWithMap(targetAccess, ref map)),
						_ => this
				};

		public void MakeDeepConstant()
		{
			switch (_value)
			{
				case DataModelObject obj:
					obj.MakeDeepConstant();
					break;
				case DataModelArray arr:
					arr.MakeDeepConstant();
					break;
			}
		}

		public static DataModelValue FromObject(object? value)
		{
			Dictionary<object, object>? map = null;

			return FromObjectWithMap(value, ref map);
		}

		private static DataModelValue FromObjectWithMap(object? value, ref Dictionary<object, object>? map)
		{
			if (value == null)
			{
				return Null;
			}

			var type = value.GetType();
			switch (System.Type.GetTypeCode(type))
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

				case TypeCode.Boolean:
					return new DataModelValue((bool) value);

				case TypeCode.DateTime:
					return new DataModelValue((DateTime) value);

				case TypeCode.String:
					return new DataModelValue((string) value);

				case TypeCode.Object:
					return FromUnknownObjectWithMap(value, ref map);

				default: throw new ArgumentException(Resources.Exception_Unsupported_object_type, nameof(value));
			}
		}

		private static DataModelValue FromUnknownObjectWithMap(object value, ref Dictionary<object, object>? map) =>
				value switch
				{
						DateTimeOffset val => new DataModelValue(val),
						DataModelValue val => val,
						IObject obj => FromObjectWithMap(obj.ToObject(), ref map),
						DataModelObject obj => new DataModelValue(obj),
						DataModelArray arr => new DataModelValue(arr),
						IDictionary<string, object> dict => CreateDataModelObject(dict, ref map),
						IDictionary<string, string> dict => CreateDataModelObject(dict, ref map),
						IEnumerable arr => CreateDataModelArray(arr, ref map),
						ILazyValue val => new DataModelValue(val),
						{ } when TryFromAnonymousType(value, ref map, out var val) => val,
						_ => throw new ArgumentException(Resources.Exception_Unsupported_object_type, nameof(value))
				};

		private static bool TryFromAnonymousType(object value, ref Dictionary<object, object>? map, out DataModelValue result)
		{
			var type = value.GetType();

			if (!type.Name.Contains("AnonymousType") || type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), inherit: false).Length == 0)
			{
				result = default;

				return false;
			}

			map ??= new Dictionary<object, object>();

			if (map.TryGetValue(value, out var val))
			{
				result = new DataModelValue((DataModelObject) val);

				return true;
			}

			var propertyInfos = value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

			var obj = new DataModelObject(propertyInfos.Length);

			map[value] = obj;

			foreach (var propertyInfo in propertyInfos)
			{
				obj[propertyInfo.Name] = FromObjectWithMap(propertyInfo.GetValue(value), ref map);
			}

			result = new DataModelValue(obj);

			return true;
		}

		private static DataModelValue CreateDataModelObject(IDictionary<string, object> dictionary, ref Dictionary<object, object>? map)
		{
			map ??= new Dictionary<object, object>();

			if (map.TryGetValue(dictionary, out var val))
			{
				return new DataModelValue((DataModelObject) val);
			}

			var obj = new DataModelObject(dictionary.Count);

			map[dictionary] = obj;

			foreach (var pair in dictionary)
			{
				obj[pair.Key] = FromObjectWithMap(pair.Value, ref map);
			}

			return new DataModelValue(obj);
		}

		private static DataModelValue CreateDataModelObject(IDictionary<string, string> dictionary, ref Dictionary<object, object>? map)
		{
			map ??= new Dictionary<object, object>();

			if (map.TryGetValue(dictionary, out var val))
			{
				return new DataModelValue((DataModelObject) val);
			}

			var obj = new DataModelObject(dictionary.Count);

			map[dictionary] = obj;

			foreach (var pair in dictionary)
			{
				obj[pair.Key] = new DataModelValue(pair.Value);
			}

			return new DataModelValue(obj);
		}

		private static DataModelValue CreateDataModelArray(IEnumerable array, ref Dictionary<object, object>? map)
		{
			map ??= new Dictionary<object, object>();

			if (map.TryGetValue(array, out var val))
			{
				return new DataModelValue((DataModelArray) val);
			}

			var arr = new DataModelArray(array.Capacity());

			map[array] = arr;

			foreach (var item in array)
			{
				arr.Add(FromObjectWithMap(item, ref map));
			}

			return new DataModelValue(arr);
		}

		public override string ToString() => ToString(format: null, formatProvider: null);

		private sealed class DateTimeValue
		{
			private static readonly TimeSpan MinOffset = new TimeSpan(hours: -14, minutes: 0, seconds: 0);
			private static readonly TimeSpan MaxOffset = new TimeSpan(hours: 14, minutes: 0, seconds: 0);

			private static ImmutableDictionary<int, DateTimeValue> _cachedOffsets = ImmutableDictionary<int, DateTimeValue>.Empty;

			public DateTimeValue(TimeSpan offset) => Offset = offset;

			public TimeSpan Offset { get; }

			public static DateTimeValue GetDateTimeValue(DateTimeOffset dateTimeOffset, out long utcTicks)
			{
				utcTicks = dateTimeOffset.UtcTicks;

				var offset = dateTimeOffset.Offset;

				Infrastructure.Assert(MinOffset <= offset && offset <= MaxOffset && offset.Ticks % TimeSpan.TicksPerMinute == 0);

				var val = (int) (offset.Ticks / TimeSpan.TicksPerMinute);

				if (val % 15 != 0)
				{
					return new DateTimeValue(offset);
				}

				var cachedOffsets = _cachedOffsets;

				if (!cachedOffsets.TryGetValue(val, out var dateTimeValue))
				{
					dateTimeValue = new DateTimeValue(offset);

					_cachedOffsets = cachedOffsets.Add(val, dateTimeValue);
				}

				return dateTimeValue;
			}

			public DateTimeOffset GetDateTimeOffset(long utcTicks) => new DateTimeOffset(new DateTime(utcTicks + Offset.Ticks), Offset);

			public override int GetHashCode() => 0;

			public override bool Equals(object obj) => obj is DateTimeValue;
		}

		[PublicAPI]
		[ExcludeFromCodeCoverage]
		private class DebugView
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private readonly DataModelValue _dataModelValue;

			public DebugView(DataModelValue dataModelValue) => _dataModelValue = dataModelValue;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public object? Value => _dataModelValue.ToObject();
		}

		private class Dynamic : DynamicObject
		{
			private static readonly IDynamicMetaObjectProvider Instance = new Dynamic(default);

			private static readonly ConstructorInfo ConstructorInfo = typeof(Dynamic).GetConstructor(new[] { typeof(DataModelValue) })!;

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

				if (binder.Type == typeof(DateTimeOffset))
				{
					result = _value.AsDateTimeOffset();

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