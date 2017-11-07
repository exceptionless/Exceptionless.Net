using Exceptionless.Models;
using Exceptionless.Models.Data;
using MessagePack.Formatters;

namespace Exceptionless.MessagePack {
    internal class StackFrameCollectionFormatter : CollectionFormatterBase<StackFrame, StackFrameCollection> {
        protected override StackFrameCollection Create(int count) {
            return new StackFrameCollection();
        }

        protected override void Add(StackFrameCollection collection, int index, StackFrame value) {
            collection[index] = value;
        }
    }
}
