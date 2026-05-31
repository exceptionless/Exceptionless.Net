using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Exceptionless.Models;

namespace Exceptionless.Serializer {
    /// <summary>
    /// Converter for DataDictionary that preserves nested objects/arrays as JSON strings.
    /// This matches the legacy behavior where complex values in the data dictionary
    /// are stored as their JSON string representation.
    /// </summary>
    internal sealed class DataDictionaryConverter : JsonConverter<DataDictionary> {
        public override DataDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            var dictionary = new DataDictionary();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return dictionary;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName token");

                string key = reader.GetString();
                if (!reader.Read())
                    throw new JsonException("Unexpected end of JSON");

                switch (reader.TokenType) {
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        // Store complex values as JSON strings (legacy behavior)
                        using (var doc = JsonDocument.ParseValue(ref reader))
                            dictionary[key] = doc.RootElement.GetRawText();
                        break;
                    case JsonTokenType.String:
                        dictionary[key] = reader.GetString();
                        break;
                    case JsonTokenType.Number:
                        if (reader.TryGetInt32(out int intVal))
                            dictionary[key] = intVal;
                        else if (reader.TryGetInt64(out long longVal))
                            dictionary[key] = longVal;
                        else if (reader.TryGetDecimal(out decimal decVal))
                            dictionary[key] = decVal;
                        else
                            dictionary[key] = reader.GetDouble();
                        break;
                    case JsonTokenType.True:
                        dictionary[key] = true;
                        break;
                    case JsonTokenType.False:
                        dictionary[key] = false;
                        break;
                    case JsonTokenType.Null:
                        dictionary[key] = null;
                        break;
                    default:
                        throw new JsonException($"Unexpected token type: {reader.TokenType}");
                }
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, DataDictionary value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            foreach (var kvp in value) {
                writer.WritePropertyName(kvp.Key);
                if (kvp.Value == null) {
                    writer.WriteNullValue();
                } else if (kvp.Value is string str && str.Length > 0 && (str[0] == '{' || str[0] == '[')) {
                    // String values that contain JSON (from roundtripping through storage)
                    // must be emitted as raw JSON objects, not escaped strings.
                    try {
                        writer.WriteRawValue(str);
                    } catch (JsonException) {
                        // Not valid JSON - write as a normal string
                        writer.WriteStringValue(str);
                    }
                } else {
                    JsonSerializer.Serialize(writer, kvp.Value, kvp.Value.GetType(), options);
                }
            }

            writer.WriteEndObject();
        }
    }
}
