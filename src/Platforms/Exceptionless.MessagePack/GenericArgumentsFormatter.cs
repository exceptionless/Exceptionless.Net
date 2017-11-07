using Exceptionless.Models;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class GenericArgumentsFormatter : CollectionFormatterBase<string, GenericArguments> {
        protected override GenericArguments Create(int count) {
            return new GenericArguments();
        }

        protected override void Add(GenericArguments collection, int index, string value) {
            collection.Insert(index, value);
        }
    }
}
