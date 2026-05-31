using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ParameterCollectionSerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """[]""";
        private const string CompleteJson = /* lang=json */ """[{"name":"param1","type":"System.String","type_namespace":"System","data":{"ParameterKey":"ParameterValue"},"generic_arguments":["U"]}]""";

        [Fact]
        public void Serialize_MinimalParameterCollection_ProducesCorrectJson() {
            // Arrange
            var parameters = new ParameterCollection();

            // Act
            string json = Serialize(parameters);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteParameterCollection_ProducesCorrectJson() {
            // Arrange
            var parameters = new ParameterCollection { CreateParameter() };

            // Act
            string json = Serialize(parameters);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_ParameterCollection_RoundTrips() {
            // Arrange
            var parameters = new ParameterCollection { CreateParameter() };

            // Act
            ParameterCollection roundTripped = RoundTrip(parameters);

            // Assert
            Assert.Single(roundTripped);
            Assert.Equal("param1", roundTripped[0].Name);
            Assert.Equal("System.String", roundTripped[0].Type);
            Assert.Equal("U", roundTripped[0].GenericArguments[0]);
        }

        [Fact]
        public void Deserialize_ParameterCollection_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            ParameterCollection parameters = Deserialize<ParameterCollection>(json);

            // Assert
            Assert.Single(parameters);
            Assert.Equal("param1", parameters[0].Name);
            Assert.Equal("System.String", parameters[0].Type);
            Assert.Equal("ParameterValue", parameters[0].Data["ParameterKey"]);
            Assert.Equal("U", parameters[0].GenericArguments[0]);
        }

        private static Parameter CreateParameter() {
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
