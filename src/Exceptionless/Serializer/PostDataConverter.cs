using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Exceptionless.Serializer {

    /// <summary>
    /// Converts JSON objects and arrays in PostData to indented JSON strings on deserialization.
    /// Primitive values (strings, numbers, booleans) pass through as-is.
    /// </summary>
    internal sealed class PostDataConverter : JsonConverter<object> {
        public override bool HandleNull => true;

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            switch (reader.TokenType) {
                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    using (var doc = JsonDocument.ParseValue(ref reader)) {
                        var indentedOptions = new JsonSerializerOptions(options) { WriteIndented = true };
                        return JsonSerializer.Serialize(doc.RootElement, (JsonTypeInfo<JsonElement>)indentedOptions.GetTypeInfo(typeof(JsonElement)));
                    }
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long l))
                        return l;
                    return reader.GetDouble();
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Null:
                    return null;
                default:
                    throw new JsonException($"Unexpected token type: {reader.TokenType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            Type type = value.GetType();
            if (type == typeof(object)) {
                writer.WriteStartObject();
                writer.WriteEndObject();
                return;
            }

            JsonSerializer.Serialize(writer, value, options.GetTypeInfo(type));
        }
    }
}
