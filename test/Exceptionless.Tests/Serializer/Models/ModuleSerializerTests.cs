using System;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class ModuleSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteModule_ProducesSnakeCaseJson() {
            // Arrange
            var module = new Module {
                ModuleId = 1,
                Name = "MyApp.Core.dll",
                Version = "2.1.0",
                IsEntry = true,
                CreatedDate = new DateTime(2023, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2023, 6, 20, 14, 30, 0, DateTimeKind.Utc),
                Data = { ["PublicKeyToken"] = "b03f5f7f11d50a3a" }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(module);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "module_id", "name", "version", "is_entry", "created_date", "modified_date", "data");
            SerializerContractAssertions.ExcludesProperties(json,
                "ModuleId", "Name", "Version", "IsEntry", "CreatedDate", "ModifiedDate");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Module {
                ModuleId = 42,
                Name = "System.Runtime.dll",
                Version = "6.0.0",
                IsEntry = false,
                CreatedDate = new DateTime(2022, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2023, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                Data = {
                    ["PublicKeyToken"] = "b77a5c561934e089",
                    ["Culture"] = "neutral"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Module)serializer.Deserialize(json, typeof(Module));

            // Assert
            Assert.Equal(42, deserialized.ModuleId);
            Assert.Equal("System.Runtime.dll", deserialized.Name);
            Assert.Equal("6.0.0", deserialized.Version);
            Assert.False(deserialized.IsEntry);
            Assert.Equal(original.CreatedDate, deserialized.CreatedDate);
            Assert.Equal(original.ModifiedDate, deserialized.ModifiedDate);
            Assert.NotNull(deserialized.Data);
            Assert.Equal(2, deserialized.Data.Count);
        }

        [Fact]
        public void Deserialize_MinimalModule_PreservesProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Module {
                ModuleId = 1,
                Name = "Simple.dll",
                Version = "1.0.0"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Module)serializer.Deserialize(json, typeof(Module));

            // Assert
            Assert.Equal(1, deserialized.ModuleId);
            Assert.Equal("Simple.dll", deserialized.Name);
            Assert.Equal("1.0.0", deserialized.Version);
            Assert.False(deserialized.IsEntry);
        }

        [Fact]
        public void Deserialize_WithDataDictionary_PreservesCustomData() {
            // Arrange
            var serializer = GetSerializer();
            var original = new Module {
                ModuleId = 5,
                Name = "Plugin.dll",
                Version = "3.2.1",
                Data = {
                    ["Hash"] = "abc123",
                    ["Size"] = 1024
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (Module)serializer.Deserialize(json, typeof(Module));

            // Assert
            Assert.NotNull(deserialized.Data);
            Assert.Equal(2, deserialized.Data.Count);
        }

        [Fact]
        public void Serialize_IsEntryTrue_SerializesAsTrue() {
            // Arrange
            var module = new Module { ModuleId = 1, Name = "Entry.dll", Version = "1.0.0", IsEntry = true };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(module);

            // Assert
            Assert.Contains("\"is_entry\":true", json);
        }

        [Fact]
        public void Serialize_IsEntryFalse_SerializesAsFalse() {
            // Arrange
            var module = new Module { ModuleId = 2, Name = "Lib.dll", Version = "1.0.0", IsEntry = false };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(module);

            // Assert
            Assert.Contains("\"is_entry\":false", json);
        }

        [Fact]
        public void Serialize_DateTimesAsUtc_SerializesInIsoFormat() {
            // Arrange
            var module = new Module {
                ModuleId = 1,
                Name = "Test.dll",
                Version = "1.0.0",
                CreatedDate = new DateTime(2023, 5, 1, 12, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2023, 5, 2, 12, 0, 0, DateTimeKind.Utc)
            };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(module);

            // Assert
            Assert.Contains("\"created_date\":\"2023-05-01T12:00:00Z\"", json);
            Assert.Contains("\"modified_date\":\"2023-05-02T12:00:00Z\"", json);
        }
    }
}
