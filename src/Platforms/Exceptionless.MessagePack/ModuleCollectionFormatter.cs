using Exceptionless.Models;
using Exceptionless.Models.Data;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class ModuleCollectionFormatter : CollectionFormatterBase<Module, ModuleCollection> {
        protected override ModuleCollection Create(int count) {
            return new ModuleCollection();
        }

        protected override void Add(ModuleCollection collection, int index, Module value) {
            collection.Insert(index, value);
        }
    }
}
