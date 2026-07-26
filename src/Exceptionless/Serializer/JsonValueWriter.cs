using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Exceptionless.Extensions;

namespace Exceptionless.Serializer {
    /// <summary>
    /// Writes the filtered JSON used by the public serializer overload that supports
    /// exclusions, depth limits, cycle handling, and best-effort member serialization.
    /// </summary>
    internal sealed class JsonValueWriter {
        private readonly string[] _exclusions;
        private readonly bool _hasExclusions;
        private readonly int _maxDepth;
        private readonly bool _continueOnSerializationError;
        private readonly JsonSerializerOptions _options;
        private readonly HashSet<object> _path = new HashSet<object>(ReferenceComparer.Instance);

        public JsonValueWriter(JsonSerializerOptions options, string[] exclusions, int maxDepth, bool continueOnSerializationError) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _exclusions = exclusions;
            _hasExclusions = exclusions != null && exclusions.Length > 0;
            _maxDepth = maxDepth < 1 ? Int32.MaxValue : maxDepth;
            _continueOnSerializationError = continueOnSerializationError;
        }

        public bool TryWrite(Utf8JsonWriter writer, object value, Type type) {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            return TryWriteValue(writer, null, value, type, type, 0);
        }

        private bool TryWriteValue(Utf8JsonWriter writer, string propertyName, object value, Type type, Type depthType, int currentDepth) {
            try {
                return TryWriteValueCore(writer, propertyName, value, type, depthType, currentDepth);
            } catch (Exception) when (_continueOnSerializationError) {
                return false;
            }
        }

        private bool TryWriteValueCore(Utf8JsonWriter writer, string propertyName, object value, Type type, Type depthType, int currentDepth) {
            if (value == null) {
                bool isPrimitiveType = IsPrimitiveType(depthType);
                if (isPrimitiveType ? currentDepth > _maxDepth : currentDepth >= _maxDepth)
                    return false;

                WritePropertyName(writer, propertyName);
                writer.WriteNullValue();
                return true;
            }

            if (IsPrimitiveType(depthType)) {
                if (currentDepth > _maxDepth)
                    return false;

                JsonTypeInfo primitiveTypeInfo = GetTypeInfo(type);
                if (type.IsEnum) {
                    JsonElement element = JsonSerializer.SerializeToElement(value, primitiveTypeInfo);
                    WritePropertyName(writer, propertyName);
                    element.WriteTo(writer);
                    return true;
                }

                WritePropertyName(writer, propertyName);
                JsonSerializer.Serialize(writer, value, primitiveTypeInfo);
                return true;
            }

            if (currentDepth >= _maxDepth || _path.Contains(value))
                return false;

            if (value is Models.DataDictionary dataDictionary)
                return TryWriteComplex(writer, propertyName, value, () => WriteDataDictionary(writer, dataDictionary, currentDepth));

            if (value is Models.SettingsDictionary settingsDictionary)
                return TryWriteComplex(writer, propertyName, value, () => WriteSettingsDictionary(writer, settingsDictionary));

            if (value is IDictionary dictionary)
                return TryWriteComplex(writer, propertyName, value, () => WriteDictionary(writer, dictionary, currentDepth));

            if (value is IEnumerable enumerable && !(value is string))
                return TryWriteComplex(writer, propertyName, value, () => WriteArray(writer, enumerable, currentDepth));

            JsonTypeInfo typeInfo = GetTypeInfo(type);
            if (typeInfo.Kind != JsonTypeInfoKind.Object) {
                // Converter-backed values are serialized before the parent property name is
                // written, so a converter failure cannot leave invalid partial JSON behind.
                JsonElement element = JsonSerializer.SerializeToElement(value, typeInfo);
                WritePropertyName(writer, propertyName);
                element.WriteTo(writer);
                return true;
            }

            return TryWriteComplex(writer, propertyName, value, () => WriteObject(writer, value, typeInfo, currentDepth));
        }

        private bool TryWriteComplex(Utf8JsonWriter writer, string propertyName, object value, Action writeValue) {
            _path.Add(value);

            try {
                WritePropertyName(writer, propertyName);
                writeValue();
                return true;
            } finally {
                _path.Remove(value);
            }
        }

        private void WriteDataDictionary(Utf8JsonWriter writer, Models.DataDictionary dictionary, int currentDepth) {
            writer.WriteStartObject();
            try {
                foreach (var entry in dictionary) {
                    if (IsExcluded(entry.Key))
                        continue;

                    if (dictionary.IsRawJson(entry.Key, entry.Value))
                        WriteRawJson(writer, entry.Key, (string)entry.Value);
                    else
                        TryWriteChild(writer, entry.Key, entry.Value, currentDepth);
                }
            } catch (Exception) when (_continueOnSerializationError) { }
            writer.WriteEndObject();
        }

        private void WriteSettingsDictionary(Utf8JsonWriter writer, Models.SettingsDictionary dictionary) {
            writer.WriteStartObject();
            try {
                foreach (var entry in dictionary) {
                    if (IsExcluded(entry.Key))
                        continue;

                    writer.WritePropertyName(entry.Key);
                    if (entry.Value == null)
                        writer.WriteNullValue();
                    else
                        writer.WriteStringValue(entry.Value);
                }
            } catch (Exception) when (_continueOnSerializationError) { }
            writer.WriteEndObject();
        }

