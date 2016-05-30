using System;
using System.Reflection;
using Exceptionless.Json;
using Exceptionless.Json.Serialization;
using Exceptionless.Extensions;

namespace Exceptionless.Serializer {
    internal class ExceptionlessContractResolver : DefaultContractResolver {
        private readonly Func<JsonProperty, object, bool> _includeProperty;

        public ExceptionlessContractResolver(Func<JsonProperty, object, bool> includeProperty = null) {
            _includeProperty = includeProperty;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (_includeProperty == null)
                return property;
            
            Predicate<object> shouldSerialize = property.ShouldSerialize;
            property.ShouldSerialize = obj => _includeProperty(property, obj) && (shouldSerialize == null || shouldSerialize(obj));
            return property;
        }

        protected override JsonDictionaryContract CreateDictionaryContract(Type objectType) {
            JsonDictionaryContract contract = base.CreateDictionaryContract(objectType);
            contract.DictionaryKeyResolver = propertyName => propertyName;
            return contract;
        }

        protected override string ResolvePropertyName(string propertyName) {
            return propertyName.ToLowerUnderscoredWords();
        }
    }
}