using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Exceptionless.Extensions;
using Exceptionless.Json;

namespace Exceptionless.Serializer {
    public class DefaultJsonSerializer : IJsonSerializer, IStorageSerializer {
        private readonly JsonSerializerOptions _serializerOptions;

        public DefaultJsonSerializer() : this(null) { }

        public DefaultJsonSerializer(IJsonTypeInfoResolver typeInfoResolver) {
            _serializerOptions = new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            _serializerOptions.Converters.Add(new DataDictionaryConverter());
            _serializerOptions.Converters.Add(new SettingsDictionaryConverter());

            _serializerOptions.TypeInfoResolverChain.Add(new CompatibilityResolver(ExceptionlessJsonSerializerContext.Default));
            if (typeInfoResolver != null)
                _serializerOptions.TypeInfoResolverChain.Add(new CompatibilityResolver(typeInfoResolver));

#if NET8_0_OR_GREATER
            if (RuntimeFeature.IsDynamicCodeSupported && JsonSerializer.IsReflectionEnabledByDefault)
#else
            if (JsonSerializer.IsReflectionEnabledByDefault)
#endif
                AddReflectionFallback();
        }

        public virtual void Serialize<T>(T data, Stream outputStream) {
            JsonSerializer.Serialize(outputStream, data, GetTypeInfo<T>());
        }

        public virtual T Deserialize<T>(Stream inputStream) {
            return JsonSerializer.Deserialize(inputStream, GetTypeInfo<T>());
        }

