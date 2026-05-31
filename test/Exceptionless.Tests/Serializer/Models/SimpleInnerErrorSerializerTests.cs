using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class SimpleInnerErrorSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"message":null,"type":null,"stack_trace":null,"data":{},"inner":null}""";
        /* lang=json */
        private const string CompleteJson = """{"message":"Inner error message","type":"System.NullReferenceException","stack_trace":"at InnerTestClass.InnerTestMethod()","data":{"SimpleKey":"SimpleValue"},"inner":{"message":"Deep inner error","type":"System.Exception","stack_trace":"at DeepInner()","data":{},"inner":null}}""";

        [Fact]
        public void Serialize_MinimalSimpleInnerError_ProducesCorrectJson() {
            // Arrange
            var error = new SimpleInnerError();

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteSimpleInnerError_ProducesCorrectJson() {
            // Arrange
            var error = CreateCompleteSimpleInnerError();

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_SimpleInnerError_RoundTrips() {
            // Arrange
            var error = CreateCompleteSimpleInnerError();

            // Act
            SimpleInnerError roundTripped = RoundTrip(error);

            // Assert
            Assert.Equal("Inner error message", roundTripped.Message);
            Assert.Equal("System.NullReferenceException", roundTripped.Type);
            Assert.Equal("at InnerTestClass.InnerTestMethod()", roundTripped.StackTrace);
            Assert.Equal("SimpleValue", roundTripped.Data["SimpleKey"]);
            Assert.Equal("Deep inner error", roundTripped.Inner.Message);
            Assert.Equal("at DeepInner()", roundTripped.Inner.StackTrace);
        }

        [Fact]
        public void Deserialize_SimpleInnerError_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            SimpleInnerError error = Deserialize<SimpleInnerError>(json);

            // Assert
            Assert.Equal("Inner error message", error.Message);
            Assert.Equal("System.NullReferenceException", error.Type);
            Assert.Equal("at InnerTestClass.InnerTestMethod()", error.StackTrace);
            Assert.Equal("SimpleValue", error.Data["SimpleKey"]);
            Assert.Equal("Deep inner error", error.Inner.Message);
            Assert.Equal("System.Exception", error.Inner.Type);
        }

        private static SimpleInnerError CreateCompleteSimpleInnerError() {
            return new SimpleInnerError {
                Message = "Inner error message",
                Type = "System.NullReferenceException",
                StackTrace = "at InnerTestClass.InnerTestMethod()",
                Data = {
                    ["SimpleKey"] = "SimpleValue"
                },
                Inner = new SimpleInnerError {
                    Message = "Deep inner error",
                    Type = "System.Exception",
                    StackTrace = "at DeepInner()"
                }
            };
        }
    }
}
