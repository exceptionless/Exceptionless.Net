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
using Exceptionless.Json;

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

        private bool TryWriteValue(Utf8JsonWriter writer, string propertyName, object value, Type type, Type depthType, int currentDepth, Type declaredContractType = null) {
            try {
                return TryWriteValueCore(writer, propertyName, value, type, depthType, currentDepth, declaredContractType);
            } catch (Exception) when (_continueOnSerializationError) {
                return false;
            }
        }

        private bool TryWriteValueCore(Utf8JsonWriter writer, string propertyName, object value, Type type, Type depthType, int currentDepth, Type declaredContractType) {
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

            Type polymorphicContractType = declaredContractType ?? depthType;
            JsonTypeInfo typeInfo = ResolveContractTypeInfo(polymorphicContractType, type, out string typeDiscriminatorPropertyName, out object typeDiscriminator);

            if (typeInfo.Kind == JsonTypeInfoKind.None) {
                if (IsJsonDomType(typeInfo.Type))
                    return TryWriteSerializedJson(writer, propertyName, value, typeInfo, currentDepth);

                return TryWriteSerializedValue(writer, propertyName, value, typeInfo);
            }

            if (typeInfo.Kind == JsonTypeInfoKind.Dictionary) {
                if (CanWriteDictionaryEntriesDirectly(typeInfo, value))
                    return TryWriteComplex(writer, propertyName, value,
                        () => WriteDictionary(writer, (IDictionary)value, currentDepth, typeInfo.ElementType));

                // STJ owns dictionary key formatting (including DictionaryKeyPolicy and
                // converter WriteAsPropertyName overrides). Materialize its canonical JSON,
                // then apply exclusions and depth to the emitted property names.
                return TryWriteSerializedJson(writer, propertyName, value, typeInfo, currentDepth);
            }

            if (typeInfo.Kind == JsonTypeInfoKind.Enumerable && value is IEnumerable enumerable)
                return TryWriteComplex(writer, propertyName, value, () => WriteArray(writer, enumerable, currentDepth, typeInfo.ElementType));

            ValidateTypeDiscriminator(typeInfo, typeDiscriminatorPropertyName);
            return TryWriteObjectContract(writer, propertyName, value, typeInfo,
                () => WriteObject(writer, value, typeInfo, currentDepth, typeDiscriminatorPropertyName, typeDiscriminator));
        }

        private JsonTypeInfo ResolveContractTypeInfo(Type contractType, Type runtimeType, out string discriminatorPropertyName, out object discriminator) {
            discriminatorPropertyName = null;
            discriminator = null;

            if (contractType == null || contractType == typeof(object) || contractType == runtimeType) {
                if (TryGetTypeInfo(runtimeType, out JsonTypeInfo runtimeTypeInfo))
                    return runtimeTypeInfo;

                throw new NotSupportedException($"No System.Text.Json metadata is registered for '{runtimeType}'.");
            }

            JsonTypeInfo declaredTypeInfo = GetTypeInfo(contractType);
            return ResolvePolymorphicTypeInfo(declaredTypeInfo, runtimeType, out discriminatorPropertyName, out discriminator)
                ?? declaredTypeInfo;
        }

        private bool TryGetTypeInfo(Type type, out JsonTypeInfo typeInfo) {
            try {
                typeInfo = GetTypeInfo(type);
                return true;
            } catch (NotSupportedException) {
                typeInfo = null;
                return false;
            }
        }

        private bool TryWriteSerializedValue(Utf8JsonWriter writer, string propertyName, object value, JsonTypeInfo typeInfo) {
            // Converter-backed values are serialized before the parent property name is
            // written, so a converter failure cannot leave invalid partial JSON behind.
            JsonElement element = JsonSerializer.SerializeToElement(value, typeInfo);
            WritePropertyName(writer, propertyName);
            element.WriteTo(writer);
            return true;
        }

        private bool TryWriteSerializedJson(Utf8JsonWriter writer, string propertyName, object value, JsonTypeInfo typeInfo, int currentDepth) {
            JsonElement element = JsonSerializer.SerializeToElement(value, typeInfo);
            return TryWriteJsonElement(writer, propertyName, element, currentDepth);
        }

        private static bool IsJsonDomType(Type type) {
            return type == typeof(JsonElement)
                || type == typeof(JsonDocument)
                || type == typeof(JsonNode)
                || type == typeof(JsonObject)
                || type == typeof(JsonArray)
                || type == typeof(JsonValue);
        }

        private bool CanWriteDictionaryEntriesDirectly(JsonTypeInfo typeInfo, object value) {
            if (typeInfo.KeyType != typeof(string)
                || !(value is IDictionary))
                return false;

            foreach (JsonConverter converter in _options.Converters) {
                if (converter.CanConvert(typeof(string)))
                    return false;
            }

            return true;
        }

        private bool TryWriteObjectContract(Utf8JsonWriter writer, string propertyName, object value, JsonTypeInfo typeInfo, Action writeValue) {
            typeInfo.OnSerializing?.Invoke(value);
            bool wroteValue = false;
            try {
                wroteValue = TryWriteComplex(writer, propertyName, value, writeValue);
                return wroteValue;
            } finally {
                if (wroteValue) {
                    try {
                        typeInfo.OnSerialized?.Invoke(value);
                    } catch (Exception) when (_continueOnSerializationError) { }
                }
            }
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
                        WriteRawJson(writer, entry.Key, (string)entry.Value, currentDepth + 1);
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

        private void WriteDictionary(Utf8JsonWriter writer, IDictionary dictionary, int currentDepth, Type declaredValueType) {
            writer.WriteStartObject();
            try {
                foreach (DictionaryEntry entry in dictionary) {
                    string key = (string)entry.Key;
                    if (_options.DictionaryKeyPolicy != null)
                        key = _options.DictionaryKeyPolicy.ConvertName(key);

                    if (!IsExcluded(key))
                        TryWriteChild(writer, key, entry.Value, currentDepth, declaredValueType, useRuntimeDepthType: true);
                }
            } catch (Exception) when (_continueOnSerializationError) { }
            writer.WriteEndObject();
        }

        private void WriteArray(Utf8JsonWriter writer, IEnumerable enumerable, int currentDepth, Type declaredElementType) {
            writer.WriteStartArray();
            try {
                foreach (object item in enumerable)
                    TryWriteChild(writer, null, item, currentDepth, declaredElementType, useRuntimeDepthType: true);
            } catch (Exception) when (_continueOnSerializationError) { }
            writer.WriteEndArray();
        }

        private void WriteObject(Utf8JsonWriter writer, object value, JsonTypeInfo typeInfo, int currentDepth, string typeDiscriminatorPropertyName, object typeDiscriminator) {
            writer.WriteStartObject();
            WriteTypeDiscriminator(writer, typeDiscriminatorPropertyName, typeDiscriminator);
            foreach (var property in typeInfo.Properties) {
                if (property.Get == null
                    || property.AttributeProvider?.IsDefined(typeof(ExceptionlessIgnoreAttribute), true) == true)
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

        private JsonTypeInfo ResolvePolymorphicTypeInfo(JsonTypeInfo declaredTypeInfo, Type runtimeType, out string discriminatorPropertyName, out object discriminator) {
            discriminatorPropertyName = null;
            discriminator = null;

            JsonPolymorphismOptions polymorphism = declaredTypeInfo.PolymorphismOptions;
            if (polymorphism == null)
                return null;

            JsonDerivedType selected = default;
            bool found = false;
            foreach (JsonDerivedType candidate in polymorphism.DerivedTypes) {
                if (candidate.DerivedType != runtimeType)
                    continue;

                selected = candidate;
                found = true;
                break;
            }

            if (!found && polymorphism.UnknownDerivedTypeHandling == JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor) {
                foreach (JsonDerivedType candidate in polymorphism.DerivedTypes) {
                    if (!candidate.DerivedType.IsAssignableFrom(runtimeType))
                        continue;

                    if (!found || selected.DerivedType.IsAssignableFrom(candidate.DerivedType)) {
                        selected = candidate;
                        found = true;
                    } else if (!candidate.DerivedType.IsAssignableFrom(selected.DerivedType)) {
                        throw new NotSupportedException($"Runtime type '{runtimeType}' has multiple equally-near polymorphic ancestors for '{declaredTypeInfo.Type}'.");
                    }
                }
            }

            if (!found) {
                if (polymorphism.UnknownDerivedTypeHandling == JsonUnknownDerivedTypeHandling.FailSerialization)
                    throw new NotSupportedException($"Runtime type '{runtimeType}' is not registered as a derived type of '{declaredTypeInfo.Type}'.");

                return declaredTypeInfo;
            }

            discriminatorPropertyName = polymorphism.TypeDiscriminatorPropertyName;
            discriminator = selected.TypeDiscriminator;
            return GetTypeInfo(selected.DerivedType);
        }

        private static void WriteTypeDiscriminator(Utf8JsonWriter writer, string propertyName, object discriminator) {
            if (propertyName == null || discriminator == null)
                return;

            if (discriminator is string stringDiscriminator) {
                writer.WriteString(propertyName, stringDiscriminator);
                return;
            }

            if (discriminator is int integerDiscriminator) {
                writer.WriteNumber(propertyName, integerDiscriminator);
                return;
            }

            throw new NotSupportedException($"Unsupported JSON type discriminator '{discriminator}'.");
        }

        private static void ValidateTypeDiscriminator(JsonTypeInfo typeInfo, string propertyName) {
            if (propertyName == null)
                return;

            foreach (JsonPropertyInfo property in typeInfo.Properties) {
                if (String.Equals(property.Name, propertyName, StringComparison.Ordinal))
                    throw new InvalidOperationException($"The polymorphic type discriminator '{propertyName}' conflicts with a property on '{typeInfo.Type}'.");
            }
        }

        private void TryWriteChild(Utf8JsonWriter writer, string propertyName, object value, int currentDepth, Type declaredType = null, bool useRuntimeDepthType = false) {
            Type type = value?.GetType() ?? typeof(object);
            Type depthType = useRuntimeDepthType && value != null ? type : declaredType ?? type;
            TryWriteValue(writer, propertyName, value, type, depthType, currentDepth + 1, declaredType);
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

        private void WriteRawJson(Utf8JsonWriter writer, string propertyName, string json, int currentDepth) {
            try {
                using (var document = JsonDocument.Parse(json))
                    TryWriteJsonElement(writer, propertyName, document.RootElement, currentDepth);
            } catch (JsonException) {
                TryWriteValue(writer, propertyName, json, typeof(string), typeof(string), currentDepth);
            }
        }

        private bool TryWriteJsonElement(Utf8JsonWriter writer, string propertyName, JsonElement element, int currentDepth) {
            bool isComplex = element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array;
            if (isComplex ? currentDepth >= _maxDepth : currentDepth > _maxDepth)
                return false;

            WritePropertyName(writer, propertyName);
            if (element.ValueKind == JsonValueKind.Object) {
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject()) {
                    if (!IsExcluded(property.Name))
                        TryWriteJsonElement(writer, property.Name, property.Value, currentDepth + 1);
                }
                writer.WriteEndObject();
                return true;
            }

            if (element.ValueKind == JsonValueKind.Array) {
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                    TryWriteJsonElement(writer, null, item, currentDepth + 1);
                writer.WriteEndArray();
                return true;
            }

            if (element.ValueKind == JsonValueKind.Undefined)
                writer.WriteNullValue();
            else
                element.WriteTo(writer);
            return true;
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
