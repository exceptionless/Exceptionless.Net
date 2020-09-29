using Exceptionless.Models;
using Exceptionless.Models.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class StackFrameCollectionFormatter : CollectionFormatterBase<StackFrame, StackFrameCollection> {
        protected override StackFrameCollection Create(int count, MessagePackSerializerOptions options) {
            return new StackFrameCollection();
        }

        protected override void Add(StackFrameCollection collection, int index, StackFrame value, MessagePackSerializerOptions options) {
            collection[index] = value;
        }
    }
}
