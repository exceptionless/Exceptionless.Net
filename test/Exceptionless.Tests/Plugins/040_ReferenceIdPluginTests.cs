using Exceptionless.Plugins;
using Exceptionless.Models;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class ReferenceIdPluginTests : PluginTestBase {
        public ReferenceIdPluginTests(ITestOutputHelper output) : base(output) { }
        
        [Fact]
        public void ShouldUseReferenceIds() {
            var client = CreateClient();
            foreach (var plugin in client.Configuration.Plugins)
                client.Configuration.RemovePlugin(plugin.Key);

            var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Error });
            EventPluginManager.Run(context);
            Assert.Null(context.Event.ReferenceId);

            client.Configuration.UseReferenceIds();
            context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Error });
            EventPluginManager.Run(context);
            Assert.NotNull(context.Event.ReferenceId);
        }
    }
}