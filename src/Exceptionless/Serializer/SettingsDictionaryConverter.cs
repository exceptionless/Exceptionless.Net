using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Exceptionless.Models;

namespace Exceptionless.Serializer {
    /// <summary>
    /// Converter for SettingsDictionary which extends ObservableDictionary.
    /// </summary>
    internal sealed class SettingsDictionaryConverter : JsonConverter<SettingsDictionary> {
        public override SettingsDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            var dictionary = new SettingsDictionary();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return dictionary;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName token");

                string key = reader.GetString();
                if (!reader.Read())
                    throw new JsonException("Unexpected end of JSON");

                string value;
                if (reader.TokenType == JsonTokenType.Null) {
                    value = null;
                } else if (reader.TokenType == JsonTokenType.String) {
                    value = reader.GetString();
                } else {
                    using (var doc = JsonDocument.ParseValue(ref reader))
                        value = doc.RootElement.GetRawText();
                }

                dictionary[key] = value;
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, SettingsDictionary value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            foreach (var kvp in value) {
                writer.WritePropertyName(kvp.Key);
                if (kvp.Value == null)
                    writer.WriteNullValue();
                else
                    writer.WriteStringValue(kvp.Value);
            }

            writer.WriteEndObject();
        }
    }
}
