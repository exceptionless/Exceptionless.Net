using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class MethodSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"is_signature_target":false,"declaring_namespace":null,"declaring_type":null,"name":null,"module_id":0,"data":{},"generic_arguments":[],"parameters":[]}""";
        /* lang=json */
        private const string CompleteJson = """{"is_signature_target":true,"declaring_namespace":"TestNamespace","declaring_type":"TestClass","name":"TestMethod","module_id":1,"data":{"MethodKey":"MethodValue"},"generic_arguments":["T"],"parameters":[{"name":"param1","type":"System.String","type_namespace":"System","data":{"ParameterKey":"ParameterValue"},"generic_arguments":["U"]}]}""";

        [Fact]
        public void Serialize_MinimalMethod_ProducesCorrectJson() {
            // Arrange
            var method = new Method();

            // Act
            string json = Serialize(method);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteMethod_ProducesCorrectJson() {
            // Arrange
            var method = CreateCompleteMethod();

            // Act
            string json = Serialize(method);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_Method_RoundTrips() {
            // Arrange
            var method = CreateCompleteMethod();

            // Act
            Method roundTripped = RoundTrip(method);

            // Assert
            Assert.True(roundTripped.IsSignatureTarget);
            Assert.Equal("TestNamespace", roundTripped.DeclaringNamespace);
            Assert.Equal("TestClass", roundTripped.DeclaringType);
            Assert.Equal("TestMethod", roundTripped.Name);
            Assert.Equal(1, roundTripped.ModuleId);
            Assert.Equal("MethodValue", roundTripped.Data["MethodKey"]);
            Assert.Equal("T", roundTripped.GenericArguments[0]);
            Assert.Equal("param1", roundTripped.Parameters[0].Name);
            Assert.Equal("System.String", roundTripped.Parameters[0].Type);
            Assert.Equal("System", roundTripped.Parameters[0].TypeNamespace);
            Assert.Equal("ParameterValue", roundTripped.Parameters[0].Data["ParameterKey"]);
            Assert.Equal("U", roundTripped.Parameters[0].GenericArguments[0]);
        }

        [Fact]
        public void Deserialize_Method_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            Method method = Deserialize<Method>(json);

            // Assert
            Assert.True(method.IsSignatureTarget);
            Assert.Equal("TestNamespace", method.DeclaringNamespace);
            Assert.Equal("TestClass", method.DeclaringType);
            Assert.Equal("TestMethod", method.Name);
            Assert.Equal(1, method.ModuleId);
            Assert.Equal("MethodValue", method.Data["MethodKey"]);
            Assert.Equal("T", method.GenericArguments[0]);
            Assert.Equal("param1", method.Parameters[0].Name);
            Assert.Equal("System.String", method.Parameters[0].Type);
            Assert.Equal("System", method.Parameters[0].TypeNamespace);
            Assert.Equal("ParameterValue", method.Parameters[0].Data["ParameterKey"]);
            Assert.Equal("U", method.Parameters[0].GenericArguments[0]);
        }

        private static Method CreateCompleteMethod() {
            return new Method {
                IsSignatureTarget = true,
                DeclaringNamespace = "TestNamespace",
                DeclaringType = "TestClass",
                Name = "TestMethod",
                ModuleId = 1,
                Data = {
                    ["MethodKey"] = "MethodValue"
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
