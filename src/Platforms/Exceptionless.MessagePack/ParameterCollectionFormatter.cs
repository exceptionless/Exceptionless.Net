using Exceptionless.Models;
using Exceptionless.Models.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class ParameterCollectionFormatter : CollectionFormatterBase<Parameter, ParameterCollection> {
        protected override ParameterCollection Create(int count, MessagePackSerializerOptions options) {
            return new ParameterCollection();
        }

        protected override void Add(ParameterCollection collection, int index, Parameter value, MessagePackSerializerOptions options) {
            collection.Insert(index, value);
        }
    }
}
