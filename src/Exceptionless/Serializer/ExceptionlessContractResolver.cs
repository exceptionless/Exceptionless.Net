using System;
using System.Reflection;
using Exceptionless.Json;
using Exceptionless.Json.Serialization;

namespace Exceptionless.Serializer {
    internal class ExceptionlessContractResolver : DefaultContractResolver {
        private readonly Func<JsonProperty, object, bool> _includeProperty;

        public ExceptionlessContractResolver(Func<JsonProperty, object, bool> includeProperty = null) {
            _includeProperty = includeProperty;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            var property = base.CreateProperty(member, memberSerialization);
            if (_includeProperty == null)
                return property;
            
            var shouldSerialize = property.ShouldSerialize;
            property.ShouldSerialize = obj => _includeProperty(property, obj) && (shouldSerialize == null || shouldSerialize(obj));
            return property;
        }
    }
}