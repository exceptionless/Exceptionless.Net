using System.Threading.Tasks;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class EnvironmentInfoPluginTests : PluginTestBase {
        public EnvironmentInfoPluginTests(ITestOutputHelper output) : base(output) { }
        
        [Fact]
        public void CanRunInParallel() {
            var client = CreateClient();
            var plugin = new EnvironmentInfoPlugin();

            Parallel.For(0, 10000, i => {
                var ev = new Event { Type = Event.KnownTypes.Session };
                var context = new EventPluginContext(client, ev);
                plugin.Run(context);
                Assert.Single(context.Event.Data);
                Assert.NotNull(context.Event.Data[Event.KnownDataKeys.EnvironmentInfo]);
            });
        }

        [Fact]
        public void ShouldAddSessionStart() {
            var client = CreateClient();
            var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Session });

            var plugin = new EnvironmentInfoPlugin();
            plugin.Run(context);
            Assert.Single(context.Event.Data);
            Assert.NotNull(context.Event.Data[Event.KnownDataKeys.EnvironmentInfo]);
        }
    }
}