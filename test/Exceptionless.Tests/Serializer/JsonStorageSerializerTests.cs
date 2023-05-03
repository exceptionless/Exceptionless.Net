using Exceptionless.Dependency;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer {
    public class JsonStorageSerializerTests : StorageSerializerTestBase {
        public JsonStorageSerializerTests() {
            Resolver.Register<IStorageSerializer, DefaultJsonSerializer>();
        }

        [Fact(Skip = "The JSON DataDictionaryConverter converts objects into json strings")]
        public override void CanSerializeEnvironmentInfo() {
            base.CanSerializeEnvironmentInfo();
        }

        [Fact(Skip = "The JSON DataDictionaryConverter converts objects into json strings")]
        public override void CanSerializeRequestInfo() {
            base.CanSerializeRequestInfo();
        }

        [Fact(Skip = "The JSON DataDictionaryConverter converts objects into json strings")]
        public override void CanSerializeTraceLogEntries() {
            base.CanSerializeTraceLogEntries();
        }

        [Fact(Skip = "The JSON DataDictionaryConverter converts objects into json strings")]
        public override void CanSerializeUserInfo() {
            base.CanSerializeUserInfo();
        }

        [Fact(Skip = "The JSON DataDictionaryConverter converts objects into json strings")]
        public override void CanSerializeUserDescription() {
            base.CanSerializeUserDescription();
        }

        [Fact(Skip = "The JSON DataDictionaryConverter converts objects into json strings")]
        public override void CanSerializeManualStackingInfo() {
            base.CanSerializeManualStackingInfo();
        }

        [Fact(Skip = "The JSON DataDictionaryConverter converts objects into json strings")]
        public override void CanSerializeSimpleError() {
            base.CanSerializeSimpleError();
        }

        [Fact(Skip = "The JSON DataDictionaryConverter converts objects into json strings")]
        public override void CanSerializeError() {
            base.CanSerializeError();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeSimpleDataValues() {
            base.CanSerializeSimpleDataValues();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeSimpleEvent() {
            base.CanSerializeSimpleEvent();
        }

        [Fact(Skip = "The equality comparer algorithm does not support.")]
        public override void CanSerializeTags() {
            base.CanSerializeTags();
        }
    }
}
