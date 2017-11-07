using Exceptionless.Models;
using Exceptionless.Models.Data;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class ParameterCollectionFormatter : CollectionFormatterBase<Parameter, ParameterCollection> {
        protected override ParameterCollection Create(int count) {
            return new ParameterCollection();
        }

        protected override void Add(ParameterCollection collection, int index, Parameter value) {
            collection.Insert(index, value);
        }
    }
}
