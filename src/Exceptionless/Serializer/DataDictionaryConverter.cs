using System;
using Exceptionless.Models;
using Exceptionless.Json;
using Exceptionless.Json.Converters;
using Exceptionless.Json.Linq;
namespace Exceptionless.Serializer {
    internal class DataDictionaryConverter : CustomCreationConverter<DataDictionary> {
        public override DataDictionary Create(Type objectType) {
            return new DataDictionary();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            object obj = base.ReadJson(reader, objectType, existingValue, serializer);
            var result = obj as DataDictionary;
            if (result == null)
                return obj;

            var dictionary = new DataDictionary();
            foreach (string key in result.Keys) {
                object value = result[key];
                if (value is JObject jObject)
                    dictionary[key] = jObject.ToString(serializer.Formatting);
                else if (value is JArray jArray)
                    dictionary[key] = jArray.ToString(serializer.Formatting);
                else
                    dictionary[key] = value;
            }

            return dictionary;
        }
    }
}