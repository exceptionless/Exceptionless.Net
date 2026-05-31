using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ParameterSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"name":null,"type":null,"type_namespace":null,"data":{},"generic_arguments":[]}""";
        /* lang=json */
        private const string CompleteJson = """{"name":"param1","type":"System.String","type_namespace":"System","data":{"ParameterKey":"ParameterValue"},"generic_arguments":["U"]}""";

        [Fact]
        public void Serialize_MinimalParameter_ProducesCorrectJson() {
            // Arrange
            var parameter = new Parameter();

            // Act
            string json = Serialize(parameter);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteParameter_ProducesCorrectJson() {
            // Arrange
            var parameter = CreateCompleteParameter();

            // Act
            string json = Serialize(parameter);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_Parameter_RoundTrips() {
            // Arrange
            var parameter = CreateCompleteParameter();

            // Act
            Parameter roundTripped = RoundTrip(parameter);

            // Assert
            Assert.Equal("param1", roundTripped.Name);
            Assert.Equal("System.String", roundTripped.Type);
            Assert.Equal("System", roundTripped.TypeNamespace);
            Assert.Equal("ParameterValue", roundTripped.Data["ParameterKey"]);
            Assert.Equal("U", roundTripped.GenericArguments[0]);
        }

        [Fact]
        public void Deserialize_Parameter_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            Parameter parameter = Deserialize<Parameter>(json);

            // Assert
            Assert.Equal("param1", parameter.Name);
            Assert.Equal("System.String", parameter.Type);
            Assert.Equal("System", parameter.TypeNamespace);
            Assert.Equal("ParameterValue", parameter.Data["ParameterKey"]);
            Assert.Equal("U", parameter.GenericArguments[0]);
        }

        private static Parameter CreateCompleteParameter() {
            return new Parameter {
                Name = "param1",
                Type = "System.String",
                TypeNamespace = "System",
                Data = {
                    ["ParameterKey"] = "ParameterValue"
                },
                GenericArguments = new GenericArguments { "U" }
            };
        }
    }
}
