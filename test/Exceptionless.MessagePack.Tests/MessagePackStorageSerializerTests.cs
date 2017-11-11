using Exceptionless.Dependency;
using Exceptionless.Serializer;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.MessagePack.Tests {
    public class MessagePackStorageSerializerTests : StorageSerializerTestBase {
        protected override void Initialize(IDependencyResolver resolver) {
            base.Initialize(resolver);
            resolver.Register<IStorageSerializer>(new MessagePackStorageSerializer(resolver));
        }

        protected override IStorageSerializer GetSerializer(IDependencyResolver resolver) {
            return resolver.Resolve<IStorageSerializer>();
        }

        [Fact(Skip = "The equality comparer algorithm does not support List values for data dictionary.")]
        public override void CanSerializeTraceLogEntries() {
            base.CanSerializeTraceLogEntries();
        }
    }
}
