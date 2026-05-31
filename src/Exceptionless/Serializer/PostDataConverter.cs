using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Exceptionless.Serializer {

    /// <summary>
    /// Converts JSON objects and arrays in PostData to indented JSON strings on deserialization.
    /// Primitive values (strings, numbers, booleans) pass through as-is.
    /// </summary>
    internal sealed class PostDataConverter : JsonConverter<object> {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            switch (reader.TokenType) {
                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    using (var doc = JsonDocument.ParseValue(ref reader)) {
                        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
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
                    using (var doc2 = JsonDocument.ParseValue(ref reader)) {
                        return doc2.RootElement.Clone();
                    }
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) {
            JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
        }
    }
}
