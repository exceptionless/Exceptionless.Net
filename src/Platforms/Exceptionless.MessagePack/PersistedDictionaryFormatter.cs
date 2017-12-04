using Exceptionless.Dependency;
using Exceptionless.Storage;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class PersistedDictionaryFormatter : DictionaryFormatterBase<string, string, PersistedDictionary> {
        private readonly IDependencyResolver _resolver;

        public PersistedDictionaryFormatter(IDependencyResolver resolver) {
            _resolver = resolver;
        }

        protected override PersistedDictionary Create(int count) {
            return new PersistedDictionary("client-data.json", _resolver.Resolve<IObjectStorage>(), _resolver.Resolve<IJsonSerializer>());
        }

        protected override void Add(PersistedDictionary collection, int index, string key, string value) {
            collection.Add(key, value);
        }
    }
}
