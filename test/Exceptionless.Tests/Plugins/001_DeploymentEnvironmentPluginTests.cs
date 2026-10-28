using Exceptionless.Models;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Xunit;

namespace Exceptionless.Tests.Plugins {
    public class DeploymentEnvironmentPluginTests : PluginTestBase {
        public DeploymentEnvironmentPluginTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Run_DeploymentEnvironment_UsesDefaultAndPreservesEventOverride() {
            var client = CreateClient();
            client.Configuration.SetEnvironment(" Production ");
            var plugin = new DeploymentEnvironmentPlugin();
            var context = new EventPluginContext(client, new Event());
            plugin.Run(context);
            Assert.Equal("Production", context.Event.Environment);

            var builder = client.CreateLog("test", "message").SetEnvironment(" Staging ");
            var overridden = new EventPluginContext(client, builder.Target);
            plugin.Run(overridden);
            Assert.Equal("Staging", overridden.Event.Environment);
            Assert.Empty(overridden.Event.Data);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("prod\ninvalid")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        public void Run_InvalidDeploymentEnvironment_DoesNotUseDefault(string environment) {
            var client = CreateClient();
            client.Configuration.SetEnvironment("production");
            var builder = client.CreateLog("test", "message").SetEnvironment(environment);
            var context = new EventPluginContext(client, builder.Target);
            new DeploymentEnvironmentPlugin().Run(context);
            Assert.Null(context.Event.Environment);
        }

        [Theory]
        [InlineData(null, "development", true)]
        [InlineData("production", "production", false)]
        [InlineData("", null, false)]
        public void Pipeline_ExclusionCallbacks_SeeEffectiveEnvironment(string? environment, string? expectedEnvironment, bool cancelled) {
            using var client = CreateClient();
            client.Configuration.Environment = "development";
            string? observedEnvironment = null;
            client.Configuration.AddEventExclusion(ev => {
                observedEnvironment = ev.Environment;
                return ev.Environment != "development";
            });
            var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.FeatureUsage, Environment = environment });

            EventPluginManager.Run(context);

            Assert.Equal(expectedEnvironment, observedEnvironment);
            Assert.Equal(cancelled, context.Cancel);
        }
    }
}
