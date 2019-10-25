using System;
using System.Net.Mime;

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

	public readonly struct DataModelValue : IObject, IEquatable<DataModelValue>
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
				case TypeCode.Object when obj is DataModelObject dataModelObject:
					return new DataModelValue(dataModelObject, isReadOnly);
				case TypeCode.Object when obj is DataModelArray dataModelArray:
					return new DataModelValue(dataModelArray, isReadOnly);
				default: throw new ArgumentException(message: "Unsupported object type", nameof(obj));
			}
		}

		public static DataModelValue FromEvent(IEvent @event, bool isReadOnly = false)
		{
			var eventObject = new DataModelObject
							  {
									  ["name"] = new DataModelValue(string.Join(separator: ".", @event.NameParts), isReadOnly)
							  };

			if (isReadOnly)
			{
				eventObject.Freeze();
			}

			return new DataModelValue(eventObject, isReadOnly);
		}
	}
}