using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class StackFrameSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"file_name":null,"line_number":0,"column":0,"is_signature_target":false,"declaring_namespace":null,"declaring_type":null,"name":null,"module_id":0,"data":{},"generic_arguments":[],"parameters":[]}""";
        /* lang=json */
        private const string CompleteJson = """{"file_name":"TestFile.cs","line_number":20,"column":5,"is_signature_target":true,"declaring_namespace":"TestNamespace","declaring_type":"TestClass","name":"InnerMethodName","module_id":1,"data":{"StackFrameKey":"StackFrameValue"},"generic_arguments":["T"],"parameters":[{"name":"param1","type":"System.String","type_namespace":"System","data":{"ParameterKey":"ParameterValue"},"generic_arguments":["U"]}]}""";

        [Fact]
        public void Serialize_MinimalStackFrame_ProducesCorrectJson() {
            // Arrange
            var stackFrame = new StackFrame();

            // Act
            string json = Serialize(stackFrame);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteStackFrame_ProducesCorrectJson() {
            // Arrange
            var stackFrame = CreateCompleteStackFrame();

            // Act
            string json = Serialize(stackFrame);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_StackFrame_RoundTrips() {
            // Arrange
            var stackFrame = CreateCompleteStackFrame();

            // Act
            StackFrame roundTripped = RoundTrip(stackFrame);

            // Assert
            Assert.Equal("TestFile.cs", roundTripped.FileName);
            Assert.Equal(20, roundTripped.LineNumber);
            Assert.Equal(5, roundTripped.Column);
            Assert.True(roundTripped.IsSignatureTarget);
            Assert.Equal("TestNamespace", roundTripped.DeclaringNamespace);
            Assert.Equal("TestClass", roundTripped.DeclaringType);
            Assert.Equal("InnerMethodName", roundTripped.Name);
            Assert.Equal(1, roundTripped.ModuleId);
            Assert.Equal("StackFrameValue", roundTripped.Data["StackFrameKey"]);
            Assert.Equal("T", roundTripped.GenericArguments[0]);
            Assert.Equal("param1", roundTripped.Parameters[0].Name);
            Assert.Equal("System.String", roundTripped.Parameters[0].Type);
            Assert.Equal("ParameterValue", roundTripped.Parameters[0].Data["ParameterKey"]);
            Assert.Equal("U", roundTripped.Parameters[0].GenericArguments[0]);
        }

        [Fact]
        public void Deserialize_StackFrame_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            StackFrame stackFrame = Deserialize<StackFrame>(json);

            // Assert
            Assert.Equal("TestFile.cs", stackFrame.FileName);
            Assert.Equal(20, stackFrame.LineNumber);
            Assert.Equal(5, stackFrame.Column);
            Assert.True(stackFrame.IsSignatureTarget);
            Assert.Equal("TestNamespace", stackFrame.DeclaringNamespace);
            Assert.Equal("TestClass", stackFrame.DeclaringType);
            Assert.Equal("InnerMethodName", stackFrame.Name);
            Assert.Equal(1, stackFrame.ModuleId);
            Assert.Equal("StackFrameValue", stackFrame.Data["StackFrameKey"]);
            Assert.Equal("T", stackFrame.GenericArguments[0]);
            Assert.Equal("param1", stackFrame.Parameters[0].Name);
            Assert.Equal("System.String", stackFrame.Parameters[0].Type);
            Assert.Equal("ParameterValue", stackFrame.Parameters[0].Data["ParameterKey"]);
            Assert.Equal("U", stackFrame.Parameters[0].GenericArguments[0]);
        }

        private static StackFrame CreateCompleteStackFrame() {
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
