#if NET45
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using Xunit;

namespace Exceptionless.Tests.Configuration {
    public class DeploymentEnvironmentConfigurationTests {
        [Fact]
        public void ReadFromConfigSection_LoadsEnvironmentAttribute() {
            string path = Path.GetTempFileName();
            try {
                File.WriteAllText(path, "<configuration><configSections><section name=\"exceptionless\" type=\"Exceptionless.ExceptionlessSection, Exceptionless\" /></configSections><exceptionless apiKey=\"test\" environment=\" Staging \" /></configuration>");
                var mapped = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = path }, ConfigurationUserLevel.None);
                using var client = new ExceptionlessClient();
                client.Configuration.ReadFromConfigSection((ExceptionlessSection)mapped.GetSection("exceptionless"));
                Assert.Equal("Staging", client.Configuration.Environment);
            } finally {
                File.Delete(path);
            }
        }

        [Fact]
        public void ReadFromConfigSection_MissingEnvironment_PreservesConfiguredValue() {
            using var client = new ExceptionlessClient();
            client.Configuration.Environment = "staging";
            client.Configuration.ReadFromConfigSection(new ExceptionlessSection());
            Assert.Equal("staging", client.Configuration.Environment);
        }

        [Fact]
        public void ReadFromAppSettings_LoadsEnvironmentAndPreservesMissingSetting() {
            using var client = new ExceptionlessClient();
            client.Configuration.Environment = "staging";
            client.Configuration.ReadFromAppSettings(new NameValueCollection());
            Assert.Equal("staging", client.Configuration.Environment);
            client.Configuration.ReadFromAppSettings(new NameValueCollection { ["Exceptionless:Environment"] = " Production " });
            Assert.Equal("Production", client.Configuration.Environment);
        }
    }
}
#endif
