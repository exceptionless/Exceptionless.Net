using Exceptionless.Models;
using Exceptionless.Models.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class ModuleCollectionFormatter : CollectionFormatterBase<Module, ModuleCollection> {
        protected override ModuleCollection Create(int count, MessagePackSerializerOptions options) {
            return new ModuleCollection();
        }

        protected override void Add(ModuleCollection collection, int index, Module value, MessagePackSerializerOptions options) {
            collection.Insert(index, value);
        }
    }
}
