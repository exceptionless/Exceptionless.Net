using System;
using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;
using Module = Exceptionless.Models.Data.Module;

namespace Exceptionless.Tests.Serializer.Models {
    public class ModuleSerializerTests : SerializerTestBase {
        private const string MinimalJson = /* lang=json */ """{"module_id":0,"name":null,"version":null,"is_entry":false,"created_date":"0001-01-01T00:00:00","modified_date":"0001-01-01T00:00:00","data":{}}""";
        private const string CompleteJson = /* lang=json */ """{"module_id":1,"name":"TestModule","version":"1.0.0","is_entry":true,"created_date":"2023-05-01T12:00:00Z","modified_date":"2023-05-02T12:00:00Z","data":{"PublicKeyToken":"b03f5f7f11d50a3a"}}""";

        [Fact]
        public void Serialize_MinimalModule_ProducesCorrectJson() {
            // Arrange
            var module = new Module();

            // Act
            string json = Serialize(module);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteModule_ProducesCorrectJson() {
            // Arrange
            var module = CreateCompleteModule();

            // Act
            string json = Serialize(module);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_Module_RoundTrips() {
            // Arrange
            var module = CreateCompleteModule();

            // Act
            Module roundTripped = RoundTrip(module);

            // Assert
            Assert.Equal(1, roundTripped.ModuleId);
            Assert.Equal("TestModule", roundTripped.Name);
            Assert.Equal("1.0.0", roundTripped.Version);
            Assert.True(roundTripped.IsEntry);
            Assert.Equal(new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc), roundTripped.CreatedDate);
            Assert.Equal(new DateTime(2023, 5, 2, 12, 0, 0, DateTimeKind.Utc), roundTripped.ModifiedDate);
            Assert.Equal("b03f5f7f11d50a3a", roundTripped.Data["PublicKeyToken"]);
        }

        [Fact]
        public void Deserialize_Module_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            Module module = Deserialize<Module>(json);

            // Assert
            Assert.Equal(1, module.ModuleId);
            Assert.Equal("TestModule", module.Name);
            Assert.Equal("1.0.0", module.Version);
            Assert.True(module.IsEntry);
            Assert.Equal(new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc), module.CreatedDate);
            Assert.Equal(new DateTime(2023, 5, 2, 12, 0, 0, DateTimeKind.Utc), module.ModifiedDate);
            Assert.Equal("b03f5f7f11d50a3a", module.Data["PublicKeyToken"]);
        }

        private static Module CreateCompleteModule() {
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
