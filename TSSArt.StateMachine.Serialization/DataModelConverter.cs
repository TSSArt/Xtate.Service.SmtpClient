using System;

namespace TSSArt.StateMachine
{
	public static class DataModelConverter
	{
		public static string ToJson(DataModelValue value)
		{
			throw new NotImplementedException();

		}

		public static string ToJson(DataModelObject obj)
		{
			throw new NotImplementedException();

		}

		public static string ToJson(DataModelArray array)
		{
			throw new NotImplementedException();

		}

		public static DataModelValue FromJson(string str)
		{
			throw new NotImplementedException();
		}
	}
	/*
	private class JsonValueConverter : JsonConverter<DataModelValue>
	{
		public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(DataModelValue);

		public override DataModelValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new DataModelValue(ref reader, options);

		public override void Write(Utf8JsonWriter writer, DataModelValue value, JsonSerializerOptions options) => value.WriteTo(writer, options);
	}
	*/
}

/*
internal DataModelValue(ref Utf8JsonReader reader, JsonSerializerOptions options) : this()
{
	switch (reader.TokenType)
	{
		case JsonTokenType.String:
			if (reader.TryGetDateTime(out var datetime))
			{
				Type = DataModelValueType.DateTime;
				_int64 = datetime.Ticks + ((long)datetime.Kind << 62);
			}
			else
			{
				Type = DataModelValueType.String;
				_value = reader.GetString();
			}

			break;

		case JsonTokenType.Number:
			Type = DataModelValueType.Number;
			_int64 = BitConverter.Int64BitsToDouble(_int64);

			break;
		case JsonTokenType.True: return new DataModelValue(true);
		case JsonTokenType.False: return new DataModelValue(false);
		case JsonTokenType.Null: return Null;

		case JsonTokenType.StartObject:
			var obj = new DataModelObject();
			obj.ReadFrom(reader, options);
			obj.Freeze();
			return new DataModelValue(obj);

		case JsonTokenType.StartArray:
			var arr = new DataModelArray();
			arr.ReadFrom(reader, options);
			arr.Freeze();
			return new DataModelValue(arr);

		default: throw new ArgumentOutOfRangeException();
	}
}

internal void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options)
{
	switch (Type)
	{
		case DataModelValueType.Undefined:
			writer.WriteNullValue();
			break;
		case DataModelValueType.Null:
			writer.WriteNullValue();
			break;
		case DataModelValueType.String:
			writer.WriteStringValue(AsString());
			break;
		case DataModelValueType.Number:
			writer.WriteNumberValue(AsNumber());
			break;
		case DataModelValueType.DateTime:
			writer.WriteStringValue(AsDateTime());
			break;
		case DataModelValueType.Boolean:
			writer.WriteBooleanValue(AsBoolean());
			break;
		case DataModelValueType.Object:
			AsObject().WriteTo(writer, options);
			break;
		case DataModelValueType.Array:
			AsArray().WriteTo(writer, options);
			break;
		default: throw new ArgumentOutOfRangeException();
	}
}

internal void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options)
{
writer.WriteStartObject();

foreach (var pair in _properties)
{
	if (!pair.Value.Value.IsUndefinedOrNull())
	{
		writer.WritePropertyName(pair.Key);
		pair.Value.Value.WriteTo(writer, options);
	}
}

writer.WriteEndObject();
}

private class JsonValueConverter : JsonConverter<DataModelObject>
{
	public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(DataModelObject);

	public override DataModelObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType)

			switch (reader.TokenType)
			{
					reader.

				case JsonTokenType.String: return reader.TryGetDateTime(out DateTime datetime) ? new DataModelValue(datetime) : new DataModelValue(reader.GetString());
				case JsonTokenType.Number: return reader.TryGetInt64(out long l) ? new DataModelValue(l) : new DataModelValue(reader.GetDouble());
				case JsonTokenType.True: return new DataModelValue(true);
				case JsonTokenType.False: return new DataModelValue(false);
				case JsonTokenType.Null: return Null;
				default: throw new ArgumentOutOfRangeException();
			}
	}

	public override void Write(Utf8JsonWriter writer, DataModelObject value, JsonSerializerOptions options) => value.WriteTo(writer, options);
}
*/