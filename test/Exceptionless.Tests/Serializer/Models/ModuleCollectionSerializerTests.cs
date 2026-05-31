using System;
using Exceptionless.Models;
using Exceptionless.Tests.Serializer;
using Xunit;
using Module = Exceptionless.Models.Data.Module;

namespace Exceptionless.Tests.Serializer.Models {
    public class ModuleCollectionSerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """[]""";
        private const string CompleteJson = /* lang=json */ """[{"module_id":1,"name":"TestModule","version":"1.0.0","is_entry":true,"created_date":"2023-05-01T12:00:00Z","modified_date":"2023-05-02T12:00:00Z","data":{"PublicKeyToken":"b03f5f7f11d50a3a"}}]""";

        [Fact]
        public void Serialize_MinimalModuleCollection_ProducesCorrectJson() {
            // Arrange
            var modules = new ModuleCollection();

            // Act
            string json = Serialize(modules);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteModuleCollection_ProducesCorrectJson() {
            // Arrange
            var modules = new ModuleCollection { CreateModule() };

            // Act
            string json = Serialize(modules);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_ModuleCollection_RoundTrips() {
            // Arrange
            var modules = new ModuleCollection { CreateModule() };

            // Act
            ModuleCollection roundTripped = RoundTrip(modules);

            // Assert
            Assert.Single(roundTripped);
            Assert.Equal(1, roundTripped[0].ModuleId);
            Assert.Equal("TestModule", roundTripped[0].Name);
            Assert.Equal(new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc), roundTripped[0].CreatedDate);
        }

        [Fact]
        public void Deserialize_ModuleCollection_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            ModuleCollection modules = Deserialize<ModuleCollection>(json);

            // Assert
            Assert.Single(modules);
            Assert.Equal(1, modules[0].ModuleId);
            Assert.Equal("TestModule", modules[0].Name);
            Assert.Equal("b03f5f7f11d50a3a", modules[0].Data["PublicKeyToken"]);
        }

        private static Module CreateModule() {
            return new Module {
                ModuleId = 1,
                Name = "TestModule",
                Version = "1.0.0",
                IsEntry = true,
                CreatedDate = new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2023, 5, 2, 12, 0, 0, DateTimeKind.Utc),
                Data = {
                    ["PublicKeyToken"] = "b03f5f7f11d50a3a"
                }
            };
        }
    }
}
