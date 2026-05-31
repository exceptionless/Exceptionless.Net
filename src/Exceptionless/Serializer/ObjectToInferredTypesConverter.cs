using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Exceptionless.Serializer {
    /// <summary>
    /// Deserializes object-typed properties into appropriate .NET types instead of JsonElement.
    /// Matches behavior from the Exceptionless server.
    /// </summary>
    internal sealed class ObjectToInferredTypesConverter : JsonConverter<object> {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            switch (reader.TokenType) {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    return ReadNumber(ref reader);
                case JsonTokenType.String:
                    return ReadString(ref reader);
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.StartObject:
                    return ReadObject(ref reader, options);
                case JsonTokenType.StartArray:
                    return ReadArray(ref reader, options);
                default:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                        return doc.RootElement.Clone();
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            if (value is JsonElement element) {
                element.WriteTo(writer);
                return;
            }

            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static object ReadNumber(ref Utf8JsonReader reader) {
            if (reader.TryGetInt32(out int i))
                return i;
            if (reader.TryGetInt64(out long l))
                return l;
            if (reader.TryGetDecimal(out decimal d))
                return d;
            return reader.GetDouble();
        }

        private static object ReadString(ref Utf8JsonReader reader) {
            if (reader.TryGetDateTimeOffset(out DateTimeOffset dto))
                return dto;
            return reader.GetString();
        }

        private static Dictionary<string, object> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return dictionary;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                string propertyName = reader.GetString() ?? string.Empty;
                if (!reader.Read())
                    continue;

                dictionary[propertyName] = ReadValue(ref reader, options);
            }

            return dictionary;
        }

        private static List<object> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            var list = new List<object>();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return list;

                list.Add(ReadValue(ref reader, options));
            }

            return list;
        }

        private static object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            switch (reader.TokenType) {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Number:
                    return ReadNumber(ref reader);
                case JsonTokenType.String:
                    return ReadString(ref reader);
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.StartObject:
                    return ReadObject(ref reader, options);
                case JsonTokenType.StartArray:
                    return ReadArray(ref reader, options);
                default:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                        return doc.RootElement.Clone();
            }
        }
    }
}
