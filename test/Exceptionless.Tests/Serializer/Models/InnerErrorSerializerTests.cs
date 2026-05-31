using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class InnerErrorSerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """{"message":null,"type":null,"code":null,"data":{},"inner":null,"stack_trace":[],"target_method":null}""";
        private const string CompleteJson = /* lang=json */ """{"message":"Inner error message","type":"System.ArgumentException","code":"2002","data":{"Detail":"Value"},"inner":{"message":"Deep inner error","type":"System.Exception","code":"3003","data":{},"inner":null,"stack_trace":[],"target_method":null},"stack_trace":[{"file_name":null,"line_number":20,"column":0,"is_signature_target":false,"declaring_namespace":null,"declaring_type":null,"name":"InnerMethodName","module_id":0,"data":{},"generic_arguments":[],"parameters":[]}],"target_method":null}""";

        [Fact]
        public void Serialize_MinimalInnerError_ProducesCorrectJson() {
            // Arrange
            var error = new InnerError();

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteInnerError_ProducesCorrectJson() {
            // Arrange
            var error = CreateCompleteInnerError();

            // Act
            string json = Serialize(error);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_InnerError_RoundTrips() {
            // Arrange
            var error = CreateCompleteInnerError();

            // Act
            InnerError roundTripped = RoundTrip(error);

            // Assert
            Assert.Equal("Inner error message", roundTripped.Message);
            Assert.Equal("System.ArgumentException", roundTripped.Type);
            Assert.Equal("2002", roundTripped.Code);
            Assert.Equal("Value", roundTripped.Data["Detail"]);
            Assert.Equal("Deep inner error", roundTripped.Inner.Message);
            Assert.Equal("3003", roundTripped.Inner.Code);
            Assert.Equal(20, roundTripped.StackTrace[0].LineNumber);
            Assert.Equal("InnerMethodName", roundTripped.StackTrace[0].Name);
            Assert.Null(roundTripped.TargetMethod);
        }

        [Fact]
        public void Deserialize_InnerError_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            InnerError error = Deserialize<InnerError>(json);

            // Assert
            Assert.Equal("Inner error message", error.Message);
            Assert.Equal("System.ArgumentException", error.Type);
            Assert.Equal("2002", error.Code);
            Assert.Equal("Value", error.Data["Detail"]);
            Assert.Equal("Deep inner error", error.Inner.Message);
            Assert.Equal("System.Exception", error.Inner.Type);
            Assert.Equal(20, error.StackTrace[0].LineNumber);
            Assert.Equal("InnerMethodName", error.StackTrace[0].Name);
            Assert.Null(error.TargetMethod);
        }

        private static InnerError CreateCompleteInnerError() {
            return new InnerError {
                Message = "Inner error message",
                Type = "System.ArgumentException",
                Code = "2002",
                Data = {
                    ["Detail"] = "Value"
                },
                Inner = new InnerError {
                    Message = "Deep inner error",
                    Type = "System.Exception",
                    Code = "3003"
                },
                StackTrace = new StackFrameCollection {
                    new StackFrame {
                        LineNumber = 20,
                        Name = "InnerMethodName"
                    }
                }
            };
        }
    }
}
