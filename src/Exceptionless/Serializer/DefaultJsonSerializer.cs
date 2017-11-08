using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Exceptionless.Extensions;
using Exceptionless.Json;
using Exceptionless.Json.Converters;
using Exceptionless.Json.Serialization;

namespace Exceptionless.Serializer {
    public class DefaultJsonSerializer : IJsonSerializer, IStorageSerializer {
        private readonly JsonSerializerSettings _serializerSettings;

        public DefaultJsonSerializer() {
            _serializerSettings = new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                ContractResolver = new ExceptionlessContractResolver()
            };

            _serializerSettings.Converters.Add(new StringEnumConverter());
            _serializerSettings.Converters.Add(new DataDictionaryConverter());
            _serializerSettings.Converters.Add(new RequestInfoConverter());
        }

        public virtual void Serialize<T>(T data, Stream outputStream) {
            var buffer = System.Text.Encoding.UTF8.GetBytes(Serialize(data));
            outputStream.Write(buffer,0, buffer.Length);
        }

        public virtual T Deserialize<T>(Stream inputStream) {
            using (var memory = new MemoryStream()) {
                inputStream.CopyTo(memory);
                var buffer = memory.ToArray();
                return (T)Deserialize(System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length), typeof(T));
            }
        }

        public virtual string Serialize(object model, string[] exclusions = null, int maxDepth = 10, bool continueOnSerializationError = true) {
            if (model == null)
                return null;

            JsonSerializer serializer = JsonSerializer.Create(_serializerSettings);
            if (maxDepth < 1)
                maxDepth = Int32.MaxValue;

            using (var sw = new StringWriter()) {
                using (var jw = new JsonTextWriterWithDepth(sw)) {
                    jw.Formatting = Formatting.None;
                    Func<JsonProperty, object, bool> include = (property, value) => ShouldSerialize(jw, property, value, maxDepth, exclusions);
                    var resolver = new ExceptionlessContractResolver(include);
                    serializer.ContractResolver = resolver;
                    if (continueOnSerializationError)
                        serializer.Error += (sender, args) => { args.ErrorContext.Handled = true; };

                    serializer.Serialize(jw, model);
                }

                return sw.ToString();
            }
        }

        public virtual object Deserialize(string json, Type type) {
            if (String.IsNullOrWhiteSpace(json))
                return null;

            return JsonConvert.DeserializeObject(json, type, _serializerSettings);
        }

        private bool ShouldSerialize(JsonTextWriterWithDepth jw, JsonProperty property, object obj, int maxDepth, IEnumerable<string> excludedPropertyNames) {
            try {
                if (excludedPropertyNames != null && (property.UnderlyingName.AnyWildcardMatches(excludedPropertyNames, true) || property.PropertyName.AnyWildcardMatches(excludedPropertyNames, true)))
                    return false;

                bool isPrimitiveType = DefaultContractResolver.IsJsonPrimitiveType(property.PropertyType);
                bool isPastMaxDepth = !(isPrimitiveType ? jw.CurrentDepth <= maxDepth : jw.CurrentDepth < maxDepth);
                if (isPastMaxDepth)
                    return false;

                if (isPrimitiveType)
                    return true;

                object value = property.ValueProvider.GetValue(obj);
                if (value == null)
                    return true;

                if (typeof(ICollection).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetTypeInfo())) {
                    var collection = value as ICollection;
                    if (collection != null)
                        return collection.Count > 0;
                }

                var collectionType = value.GetType().GetInterfaces().FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
                if (collectionType != null) {
                    var countProperty = collectionType.GetProperty("Count");
                    if (countProperty != null)
                        return (int)countProperty.GetValue(value, null) > 0;
                }
            } catch (Exception) {}

            return true;
        }
    }
}