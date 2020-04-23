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

		public DataModelValue(DateTimeOffset value)
		{
			_value = DateTimeValue.Get(value.Offset);
			_int64 = value.Ticks;
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
						DateTimeValue _ => DataModelValueType.DateTime,
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
						DateTimeValue val => new DateTimeOffset(new DateTime(_int64), val.Offset),
						{ } val when val == NumberValue => AsNumber(),
						{ } val when val == BooleanValue => AsBoolean(),
						{ } val when val == NullValue => null,
						null => null,
						_ => Infrastructure.UnexpectedValue<object>()
				};

	#endregion

		public static implicit operator DataModelValue(DataModelObject? val) => FromDataModelObject(val);
		public static implicit operator DataModelValue(DataModelArray? val)  => FromDataModelArray(val);
		public static implicit operator DataModelValue(string? val)          => FromString(val);
		public static implicit operator DataModelValue(double val)           => FromDouble(val);
		public static implicit operator DataModelValue(DateTimeOffset val)   => FromDateTimeOffset(val);
		public static implicit operator DataModelValue(bool val)             => FromBoolean(val);

		public static DataModelValue FromDataModelObject(DataModelObject? val) => new DataModelValue(val);
		public static DataModelValue FromDataModelArray(DataModelArray? val)   => new DataModelValue(val);
		public static DataModelValue FromString(string? val)                   => new DataModelValue(val);
		public static DataModelValue FromDouble(double val)                    => new DataModelValue(val);
		public static DataModelValue FromDateTimeOffset(DateTimeOffset val)    => new DataModelValue(val);
		public static DataModelValue FromBoolean(bool val)                     => new DataModelValue(val);

		public static explicit operator DataModelObject?(DataModelValue val) => ToDataModelObject(val);
		public static explicit operator DataModelArray?(DataModelValue val)  => ToDataModelArray(val);
		public static explicit operator string?(DataModelValue val)          => ToString(val);
		public static explicit operator double(DataModelValue val)           => ToDouble(val);
		public static explicit operator DateTimeOffset(DataModelValue val)   => ToDateTimeOffset(val);
		public static explicit operator bool(DataModelValue val)             => ToBoolean(val);

		public static DataModelObject? ToDataModelObject(DataModelValue val) => val.AsNullableObject();
		public static DataModelArray?  ToDataModelArray(DataModelValue val)  => val.AsNullableArray();
		public static string?          ToString(DataModelValue val)          => val.AsNullableString();
		public static double           ToDouble(DataModelValue val)          => val.AsNumber();
		public static DateTimeOffset   ToDateTimeOffset(DataModelValue val)  => val.AsDateTime();
		public static bool             ToBoolean(DataModelValue val)         => val.AsBoolean();

		public bool IsUndefinedOrNull() => _value == null || _value == NullValue;

		public bool IsUndefined() => _value == null;

		public DataModelObject AsObject() =>
				_value switch
				{
						DataModelObject obj => obj,
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelObject)
				};

		public DataModelObject? AsNullableObject() =>
				_value switch
				{
						DataModelObject obj => obj,
						{ } val when val == NullValue => null,
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelObject)
				};

		public DataModelObject AsObjectOrEmpty() => _value is DataModelObject obj ? obj : DataModelObject.Empty;

		public DataModelArray AsArray() =>
				_value switch
				{
						DataModelArray arr => arr,
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelArray)
				};

		public DataModelArray? AsNullableArray() =>
				_value switch
				{
						DataModelArray arr => arr,
						{ } val when val == NullValue => null,
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DataModelArray)
				};

		public DataModelArray AsArrayOrEmpty() => _value is DataModelArray arr ? arr : DataModelArray.Empty;

		public string AsString() =>
				_value switch
				{
						string str => str,
						_ => throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_String)
				};

		public string? AsNullableString() =>
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

		public DateTimeOffset AsDateTime() =>
				_value is DateTimeValue val
						? new DateTimeOffset(new DateTime(_int64), val.Offset)
						: throw new ArgumentException(message: Resources.Exception_DataModelValue_is_not_DateTime);

		public DateTimeOffset? AsDateTimeOrDefault() =>
				_value is DateTimeValue val
						? new DateTimeOffset(new DateTime(_int64), val.Offset)
						: (DateTimeOffset?) null;

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
						string _ => this,
						DateTimeValue _ => this,
						{ } val when val == NumberValue => this,
						{ } val when val == BooleanValue => this,
						{ } val when val == NullValue => this,
						null => this,
						_ => Infrastructure.UnexpectedValue<DataModelValue>()
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
				case TypeCode.Boolean: return new DataModelValue((bool) value);
				case TypeCode.DateTime: return new DataModelValue((DateTime) value);
				case TypeCode.String: return new DataModelValue((string) value);
				case TypeCode.Object when value is DataModelValue dataModelValue:
					return dataModelValue;
				case TypeCode.Object when value is IObject obj:
					return FromObjectWithMap(obj.ToObject(), ref map);
				case TypeCode.Object when value is DataModelObject dataModelObject:
					return new DataModelValue(dataModelObject);
				case TypeCode.Object when value is DataModelArray dataModelArray:
					return new DataModelValue(dataModelArray);
				case TypeCode.Object when value is IDictionary<string, object> dictionary:
					return CreateDataModelObject(dictionary, ref map);
				case TypeCode.Object when value is IEnumerable array:
					return CreateDataModelArray(array, ref map);
				case TypeCode.Object when IsAnonymousTypeValue(type):
					return CreateDataModelObjectFromObjectProperties(type, value, ref map);
				default: throw new ArgumentException(Resources.Exception_Unsupported_object_type, nameof(value));
			}
		}

		private static bool IsAnonymousTypeValue(Type type) => type.Name.Contains("AnonymousType") && type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), inherit: false).Length > 0;

		private static DataModelValue CreateDataModelObjectFromObjectProperties(Type type, object value, ref Dictionary<object, object>? map)
		{
			map ??= new Dictionary<object, object>();

			if (map.TryGetValue(value, out var val))
			{
				return new DataModelValue((DataModelObject) val);
			}

			var obj = new DataModelObject();

			map[value] = obj;

			foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				obj[propertyInfo.Name] = FromObjectWithMap(propertyInfo.GetValue(value), ref map);
			}

			return new DataModelValue(obj);
		}

		private static DataModelValue CreateDataModelObject(IDictionary<string, object> dictionary, ref Dictionary<object, object>? map)
		{
			map ??= new Dictionary<object, object>();

			if (map.TryGetValue(dictionary, out var val))
			{
				return new DataModelValue((DataModelObject) val);
			}

			var obj = new DataModelObject();

			map[dictionary] = obj;

			foreach (var pair in dictionary)
			{
				obj[pair.Key] = FromObjectWithMap(pair.Value, ref map);
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

			var arr = new DataModelArray();

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

			public DateTimeValue(TimeSpan offset)
			{
				Offset = offset;
			}

			public TimeSpan Offset { get; }

			public static DateTimeValue Get(TimeSpan offset)
			{
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
		}

		[PublicAPI]
		[ExcludeFromCodeCoverage]
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