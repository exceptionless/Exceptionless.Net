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

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeEnvironmentInfo() {
            base.CanSerializeEnvironmentInfo();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeError() {
            base.CanSerializeError();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeManualStackingInfo() {
            base.CanSerializeManualStackingInfo();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeRequestInfo() {
            base.CanSerializeRequestInfo();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeSimpleDataValues() {
            base.CanSerializeSimpleDataValues();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeSimpleError() {
            base.CanSerializeSimpleError();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeSimpleEvent() {
            base.CanSerializeSimpleEvent();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeTags() {
            base.CanSerializeTags();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeUserDescription() {
            base.CanSerializeUserDescription();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeUserInfo() {
            base.CanSerializeUserInfo();
        }
    }
}
