using System;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ModuleCollectionSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_EmptyCollection_ProducesEmptyArray() {
            // Arrange
            var collection = new ModuleCollection();
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(collection);

            // Assert
            Assert.Equal("[]", json);
        }

        [Fact]
        public void Serialize_SingleModule_ProducesArrayWithOneElement() {
            // Arrange
            var collection = new ModuleCollection {
                new Module { ModuleId = 1, Name = "App.dll", Version = "1.0.0" }
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(collection);

            // Assert
            Assert.Contains("\"module_id\":1", json);
            Assert.Contains("\"name\":\"App.dll\"", json);
            Assert.Contains("\"version\":\"1.0.0\"", json);
        }

        [Fact]
        public void Deserialize_RoundTrip_MultipleModules_PreservesAll() {
            // Arrange
            var serializer = GetSerializer();
            var original = new ModuleCollection {
                new Module {
                    ModuleId = 1,
                    Name = "MyApp.dll",
                    Version = "2.0.0",
                    IsEntry = true,
                    CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Module {
                    ModuleId = 2,
                    Name = "System.Runtime.dll",
                    Version = "6.0.0",
                    IsEntry = false
                },
                new Module {
                    ModuleId = 3,
                    Name = "Newtonsoft.Json.dll",
                    Version = "13.0.3",
                    IsEntry = false,
                    Data = { ["PublicKeyToken"] = "30ad4fe6b2a6aeed" }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (ModuleCollection)serializer.Deserialize(json, typeof(ModuleCollection));

            // Assert
            Assert.Equal(3, deserialized.Count);
            Assert.Equal("MyApp.dll", deserialized[0].Name);
            Assert.True(deserialized[0].IsEntry);
            Assert.Equal("System.Runtime.dll", deserialized[1].Name);
            Assert.False(deserialized[1].IsEntry);
            Assert.Equal("Newtonsoft.Json.dll", deserialized[2].Name);
            Assert.Equal("13.0.3", deserialized[2].Version);
        }

        [Fact]
        public void Deserialize_WithModuleData_PreservesDataDictionary() {
            // Arrange
            var serializer = GetSerializer();
            var original = new ModuleCollection {
                new Module {
                    ModuleId = 1,
                    Name = "Test.dll",
                    Version = "1.0.0",
                    Data = {
                        ["PublicKeyToken"] = "b77a5c561934e089",
                        ["Culture"] = "neutral"
                    }
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (ModuleCollection)serializer.Deserialize(json, typeof(ModuleCollection));

            // Assert
            Assert.Single(deserialized);
            Assert.NotNull(deserialized[0].Data);
            Assert.Equal(2, deserialized[0].Data.Count);
        }
    }
}
