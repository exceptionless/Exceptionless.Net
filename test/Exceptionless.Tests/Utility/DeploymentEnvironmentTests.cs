using Exceptionless.Utility;
using Xunit;

namespace Exceptionless.Tests.Utility {
    public class DeploymentEnvironmentTests {
        [Theory]
        [InlineData("production", "production")]
        [InlineData("Staging-EU", "Staging-EU")]
        [InlineData("qa.2", "qa.2")]
        [InlineData("dev_local", "dev_local")]
        [InlineData("a", "a")]
        [InlineData("0", "0")]
        [InlineData("  Production \t", "Production")]
        [InlineData(" Production ", "Production")]
        public void Normalize_ValidName_TrimsAndPreservesCasing(string value, string expected) {
            Assert.Equal(expected, DeploymentEnvironment.Normalize(value));
        }

        [Fact]
        public void Normalize_SixtyFourCharacters_IsValidAfterTrimming() {
            string name = "a" + new string('b', 63);
            Assert.Equal(name, DeploymentEnvironment.Normalize(name));
            Assert.Equal(name, DeploymentEnvironment.Normalize("  " + name + "  "));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(" \t\r\n ")]
        [InlineData("Staging West")]
        [InlineData("prod/eu")]
        [InlineData("prod:eu")]
        [InlineData("-prod")]
        [InlineData(".prod")]
        [InlineData("_prod")]
        [InlineData("produção")]
        [InlineData("prod\ninvalid")]
        [InlineData("prod\tinvalid")]
        [InlineData("pro\u0000d")]
        [InlineData("prod\u007f")]
        [InlineData("pro\u0085d")]
        [InlineData("prod​")]
        public void Normalize_InvalidName_ReturnsNull(string? value) {
            Assert.Null(DeploymentEnvironment.Normalize(value));
        }

        [Fact]
        public void Normalize_SixtyFiveCharacters_ReturnsNull() {
            Assert.Null(DeploymentEnvironment.Normalize(new string('a', 65)));
            Assert.Null(DeploymentEnvironment.Normalize("  " + new string('a', 65) + "  "));
        }
    }
}
