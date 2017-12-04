using System;
using System.Collections.Generic;
using System.Text;
using Exceptionless.Dependency;
using Exceptionless.Models;
using Exceptionless.Storage;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    public class ExceptionlessFormatterResolver : IFormatterResolver {
        private readonly Dictionary<Type, object> _formatterMap;

        public ExceptionlessFormatterResolver(IDependencyResolver resolver) {
            _formatterMap = new Dictionary<Type, object>{
                {typeof(DataDictionary), new DataDictionaryFormatter()},
                {typeof(SettingsDictionary), new SettingsDictionaryFormatter()},
                {typeof(PersistedDictionary), new PersistedDictionaryFormatter(resolver)},
                {typeof(TagSet), new TagSetFormatter()},
                {typeof(GenericArguments), new GenericArgumentsFormatter()},
                {typeof(ModuleCollection), new ModuleCollectionFormatter()},
                {typeof(ParameterCollection), new ParameterCollectionFormatter()},
                {typeof(StackFrameCollection), new StackFrameCollectionFormatter()},
            };
        }

        public IMessagePackFormatter<T> GetFormatter<T>() {
            if (_formatterMap.TryGetValue(typeof(T), out var formatter)) {
                return (IMessagePackFormatter<T>)formatter;
            }
            return global::MessagePack.Resolvers.ContractlessStandardResolver.Instance.GetFormatter<T>();
        }
    }
}
