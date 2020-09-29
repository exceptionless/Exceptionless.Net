using Exceptionless.Models;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class GenericArgumentsFormatter : CollectionFormatterBase<string, GenericArguments> {
        protected override GenericArguments Create(int count, MessagePackSerializerOptions options) {
            return new GenericArguments();
        }

        protected override void Add(GenericArguments collection, int index, string value, MessagePackSerializerOptions options) {
            collection.Insert(index, value);
        }
    }
}
