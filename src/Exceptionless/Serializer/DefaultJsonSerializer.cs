using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Exceptionless.Extensions;

namespace Exceptionless.Serializer {
    public class DefaultJsonSerializer : IJsonSerializer, IStorageSerializer {
        private readonly JsonSerializerOptions _serializerOptions;

        public DefaultJsonSerializer() {
            _serializerOptions = new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            _serializerOptions.Converters.Add(new JsonStringEnumConverter());
            _serializerOptions.Converters.Add(new DataDictionaryConverter());
            _serializerOptions.Converters.Add(new SettingsDictionaryConverter());
        }

        public virtual void Serialize<T>(T data, Stream outputStream) {
            JsonSerializer.Serialize(outputStream, data, _serializerOptions);
        }

        public virtual T Deserialize<T>(Stream inputStream) {
            return JsonSerializer.Deserialize<T>(inputStream, _serializerOptions);
        }

        public virtual string Serialize(object model, string[] exclusions = null, int maxDepth = 10, bool continueOnSerializationError = true) {
            if (model == null)
                return null;

            if (maxDepth < 1)
                maxDepth = Int32.MaxValue;

            bool hasExclusions = exclusions != null && exclusions.Length > 0;
            bool hasDepthLimit = maxDepth != Int32.MaxValue;

            if (!hasExclusions && !hasDepthLimit) {
                return JsonSerializer.Serialize(model, model.GetType(), _serializerOptions);
            }

            try {
                using (var stream = new System.IO.MemoryStream()) {
                    using (var writer = new Utf8JsonWriter(stream)) {
                        WriteValue(writer, model, model.GetType(), exclusions, hasExclusions, maxDepth, 0);
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            } catch (Exception) when (continueOnSerializationError) {
                try {
                    return JsonSerializer.Serialize(model, model.GetType(), _serializerOptions);
                } catch (Exception) {
                    return null;
                }
            }
        }

        private void WriteValue(Utf8JsonWriter writer, object value, Type type, string[] exclusions, bool hasExclusions, int maxDepth, int currentDepth) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            // For primitive-like types, serialize directly
            if (IsPrimitiveType(type)) {
                JsonSerializer.Serialize(writer, value, type, _serializerOptions);
                return;
            }

            // Check if we're past max depth for complex types
            if (currentDepth >= maxDepth) {
                return;
            }

            // Handle DataDictionary with its converter (preserves JSON string behavior for complex values)
            if (value is Models.DataDictionary dataDictionary) {
                JsonSerializer.Serialize(writer, dataDictionary, _serializerOptions);
                return;
            }

            // Handle dictionaries (non-generic IDictionary)
            if (value is IDictionary dict) {
                writer.WriteStartObject();
                foreach (DictionaryEntry entry in dict) {
                    string key = entry.Key?.ToString() ?? "";
                    if (hasExclusions && key.AnyWildcardMatches(exclusions, ignoreCase: true))
                        continue;
                    writer.WritePropertyName(key);
                    if (entry.Value == null)
                        writer.WriteNullValue();
                    else
                        WriteValue(writer, entry.Value, entry.Value.GetType(), exclusions, hasExclusions, maxDepth, currentDepth + 1);
                }
                writer.WriteEndObject();
                return;
            }

            // Handle enumerables (arrays, lists, etc.) - serialize directly
            if (value is IEnumerable && !(value is string)) {
                JsonSerializer.Serialize(writer, value, type, _serializerOptions);
                return;
            }

            // For complex objects, get type info first to decide path
            JsonTypeInfo typeInfo = null;
            try {
                typeInfo = _serializerOptions.GetTypeInfo(type);
            } catch { }

            if (typeInfo == null || typeInfo.Kind != JsonTypeInfoKind.Object) {
                // Fallback: direct serialization
                JsonSerializer.Serialize(writer, value, type, _serializerOptions);
                return;
            }

            // Write object with property filtering
            writer.WriteStartObject();
            foreach (var prop in typeInfo.Properties) {
                if (prop.Get == null)
                    continue;

                string memberName = prop.AttributeProvider is MemberInfo mi ? mi.Name : prop.Name;

                if (hasExclusions && (memberName.AnyWildcardMatches(exclusions, ignoreCase: true) || prop.Name.AnyWildcardMatches(exclusions, ignoreCase: true)))
                    continue;

                object propValue = null;
                try { propValue = prop.Get(value); } catch { continue; }

                Type propType = prop.PropertyType;
                bool isPrimitive = IsPrimitiveType(propType);

                // Depth check: primitives at <= maxDepth, complex at < maxDepth
                if (isPrimitive) {
                    if (currentDepth + 1 > maxDepth)
                        continue;
                } else {
                    if (currentDepth + 1 >= maxDepth)
                        continue;
                }

                writer.WritePropertyName(prop.Name);
                if (propValue == null) {
                    writer.WriteNullValue();
                } else if (isPrimitive) {
                    JsonSerializer.Serialize(writer, propValue, propType, _serializerOptions);
                } else {
                    WriteValue(writer, propValue, propType, exclusions, hasExclusions, maxDepth, currentDepth + 1);
                }
            }
            writer.WriteEndObject();
        }

        private static bool IsPrimitiveType(Type type) {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(Guid)
                || type == typeof(TimeSpan)
                || type.IsEnum;
        }

        public virtual object Deserialize(string json, Type type) {
            if (String.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize(json, type, _serializerOptions);
        }

    }
}
