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

        [Fact]
        public override void CanSerializeTraceLogEntries() {
            base.CanSerializeTraceLogEntries();
        }

        [Fact(Skip = "This test is flakey cross platform")]
        public override void CanSerializeEnvironmentInfo() {
            base.CanSerializeEnvironmentInfo();
        }

        [Fact]
        public override void CanSerializeError() {
            base.CanSerializeError();
        }

        [Fact]
        public override void CanSerializeManualStackingInfo() {
            base.CanSerializeManualStackingInfo();
        }

        [Fact]
        public override void CanSerializeRequestInfo() {
            base.CanSerializeRequestInfo();
        }

        [Fact]
        public override void CanSerializeSimpleDataValues() {
            base.CanSerializeSimpleDataValues();
        }

        [Fact]
        public override void CanSerializeSimpleError() {
            base.CanSerializeSimpleError();
        }

        [Fact]
        public override void CanSerializeSimpleEvent() {
            base.CanSerializeSimpleEvent();
        }

        [Fact]
        public override void CanSerializeTags() {
            base.CanSerializeTags();
        }

        [Fact]
        public override void CanSerializeUserDescription() {
            base.CanSerializeUserDescription();
        }

        [Fact]
        public override void CanSerializeUserInfo() {
            base.CanSerializeUserInfo();
        }
    }
}
