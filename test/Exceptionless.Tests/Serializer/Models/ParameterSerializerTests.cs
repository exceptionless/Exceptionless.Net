using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ParameterSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteParameter_ProducesSnakeCaseJson() {
            // Arrange
            var param = new Parameter {
                Name = "userId",
                Type = "Int32",
                TypeNamespace = "System",
                Data = { ["DefaultValue"] = "0" },
                GenericArguments = new GenericArguments { "T" }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(param);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "name", "type", "type_namespace", "data", "generic_arguments");
            SerializerContractAssertions.ExcludesProperties(json,
                "Name", "Type", "TypeNamespace", "GenericArguments");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Parameter {
                Name = "request",
                Type = "HttpRequestMessage",
                TypeNamespace = "System.Net.Http",
                Data = { ["IsOptional"] = false },
                GenericArguments = new GenericArguments { "TBody" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Parameter)serializer.Deserialize(json, typeof(Parameter));

            // Assert
            Assert.Equal("request", deserialized.Name);
            Assert.Equal("HttpRequestMessage", deserialized.Type);
            Assert.Equal("System.Net.Http", deserialized.TypeNamespace);
            Assert.NotNull(deserialized.GenericArguments);
            Assert.Single(deserialized.GenericArguments);
            Assert.Equal("TBody", deserialized.GenericArguments[0]);
        }

        [Fact]
        public void Deserialize_MinimalParameter_PreservesBasicProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Parameter {
                Name = "value",
                Type = "String"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Parameter)serializer.Deserialize(json, typeof(Parameter));

            // Assert
            Assert.Equal("value", deserialized.Name);
            Assert.Equal("String", deserialized.Type);
            Assert.Null(deserialized.TypeNamespace);
        }

        [Fact]
        public void Deserialize_WithMultipleGenericArguments_PreservesOrder() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Parameter {
                Name = "converter",
                Type = "Func",
                TypeNamespace = "System",
                GenericArguments = new GenericArguments { "TInput", "TOutput", "TContext" }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Parameter)serializer.Deserialize(json, typeof(Parameter));

            // Assert
            Assert.Equal(3, deserialized.GenericArguments.Count);
            Assert.Equal("TInput", deserialized.GenericArguments[0]);
            Assert.Equal("TOutput", deserialized.GenericArguments[1]);
            Assert.Equal("TContext", deserialized.GenericArguments[2]);
        }

        [Fact]
        public void Deserialize_WithDataDictionary_PreservesData() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Parameter {
                Name = "options",
                Type = "Options",
                Data = {
                    ["CustomAttribute"] = "attr_value",
                    ["IsNullable"] = true
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Parameter)serializer.Deserialize(json, typeof(Parameter));

            // Assert
            Assert.NotNull(deserialized.Data);
            Assert.Equal(2, deserialized.Data.Count);
        }

        [Fact]
        public void Deserialize_EmptyGenericArguments_PreservesEmptyCollection() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Parameter {
                Name = "simple",
                Type = "Int32",
                GenericArguments = new GenericArguments()
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Parameter)serializer.Deserialize(json, typeof(Parameter));

            // Assert
            Assert.NotNull(deserialized.GenericArguments);
            Assert.Empty(deserialized.GenericArguments);
        }
    }
}