        public virtual string Serialize(object model, string[] exclusions = null, int maxDepth = 10, bool continueOnSerializationError = true) {
            if (model == null)
                return null;

            if (maxDepth < 1)
                maxDepth = Int32.MaxValue;

            bool hasExclusions = exclusions != null && exclusions.Length > 0;
            bool hasDepthLimit = maxDepth != Int32.MaxValue;

            if (!hasExclusions && !hasDepthLimit) {
                return JsonSerializer.Serialize(model, GetTypeInfo(model.GetType()));
            }

            try {
                using (var stream = new System.IO.MemoryStream()) {
                    using (var writer = new Utf8JsonWriter(stream)) {
                        TryWriteValue(writer, model, model.GetType(), exclusions, hasExclusions, maxDepth, 0, new HashSet<object>(ReferenceComparer.Instance));
                    }
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            } catch (Exception) when (continueOnSerializationError) {
                try {
                    return JsonSerializer.Serialize(model, GetTypeInfo(model.GetType()));
                } catch (Exception) {
                    return null;
                }
            }
        }

        private bool TryWriteValue(Utf8JsonWriter writer, object value, Type type, string[] exclusions, bool hasExclusions, int maxDepth, int currentDepth, HashSet<object> path) {
            if (value == null) {
                writer.WriteNullValue();
                return true;
            }

            if (IsPrimitiveType(type)) {
                if (currentDepth > maxDepth)
                    return false;

                JsonSerializer.Serialize(writer, value, GetTypeInfo(type));
                return true;
            }

            if (currentDepth >= maxDepth || !path.Add(value))
                return false;

            try {
                if (value is Models.DataDictionary dataDictionary) {
                    writer.WriteStartObject();
                    foreach (var entry in dataDictionary) {
                        if (hasExclusions && entry.Key.AnyWildcardMatches(exclusions, ignoreCase: true))
                            continue;

                        Type entryType = entry.Value?.GetType() ?? typeof(object);
                        if (!CanWriteValue(entry.Value, entryType, maxDepth, currentDepth + 1, path))
                            continue;

                        writer.WritePropertyName(entry.Key);
                        if (dataDictionary.IsRawJson(entry.Key, entry.Value))
                            WriteRawJsonValue(writer, (string)entry.Value);
                        else
                            TryWriteValue(writer, entry.Value, entryType, exclusions, hasExclusions, maxDepth, currentDepth + 1, path);
                    }
                    writer.WriteEndObject();
                    return true;
                }

                if (value is Models.SettingsDictionary settingsDictionary) {
                    writer.WriteStartObject();
                    foreach (var entry in settingsDictionary) {
                        if (hasExclusions && entry.Key.AnyWildcardMatches(exclusions, ignoreCase: true))
                            continue;

                        writer.WritePropertyName(entry.Key);
                        if (entry.Value == null)
                            writer.WriteNullValue();
                        else
                            writer.WriteStringValue(entry.Value);
                    }
                    writer.WriteEndObject();
                    return true;
                }

                if (value is IDictionary dict) {
                    writer.WriteStartObject();
                    foreach (DictionaryEntry entry in dict) {
                        string key = entry.Key?.ToString() ?? "";
                        if (hasExclusions && key.AnyWildcardMatches(exclusions, ignoreCase: true))
                            continue;

                        Type entryType = entry.Value?.GetType() ?? typeof(object);
                        if (!CanWriteValue(entry.Value, entryType, maxDepth, currentDepth + 1, path))
                            continue;

                        writer.WritePropertyName(key);
                        TryWriteValue(writer, entry.Value, entryType, exclusions, hasExclusions, maxDepth, currentDepth + 1, path);
                    }
                    writer.WriteEndObject();
                    return true;
                }

                if (value is IEnumerable enumerable && !(value is string)) {
                    writer.WriteStartArray();
                    foreach (object item in enumerable) {
                        Type itemType = item?.GetType() ?? typeof(object);
                        if (CanWriteValue(item, itemType, maxDepth, currentDepth + 1, path))
                            TryWriteValue(writer, item, itemType, exclusions, hasExclusions, maxDepth, currentDepth + 1, path);
                    }
                    writer.WriteEndArray();
                    return true;
                }

                JsonTypeInfo typeInfo = null;
                try {
                    typeInfo = _serializerOptions.GetTypeInfo(type);
                } catch { }

                if (typeInfo == null || typeInfo.Kind != JsonTypeInfoKind.Object) {
                    JsonSerializer.Serialize(writer, value, GetTypeInfo(type));
                    return true;
                }

                writer.WriteStartObject();
                foreach (var prop in typeInfo.Properties) {
                    if (prop.Get == null)
                        continue;

                    string memberName = prop.AttributeProvider is MemberInfo mi ? mi.Name : prop.Name;
                    if (prop.AttributeProvider?.IsDefined(typeof(ExceptionlessIgnoreAttribute), true) == true)
                        continue;

                    if (hasExclusions && (memberName.AnyWildcardMatches(exclusions, ignoreCase: true) || prop.Name.AnyWildcardMatches(exclusions, ignoreCase: true)))
                        continue;

                    object propValue = null;
                    try { propValue = prop.Get(value); } catch { continue; }

                    Type propType = propValue?.GetType() ?? prop.PropertyType;
                    if (!CanWriteValue(propValue, propType, maxDepth, currentDepth + 1, path))
                        continue;

                    writer.WritePropertyName(prop.Name);
                    TryWriteValue(writer, propValue, propType, exclusions, hasExclusions, maxDepth, currentDepth + 1, path);
                }
                writer.WriteEndObject();
                return true;
            } finally {
                path.Remove(value);
            }
        }

        private static bool CanWriteValue(object value, Type type, int maxDepth, int currentDepth, HashSet<object> path) {
            if (value == null || IsPrimitiveType(type))
                return currentDepth <= maxDepth;

            return currentDepth < maxDepth && !path.Contains(value);
        }

        private static void WriteRawJsonValue(Utf8JsonWriter writer, string json) {
            try {
                writer.WriteRawValue(json);
            } catch (JsonException) {
                writer.WriteStringValue(json);
            }
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

            return JsonSerializer.Deserialize(json, GetTypeInfo(type));
        }

        private JsonTypeInfo GetTypeInfo(Type type) {
            return _serializerOptions.GetTypeInfo(type);
        }

        private JsonTypeInfo<T> GetTypeInfo<T>() {
            return (JsonTypeInfo<T>)GetTypeInfo(typeof(T));
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "This reflection fallback is guarded by RuntimeFeature.IsDynamicCodeSupported and cannot run in NativeAOT applications.")]
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Reflection is disabled by default in trimmed applications; applications that opt back in must preserve their reflected payload types.")]
        private void AddReflectionFallback() {
            _serializerOptions.Converters.Add(new JsonStringEnumConverter());
            _serializerOptions.TypeInfoResolverChain.Add(new CompatibilityResolver(new DefaultJsonTypeInfoResolver()));
        }

        private sealed class CompatibilityResolver : IJsonTypeInfoResolver {
            private readonly IJsonTypeInfoResolver _inner;

            public CompatibilityResolver(IJsonTypeInfoResolver inner) {
                _inner = inner;
            }

            public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options) {
                JsonTypeInfo typeInfo = _inner.GetTypeInfo(type, options);
                if (typeInfo?.Kind != JsonTypeInfoKind.Object)
                    return typeInfo;

                for (int index = typeInfo.Properties.Count - 1; index >= 0; index--) {
                    if (typeInfo.Properties[index].AttributeProvider?.IsDefined(typeof(ExceptionlessIgnoreAttribute), true) == true)
                        typeInfo.Properties.RemoveAt(index);
                }

                return typeInfo;
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object> {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }

    }
}