        private void WriteDictionary(Utf8JsonWriter writer, IDictionary dictionary, int currentDepth) {
            writer.WriteStartObject();
            try {
                foreach (DictionaryEntry entry in dictionary) {
                    string key;
                    try {
                        key = entry.Key?.ToString() ?? String.Empty;
                    } catch (Exception) when (_continueOnSerializationError) {
                        continue;
                    }

                    if (!IsExcluded(key))
                        TryWriteChild(writer, key, entry.Value, currentDepth);
                }
            } catch (Exception) when (_continueOnSerializationError) { }
            writer.WriteEndObject();
        }

        private void WriteArray(Utf8JsonWriter writer, IEnumerable enumerable, int currentDepth) {
            writer.WriteStartArray();
            try {
                foreach (object item in enumerable)
                    TryWriteChild(writer, null, item, currentDepth);
            } catch (Exception) when (_continueOnSerializationError) { }
            writer.WriteEndArray();
        }

        private void WriteObject(Utf8JsonWriter writer, object value, JsonTypeInfo typeInfo, int currentDepth) {
            writer.WriteStartObject();
            foreach (var property in typeInfo.Properties) {
                if (property.Get == null)
                    continue;

                string memberName = property.AttributeProvider is MemberInfo member ? member.Name : property.Name;
                if (IsExcluded(memberName) || IsExcluded(property.Name))
                    continue;

                object propertyValue;
                try {
                    propertyValue = property.Get(value);
                } catch (Exception) when (_continueOnSerializationError) {
                    continue;
                }

                try {
                    if (property.ShouldSerialize != null && !property.ShouldSerialize(value, propertyValue))
                        continue;
                } catch (Exception) when (_continueOnSerializationError) {
                    continue;
                }

                if (property.IsExtensionData) {
                    WriteExtensionData(writer, propertyValue, currentDepth);
                    continue;
                }

                if (property.CustomConverter != null || property.NumberHandling.HasValue) {
                    TryWritePropertyWithOverrides(writer, property, propertyValue, currentDepth + 1);
                    continue;
                }

                Type propertyType = propertyValue?.GetType() ?? property.PropertyType;
                TryWriteValue(writer, property.Name, propertyValue, propertyType, property.PropertyType, currentDepth + 1);
            }
            writer.WriteEndObject();
        }

        private void TryWriteChild(Utf8JsonWriter writer, string propertyName, object value, int currentDepth) {
            Type type = value?.GetType() ?? typeof(object);
            TryWriteValue(writer, propertyName, value, type, type, currentDepth + 1);
        }

        private bool TryWritePropertyWithOverrides(Utf8JsonWriter writer, JsonPropertyInfo property, object value, int currentDepth) {
            try {
                bool isPrimitiveType = IsPrimitiveType(property.PropertyType);
                if (isPrimitiveType ? currentDepth > _maxDepth : currentDepth >= _maxDepth)
                    return false;

                if (value != null && !isPrimitiveType && _path.Contains(value))
                    return false;

                var options = new JsonSerializerOptions(_options);
                if (property.NumberHandling.HasValue)
                    options.NumberHandling = property.NumberHandling.Value;
                if (property.CustomConverter != null)
                    options.Converters.Insert(0, property.CustomConverter);

                JsonElement element = JsonSerializer.SerializeToElement(value, options.GetTypeInfo(property.PropertyType));
                writer.WritePropertyName(property.Name);
                element.WriteTo(writer);
                return true;
            } catch (Exception) when (_continueOnSerializationError) {
                return false;
            }
        }

        private void WriteExtensionData(Utf8JsonWriter writer, object value, int currentDepth) {
            if (value == null)
                return;

            try {
                if (value is IDictionary dictionary) {
                    foreach (DictionaryEntry entry in dictionary) {
                        string key = entry.Key?.ToString() ?? String.Empty;
                        if (!IsExcluded(key))
                            TryWriteChild(writer, key, entry.Value, currentDepth);
                    }
                    return;
                }

                if (value is JsonObject jsonObject) {
                    foreach (var entry in jsonObject) {
                        if (!IsExcluded(entry.Key))
                            TryWriteChild(writer, entry.Key, entry.Value, currentDepth);
                    }
                    return;
                }

                throw new JsonException($"Unsupported extension-data value type '{value.GetType()}'.");
            } catch (Exception) when (_continueOnSerializationError) { }
        }

        private void WriteRawJson(Utf8JsonWriter writer, string propertyName, string json) {
            try {
                using (var document = JsonDocument.Parse(json)) {
                    writer.WritePropertyName(propertyName);
                    document.RootElement.WriteTo(writer);
                }
            } catch (JsonException) {
                writer.WriteString(propertyName, json);
            }
        }

        private JsonTypeInfo GetTypeInfo(Type type) => _options.GetTypeInfo(type);

        private bool IsExcluded(string name) {
            return _hasExclusions && name.AnyWildcardMatches(_exclusions, ignoreCase: true);
        }

        private static void WritePropertyName(Utf8JsonWriter writer, string propertyName) {
            if (propertyName != null)
                writer.WritePropertyName(propertyName);
        }

        private static bool IsPrimitiveType(Type type) {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(Guid)
                || type == typeof(TimeSpan)
                || type == typeof(Uri)
                || type == typeof(byte[]);
        }

        private sealed class ReferenceComparer : IEqualityComparer<object> {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
