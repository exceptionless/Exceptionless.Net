using Exceptionless.Models;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class TagSetFormatter : CollectionFormatterBase<string, TagSet> {
        protected override TagSet Create(int count, MessagePackSerializerOptions options) {
            return new TagSet();
        }

        protected override void Add(TagSet collection, int index, string value, MessagePackSerializerOptions options) {
            collection.Add(value);
        }
    }
}
