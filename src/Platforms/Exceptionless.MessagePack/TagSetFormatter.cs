using Exceptionless.Models;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class TagSetFormatter : CollectionFormatterBase<string, TagSet> {
        protected override TagSet Create(int count) {
            return new TagSet();
        }

        protected override void Add(TagSet collection, int index, string value) {
            collection.Add(value);
        }
    }
}
