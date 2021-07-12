using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Exceptionless.Serializer {

    internal sealed class DictionaryConverterFactory : JsonConverterFactory {
        private readonly static ConcurrentDictionary<Type, JsonConverter> s_lookUp = new();
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert.IsGenericType &&
               typeToConvert.GetGenericArguments()[0] == typeof(string) &&
               IsAssignableToGenericType(typeToConvert, typeof(IDictionary<,>));

        public static bool IsAssignableToGenericType(Type givenType, Type genericType) {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes) {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
            var lookUp = s_lookUp;
            if (lookUp.TryGetValue(typeToConvert, out var value)) return value;

            var genericArgs = typeToConvert.GetGenericArguments();
            Type elementType = genericArgs[1];

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(DictionaryConverter<>)
                    .MakeGenericType(new Type[] { elementType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            lookUp[typeToConvert] = converter;
            return converter;
        }
    }

    internal sealed class DictionaryConverter<TValue> : MaxDepthJsonConverter<IDictionary<string, TValue>> {
        public override void Write(Utf8JsonWriter writer, IDictionary<string, TValue> value, JsonSerializerOptions options) {
            // Skip empty collections
            if (value is null || value.Count == 0) return;

            writer.WriteStartObject();

            var namingPolicy = options.PropertyNamingPolicy;
            var naming = namingPolicy as SnakeCaseNamingPolicy;

            foreach (var kv in value) {
                if (!naming?.IsNameAllowed(kv.Key) ?? true) continue;

                // We don't rename dictionary keys
                writer.WritePropertyName(kv.Key);

                JsonSerializer.Serialize(writer, kv.Value, options);
            }

            writer.WriteEndObject();
        }
    }


    internal sealed class MaxDepthJsonConverterFactory : JsonConverterFactory {
        private readonly static ConcurrentDictionary<Type, JsonConverter> s_lookUp = new();

        public override bool CanConvert(Type typeToConvert)
            => !typeToConvert.IsValueType &&
               typeToConvert != typeof(object) &&
               !typeToConvert.IsArray &&
               typeToConvert != typeof(string) &&
               !(typeToConvert.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(typeToConvert.GetGenericTypeDefinition())) &&
               !typeof(IEnumerable).IsAssignableFrom(typeToConvert);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
            var lookUp = s_lookUp;
            if (lookUp.TryGetValue(typeToConvert, out var value)) return value;

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(MaxDepthJsonConverter<>)
                    .MakeGenericType(new Type[] { typeToConvert }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            lookUp[typeToConvert] = converter;
            return converter;
        }
    }

    internal class MaxDepthJsonConverter {
        private readonly static ConcurrentDictionary<Type, (FieldInfo[], PropertyInfo[])> s_lookUp = new();

        public static (FieldInfo[], PropertyInfo[]) GetFieldsAndProperties(Type type) {
            var lookUp = s_lookUp;
            if (lookUp.TryGetValue(type, out var value)) return value;

            value = (
                type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance).ToArray(),
                type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0).ToArray()
            );

            lookUp[type] = value;
            return value;
        }
    }

    internal class MaxDepthJsonConverter<T> : JsonConverter<T> {
        public sealed override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T objectValue, JsonSerializerOptions options) {
            writer.WriteStartObject();

            var (fields, properties) = MaxDepthJsonConverter.GetFieldsAndProperties(objectValue.GetType());

            var namingPolicy = options.PropertyNamingPolicy;
            for (int i = 0; i < fields.Length; i++) {
                var field = fields[i];
                var type = field.FieldType;
                if (type.IsClass && !type.IsArray && type != typeof(string) && writer.CurrentDepth >= options.MaxDepth - DefaultJsonSerializer.MaxDepthBuffer) {
                    continue;
                }
                var value = field.GetValueDirect(__makeref(objectValue));
                if (value is ICollection col && col.Count == 0) {
                    continue;
                }
                // TODO: Attribute for name?
                if (namingPolicy is SnakeCaseNamingPolicy naming && !naming.IsNameAllowed(field.Name)) {
                    continue;
                }

                writer.WritePropertyName(namingPolicy.ConvertName(field.Name));
                
                JsonSerializer.Serialize(writer, value, options);
            }

            for (int i = 0; i < properties.Length; i++) {
                var property = properties[i];
                var type = property.PropertyType;
                if (!type.IsValueType && !type.IsArray && type != typeof(string) && writer.CurrentDepth >= options.MaxDepth - DefaultJsonSerializer.MaxDepthBuffer) {
                    continue;
                }
                var value = property.GetValue(objectValue);
                if (value is ICollection col && col.Count == 0) {
                    continue;
                }
                // TODO: Attribute for name?
                if (namingPolicy is SnakeCaseNamingPolicy naming && !naming.IsNameAllowed(property.Name)) {
                    continue;
                }

                writer.WritePropertyName(namingPolicy.ConvertName(property.Name));
                JsonSerializer.Serialize(writer, value, options);
            }

            writer.WriteEndObject();
        }
    }
}
