using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
                FloatParseHandling = FloatParseHandling.Decimal,
                ContractResolver = new ExceptionlessContractResolver()
            };

            _serializerSettings.Converters.Add(new StringEnumConverter());
            _serializerSettings.Converters.Add(new DataDictionaryConverter());
            _serializerSettings.Converters.Add(new RequestInfoConverter());
        }

        public virtual void Serialize<T>(T data, Stream outputStream) {
            using (var writer = new StreamWriter(outputStream, new UTF8Encoding(false, true), 0x400, true)) {
                writer.Write(Serialize(data));
            }
        }

        public virtual T Deserialize<T>(Stream inputStream) {
            using (var reader = new StreamReader(inputStream, Encoding.UTF8, true, 0x400, true)) {
                return (T)Deserialize(reader.ReadToEnd(), typeof(T));
            }
        }

        public virtual string Serialize(object model, string[] exclusions = null, int maxDepth = 10, bool continueOnSerializationError = true) {
            if (model == null)
                return null;

            var serializer = JsonSerializer.Create(_serializerSettings);
            if (maxDepth < 1)
                maxDepth = Int32.MaxValue;

            using (var sw = new StringWriter()) {
                using (var jw = new JsonTextWriterWithExclusions(sw, exclusions)) {
                    Func<JsonProperty, object, bool> include = (property, value) => ShouldSerialize(jw, property, value, maxDepth, exclusions);
                    serializer.ContractResolver = new ExceptionlessContractResolver(include);
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

        private bool ShouldSerialize(JsonTextWriterWithDepth jw, JsonProperty property, object obj, int maxDepth, string[] excludedPropertyNames) {
            try {
                if (excludedPropertyNames != null && excludedPropertyNames.Length > 0 && (property.UnderlyingName.AnyWildcardMatches(excludedPropertyNames, ignoreCase: true) || property.PropertyName.AnyWildcardMatches(excludedPropertyNames, ignoreCase: true)))
                    return false;

                bool isPrimitiveType = DefaultContractResolver.IsJsonPrimitiveType(property.PropertyType);
                bool isPastMaxDepth = !(isPrimitiveType ? jw.CurrentDepth <= maxDepth : jw.CurrentDepth < maxDepth);
                if (isPastMaxDepth)
                    return false;
            } catch (Exception) {}

            return true;
        }
    }
}