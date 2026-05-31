using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class StackFrameCollectionSerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """[]""";
        private const string CompleteJson = /* lang=json */ """[{"file_name":"TestFile.cs","line_number":20,"column":5,"is_signature_target":true,"declaring_namespace":"TestNamespace","declaring_type":"TestClass","name":"InnerMethodName","module_id":1,"data":{"StackFrameKey":"StackFrameValue"},"generic_arguments":["T"],"parameters":[{"name":"param1","type":"System.String","type_namespace":"System","data":{"ParameterKey":"ParameterValue"},"generic_arguments":["U"]}]}]""";

        [Fact]
        public void Serialize_MinimalStackFrameCollection_ProducesCorrectJson() {
            // Arrange
            var stackFrames = new StackFrameCollection();

            // Act
            string json = Serialize(stackFrames);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteStackFrameCollection_ProducesCorrectJson() {
            // Arrange
            var stackFrames = new StackFrameCollection { CreateStackFrame() };

            // Act
            string json = Serialize(stackFrames);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_StackFrameCollection_RoundTrips() {
            // Arrange
            var stackFrames = new StackFrameCollection { CreateStackFrame() };

            // Act
            StackFrameCollection roundTripped = RoundTrip(stackFrames);

            // Assert
            Assert.Single(roundTripped);
            Assert.Equal("TestFile.cs", roundTripped[0].FileName);
            Assert.Equal(20, roundTripped[0].LineNumber);
            Assert.Equal("T", roundTripped[0].GenericArguments[0]);
        }

        [Fact]
        public void Deserialize_StackFrameCollection_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            StackFrameCollection stackFrames = Deserialize<StackFrameCollection>(json);

            // Assert
            Assert.Single(stackFrames);
            Assert.Equal("TestFile.cs", stackFrames[0].FileName);
            Assert.Equal(20, stackFrames[0].LineNumber);
            Assert.Equal("StackFrameValue", stackFrames[0].Data["StackFrameKey"]);
            Assert.Equal("U", stackFrames[0].Parameters[0].GenericArguments[0]);
        }

        private static StackFrame CreateStackFrame() {
            return new StackFrame {
                FileName = "TestFile.cs",
                LineNumber = 20,
                Column = 5,
                IsSignatureTarget = true,
                DeclaringNamespace = "TestNamespace",
                DeclaringType = "TestClass",
                Name = "InnerMethodName",
                ModuleId = 1,
                Data = {
                    ["StackFrameKey"] = "StackFrameValue"
                },
                GenericArguments = new GenericArguments { "T" },
                Parameters = new ParameterCollection {
                    new Parameter {
                        Name = "param1",
                        Type = "System.String",
                        TypeNamespace = "System",
                        Data = {
                            ["ParameterKey"] = "ParameterValue"
                        },
                        GenericArguments = new GenericArguments { "U" }
                    }
                }
            };
        }
    }
}
