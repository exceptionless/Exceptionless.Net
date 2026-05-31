using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class EnvironmentInfoSerializerTests {
        protected virtual IJsonSerializer GetSerializer() {
            return new DefaultJsonSerializer();
        }

        [Fact]
        public void Serialize_CompleteEnvironmentInfo_ProducesSnakeCaseJson() {
            // Arrange
            var env = new EnvironmentInfo {
                ProcessorCount = 8,
                TotalPhysicalMemory = 17179869184,
                AvailablePhysicalMemory = 8589934592,
                CommandLine = "/app/myapp --verbose",
                ProcessName = "MyApp",
                ProcessId = "12345",
                ProcessMemorySize = 524288000,
                ThreadName = "Main",
                ThreadId = "1",
                Architecture = "x64",
                OSName = "Windows",
                OSVersion = "10.0.19041",
                IpAddress = "192.168.1.100",
                MachineName = "PROD-SERVER-01",
                InstallId = "install-guid-123",
                RuntimeVersion = "6.0.5",
                Data = { ["FrameworkDescription"] = ".NET 6.0.5" }
            };

            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(env);

            // Assert
            SerializerContractAssertions.IncludesProperties(json,
                "processor_count", "total_physical_memory", "available_physical_memory",
                "command_line", "process_name", "process_id", "process_memory_size",
                "thread_name", "thread_id", "architecture", "o_s_name", "o_s_version",
                "ip_address", "machine_name", "install_id", "runtime_version", "data");
            SerializerContractAssertions.ExcludesProperties(json,
                "ProcessorCount", "TotalPhysicalMemory", "OSName", "MachineName");
        }

        [Fact]
        public void Deserialize_RoundTrip_PreservesAllProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new EnvironmentInfo {
                ProcessorCount = 16,
                TotalPhysicalMemory = 34359738368,
                AvailablePhysicalMemory = 17179869184,
                CommandLine = "dotnet run",
                ProcessName = "dotnet",
                ProcessId = "9876",
                ProcessMemorySize = 1073741824,
                ThreadName = "Worker",
                ThreadId = "42",
                Architecture = "arm64",
                OSName = "macOS",
                OSVersion = "12.3.1",
                IpAddress = "10.0.0.5",
                MachineName = "DEV-LAPTOP",
                InstallId = "guid-456",
                RuntimeVersion = "7.0.0"
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (EnvironmentInfo)serializer.Deserialize(json, typeof(EnvironmentInfo));

            // Assert
            Assert.Equal(original.ProcessorCount, deserialized.ProcessorCount);
            Assert.Equal(original.TotalPhysicalMemory, deserialized.TotalPhysicalMemory);
            Assert.Equal(original.AvailablePhysicalMemory, deserialized.AvailablePhysicalMemory);
            Assert.Equal(original.CommandLine, deserialized.CommandLine);
            Assert.Equal(original.ProcessName, deserialized.ProcessName);
            Assert.Equal(original.ProcessId, deserialized.ProcessId);
            Assert.Equal(original.ProcessMemorySize, deserialized.ProcessMemorySize);
            Assert.Equal(original.ThreadName, deserialized.ThreadName);
            Assert.Equal(original.ThreadId, deserialized.ThreadId);
            Assert.Equal(original.Architecture, deserialized.Architecture);
            Assert.Equal(original.OSName, deserialized.OSName);
            Assert.Equal(original.OSVersion, deserialized.OSVersion);
            Assert.Equal(original.IpAddress, deserialized.IpAddress);
            Assert.Equal(original.MachineName, deserialized.MachineName);
            Assert.Equal(original.InstallId, deserialized.InstallId);
            Assert.Equal(original.RuntimeVersion, deserialized.RuntimeVersion);
        }

        [Fact]
        public void Deserialize_MinimalEnvironmentInfo_PreservesSetProperties() {
            // Arrange
            var serializer = GetSerializer();
            var original = new EnvironmentInfo {
                MachineName = "TEST",
                ProcessorCount = 4
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (EnvironmentInfo)serializer.Deserialize(json, typeof(EnvironmentInfo));

            // Assert
            Assert.Equal("TEST", deserialized.MachineName);
            Assert.Equal(4, deserialized.ProcessorCount);
        }

        [Fact]
        public void Deserialize_WithCustomData_PreservesDataDictionary() {
            // Arrange
            var serializer = GetSerializer();
            var original = new EnvironmentInfo {
                MachineName = "SERVER-01",
                ProcessorCount = 4,
                Data = {
                    ["custom_key"] = "custom_value",
                    ["env_type"] = "production"
                }
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (EnvironmentInfo)serializer.Deserialize(json, typeof(EnvironmentInfo));

            // Assert
            Assert.NotNull(deserialized.Data);
            Assert.Equal(2, deserialized.Data.Count);
        }

        [Fact]
        public void Serialize_OSNameProperty_UsesExplicitJsonPropertyName() {
            // Arrange
            var env = new EnvironmentInfo { OSName = "Linux" };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(env);

            // Assert - OSName uses [JsonProperty("o_s_name")] attribute
            Assert.Contains("\"o_s_name\":\"Linux\"", json);
            Assert.DoesNotContain("\"OSName\"", json);
        }

        [Fact]
        public void Serialize_OSVersionProperty_UsesExplicitJsonPropertyName() {
            // Arrange
            var env = new EnvironmentInfo { OSVersion = "22.04" };
            var serializer = GetSerializer();

            // Act
            string json = serializer.Serialize(env);

            // Assert - OSVersion uses [JsonProperty("o_s_version")] attribute
            Assert.Contains("\"o_s_version\":\"22.04\"", json);
            Assert.DoesNotContain("\"OSVersion\"", json);
        }

        [Fact]
        public void Deserialize_WithLargeMemoryValues_PreservesLongValues() {
            // Arrange - 256 GB RAM
            var serializer = GetSerializer();
            var original = new EnvironmentInfo {
                MachineName = "BIG-SERVER",
                ProcessorCount = 64,
                TotalPhysicalMemory = 274877906944,
                AvailablePhysicalMemory = 137438953472
            };

            // Act
            string json = serializer.Serialize(original);
            var deserialized = (EnvironmentInfo)serializer.Deserialize(json, typeof(EnvironmentInfo));

            // Assert
            Assert.Equal(274877906944, deserialized.TotalPhysicalMemory);
            Assert.Equal(137438953472, deserialized.AvailablePhysicalMemory);
        }

        [Fact]
        public void Deserialize_SnakeCaseJsonInput_ParsesCorrectly() {
            // Arrange
            var serializer = GetSerializer();
            string json = "{\"processor_count\":12,\"total_physical_memory\":68719476736,\"available_physical_memory\":34359738368,\"command_line\":\"test\",\"process_name\":\"app\",\"process_id\":\"999\",\"process_memory_size\":1024,\"thread_name\":\"bg\",\"thread_id\":\"7\",\"architecture\":\"x64\",\"o_s_name\":\"Ubuntu\",\"o_s_version\":\"22.04\",\"ip_address\":\"10.0.0.1\",\"machine_name\":\"CLOUD-VM\",\"install_id\":\"id1\",\"runtime_version\":\"8.0.0\",\"data\":{}}";

            // Act
            var env = (EnvironmentInfo)serializer.Deserialize(json, typeof(EnvironmentInfo));

            // Assert
            Assert.Equal(12, env.ProcessorCount);
            Assert.Equal(68719476736, env.TotalPhysicalMemory);
            Assert.Equal("Ubuntu", env.OSName);
            Assert.Equal("22.04", env.OSVersion);
            Assert.Equal("CLOUD-VM", env.MachineName);
            Assert.Equal("x64", env.Architecture);
            Assert.Equal("8.0.0", env.RuntimeVersion);
        }
    }
}
