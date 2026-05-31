using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ParameterCollectionSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_EmptyCollection_ProducesEmptyArray() {
            // Arrange
            var collection = new ParameterCollection();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(collection);

            // Assert
            Assert.Equal("[]", json);
        }

        [Fact]
        public void Serialize_SingleParameter_ProducesArrayWithOneElement() {
            // Arrange
            var collection = new ParameterCollection {
                new Parameter { Name = "param1", Type = "String", TypeNamespace = "System" }
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(collection);

            // Assert
            Assert.Contains("\"name\":\"param1\"", json);
            Assert.Contains("\"type\":\"String\"", json);
            Assert.Contains("\"type_namespace\":\"System\"", json);
        }

        [Fact]
        public void Deserialize_RoundTrip_MultipleParameters_PreservesAll() {
            // Arrange
            var serializer = GetSerializer();
            var original = new ParameterCollection {
                new Parameter {
                    Name = "input",
                    Type = "String",
                    TypeNamespace = "System",
                    GenericArguments = new GenericArguments()
                },
                new Parameter {
                    Name = "options",
                    Type = "Dictionary",
                    TypeNamespace = "System.Collections.Generic",
                    GenericArguments = new GenericArguments { "String", "Object" }
                },
                new Parameter {
                    Name = "token",
                    Type = "CancellationToken",
                    TypeNamespace = "System.Threading"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (ParameterCollection)serializer.Deserialize(json, typeof(ParameterCollection));

            // Assert
            Assert.Equal(3, deserialized.Count);
            Assert.Equal("input", deserialized[0].Name);
            Assert.Equal("String", deserialized[0].Type);
            Assert.Equal("options", deserialized[1].Name);
            Assert.Equal("Dictionary", deserialized[1].Type);
            Assert.Equal(2, deserialized[1].GenericArguments.Count);
            Assert.Equal("token", deserialized[2].Name);
            Assert.Equal("CancellationToken", deserialized[2].Type);
        }

        [Fact]
        public void Deserialize_WithParameterData_PreservesDataDictionary() {
            // Arrange
            var serializer = GetSerializer();
            var original = new ParameterCollection {
                new Parameter {
                    Name = "annotated",
                    Type = "Int32",
                    TypeNamespace = "System",
                    Data = {
                        ["DefaultValue"] = "0",
                        ["IsOptional"] = true
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (ParameterCollection)serializer.Deserialize(json, typeof(ParameterCollection));

            // Assert
            Assert.Single(deserialized);
            Assert.NotNull(deserialized[0].Data);
            Assert.Equal(2, deserialized[0].Data.Count);
        }
    }
}
