using Exceptionless.Models.Data;
using Exceptionless.Tests.Serializer;
using Xunit;

namespace Exceptionless.Tests.Serializer.Models {
    public class EnvironmentInfoSerializerTests : SerializerTestBase {
        /* lang=json */
        private const string MinimalJson = """{"processor_count":0,"total_physical_memory":0,"available_physical_memory":0,"command_line":null,"process_name":null,"process_id":null,"process_memory_size":0,"thread_name":null,"thread_id":null,"architecture":null,"o_s_name":null,"o_s_version":null,"ip_address":null,"machine_name":null,"install_id":null,"runtime_version":null,"data":{}}""";
        /* lang=json */
        private const string CompleteJson = """{"processor_count":4,"total_physical_memory":8192,"available_physical_memory":4096,"command_line":"TestCommandLine","process_name":"TestProcess","process_id":"12345","process_memory_size":2048,"thread_name":"Thread","thread_id":"67890","architecture":"x64","o_s_name":"Windows","o_s_version":"10.0.19042","ip_address":"192.168.1.1","machine_name":"Machine","install_id":"InstallId","runtime_version":"5.0.0","data":{"FrameworkDescription":"Test"}}""";
        /* lang=json */
        private const string OsOnlyJson = """{"processor_count":0,"total_physical_memory":0,"available_physical_memory":0,"command_line":null,"process_name":null,"process_id":null,"process_memory_size":0,"thread_name":null,"thread_id":null,"architecture":null,"o_s_name":"Windows","o_s_version":"11.0","ip_address":null,"machine_name":null,"install_id":null,"runtime_version":null,"data":{}}""";

        [Fact]
        public void Serialize_MinimalEnvironmentInfo_ProducesCorrectJson() {
            // Arrange
            var environmentInfo = new EnvironmentInfo();

            // Act
            string json = Serialize(environmentInfo);

            // Assert
            Assert.Equal(MinimalJson, json);
        }

        [Fact]
        public void Serialize_CompleteEnvironmentInfo_ProducesCorrectJson() {
            // Arrange
            var environmentInfo = CreateCompleteEnvironmentInfo();

            // Act
            string json = Serialize(environmentInfo);

            // Assert
            Assert.Equal(CompleteJson, json);
        }

        [Fact]
        public void Deserialize_EnvironmentInfo_RoundTrips() {
            // Arrange
            var environmentInfo = CreateCompleteEnvironmentInfo();

            // Act
            EnvironmentInfo roundTripped = RoundTrip(environmentInfo);

            // Assert
            Assert.Equal(4, roundTripped.ProcessorCount);
            Assert.Equal(8192, roundTripped.TotalPhysicalMemory);
            Assert.Equal(4096, roundTripped.AvailablePhysicalMemory);
            Assert.Equal("TestCommandLine", roundTripped.CommandLine);
            Assert.Equal("TestProcess", roundTripped.ProcessName);
            Assert.Equal("12345", roundTripped.ProcessId);
            Assert.Equal(2048, roundTripped.ProcessMemorySize);
            Assert.Equal("Thread", roundTripped.ThreadName);
            Assert.Equal("67890", roundTripped.ThreadId);
            Assert.Equal("x64", roundTripped.Architecture);
            Assert.Equal("Windows", roundTripped.OSName);
            Assert.Equal("10.0.19042", roundTripped.OSVersion);
            Assert.Equal("192.168.1.1", roundTripped.IpAddress);
            Assert.Equal("Machine", roundTripped.MachineName);
            Assert.Equal("InstallId", roundTripped.InstallId);
            Assert.Equal("5.0.0", roundTripped.RuntimeVersion);
            Assert.Equal("Test", roundTripped.Data["FrameworkDescription"]);
        }

        [Fact]
        public void Deserialize_EnvironmentInfo_FromKnownJson_MapsAllProperties() {
            // Arrange
            const string json = CompleteJson;

            // Act
            EnvironmentInfo environmentInfo = Deserialize<EnvironmentInfo>(json);

            // Assert
            Assert.Equal(4, environmentInfo.ProcessorCount);
            Assert.Equal(8192, environmentInfo.TotalPhysicalMemory);
            Assert.Equal(4096, environmentInfo.AvailablePhysicalMemory);
            Assert.Equal("TestCommandLine", environmentInfo.CommandLine);
            Assert.Equal("TestProcess", environmentInfo.ProcessName);
            Assert.Equal("12345", environmentInfo.ProcessId);
            Assert.Equal(2048, environmentInfo.ProcessMemorySize);
            Assert.Equal("Thread", environmentInfo.ThreadName);
            Assert.Equal("67890", environmentInfo.ThreadId);
            Assert.Equal("x64", environmentInfo.Architecture);
            Assert.Equal("Windows", environmentInfo.OSName);
            Assert.Equal("10.0.19042", environmentInfo.OSVersion);
            Assert.Equal("192.168.1.1", environmentInfo.IpAddress);
            Assert.Equal("Machine", environmentInfo.MachineName);
            Assert.Equal("InstallId", environmentInfo.InstallId);
            Assert.Equal("5.0.0", environmentInfo.RuntimeVersion);
            Assert.Equal("Test", environmentInfo.Data["FrameworkDescription"]);
        }

        [Fact]
        public void Serialize_EnvironmentInfo_UsesExplicitOsPropertyNames() {
            // Arrange
            var environmentInfo = new EnvironmentInfo {
                OSName = "Windows",
                OSVersion = "11.0"
            };

            // Act
            string json = Serialize(environmentInfo);

            // Assert
            Assert.Equal(OsOnlyJson, json);
        }

        private static EnvironmentInfo CreateCompleteEnvironmentInfo() {
            return new EnvironmentInfo {
                ProcessorCount = 4,
                TotalPhysicalMemory = 8192,
                AvailablePhysicalMemory = 4096,
                CommandLine = "TestCommandLine",
                ProcessName = "TestProcess",
                ProcessId = "12345",
                ProcessMemorySize = 2048,
                ThreadName = "Thread",
                ThreadId = "67890",
                Architecture = "x64",
                OSName = "Windows",
                OSVersion = "10.0.19042",
                IpAddress = "192.168.1.1",
                MachineName = "Machine",
                InstallId = "InstallId",
                RuntimeVersion = "5.0.0",
                Data = {
                    ["FrameworkDescription"] = "Test"
                }
            };
        }
    }
}
