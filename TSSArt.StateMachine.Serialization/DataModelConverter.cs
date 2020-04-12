using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	public static class DataModelConverter
	{
		private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
																{
																		Converters =
																		{
																				new JsonValueConverter(),
																				new JsonObjectConverter(),
																				new JsonArrayConverter()
																		},
																		WriteIndented = true
																};

		public static string ToJson(DataModelValue value) => JsonSerializer.Serialize(value, Options);

		public static byte[] ToJsonUtf8Bytes(DataModelValue value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

		public static Task ToJsonAsync(Stream stream, DataModelValue value, CancellationToken token = default) => JsonSerializer.SerializeAsync(stream, value, Options, token);

		public static DataModelValue FromJson(string json) => JsonSerializer.Deserialize<DataModelValue>(json, Options);

		public static DataModelValue FromJson(ReadOnlySpan<byte> utf8Json) => JsonSerializer.Deserialize<DataModelValue>(utf8Json, Options);

		public static ValueTask<DataModelValue> FromJsonAsync(Stream stream, CancellationToken token = default) => JsonSerializer.DeserializeAsync<DataModelValue>(stream, Options, token);

		private class JsonValueConverter : JsonConverter<DataModelValue>
		{
			public override DataModelValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				switch (reader.TokenType)
				{
					case JsonTokenType.True: return new DataModelValue(true);
					case JsonTokenType.False: return new DataModelValue(false);
					case JsonTokenType.Null: return DataModelValue.Null;

					case JsonTokenType.String:
						if (reader.TryGetDateTime(out var datetime))
						{
							return new DataModelValue(datetime);
						}

						return new DataModelValue(reader.GetString());

					case JsonTokenType.Number:
						return new DataModelValue(reader.GetDouble());

					case JsonTokenType.StartObject:
						return new DataModelValue(JsonSerializer.Deserialize<DataModelObject>(ref reader, options));

					case JsonTokenType.StartArray:
						return new DataModelValue(JsonSerializer.Deserialize<DataModelArray>(ref reader, options));

					default: return Infrastructure.UnexpectedValue<DataModelValue>();
				}
			}

			public override void Write(Utf8JsonWriter writer, DataModelValue value, JsonSerializerOptions options)
			{
				switch (value.Type)
				{
					case DataModelValueType.Undefined:
						writer.WriteNullValue();
						break;
					case DataModelValueType.Null:
						writer.WriteNullValue();
						break;
					case DataModelValueType.String:
						writer.WriteStringValue(value.AsString());
						break;
					case DataModelValueType.Number:
						writer.WriteNumberValue(value.AsNumber());
						break;
					case DataModelValueType.DateTime:
						writer.WriteStringValue(value.AsDateTime());
						break;
					case DataModelValueType.Boolean:
						writer.WriteBooleanValue(value.AsBoolean());
						break;
					case DataModelValueType.Object:
						JsonSerializer.Serialize(writer, value.AsObject(), options);
						break;
					case DataModelValueType.Array:
						JsonSerializer.Serialize(writer, value.AsArray(), options);
						break;
					default:
						Infrastructure.UnexpectedValue();
						break;
				}
			}
		}

		private class JsonObjectConverter : JsonConverter<DataModelObject>
		{
			public override DataModelObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				var obj = new DataModelObject();

				if (reader.TokenType != JsonTokenType.StartObject)
				{
					throw new JsonException();
				}

				reader.Read();

				while (reader.TokenType != JsonTokenType.EndObject)
				{
					var name = reader.GetString();
					var value = JsonSerializer.Deserialize<DataModelValue>(ref reader, options);

					obj[name] = value;

					reader.Read();
				}

				reader.Read();

				return obj;
			}

			public override void Write(Utf8JsonWriter writer, DataModelObject obj, JsonSerializerOptions options)
			{
				writer.WriteStartObject();

				foreach (var name in obj.Properties)
				{
					var value = obj[name];
					if (!value.IsUndefined())
					{
						writer.WritePropertyName(name);
						JsonSerializer.Serialize(writer, value, options);
					}
				}

				writer.WriteEndObject();
			}
		}

		private class JsonArrayConverter : JsonConverter<DataModelArray>
		{
			public override DataModelArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				var array = new DataModelArray();

				if (reader.TokenType != JsonTokenType.StartArray)
				{
					throw new JsonException();
				}

				reader.Read();

				while (reader.TokenType != JsonTokenType.EndArray)
				{
					var value = JsonSerializer.Deserialize<DataModelValue>(ref reader, options);

					array.Add(value);

					reader.Read();
				}

				reader.Read();

				return array;
			}

			public override void Write(Utf8JsonWriter writer, DataModelArray array, JsonSerializerOptions options)
			{
				writer.WriteStartArray();

				var arrayLength = array.Length;
				for (var i = 0; i < arrayLength; i ++)
				{
					var value = array[i];
					if (!value.IsUndefined())
					{
						JsonSerializer.Serialize(writer, value, options);
					}
				}

				writer.WriteEndArray();
			}
		}
	}
}