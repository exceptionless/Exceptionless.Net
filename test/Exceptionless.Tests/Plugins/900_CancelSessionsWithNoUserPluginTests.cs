using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class CancelSessionsWithNoUserPluginTests : PluginTestBase {
        public CancelSessionsWithNoUserPluginTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(Event.KnownTypes.Error, null, false)]
        [InlineData(Event.KnownTypes.FeatureUsage, null, false)]
        [InlineData(Event.KnownTypes.Log, null, false)]
        [InlineData(Event.KnownTypes.NotFound, null, false)]
        [InlineData(Event.KnownTypes.Session, null, true)]
        [InlineData(Event.KnownTypes.Session, "123456789", false)]
        public void CancelSessionsWithNoUserTest(string eventType, string identity, bool cancelled) {
            var ev = new Event { Type = eventType };
            ev.SetUserIdentity(identity);

            var context = new EventPluginContext(CreateClient(), ev);
            var plugin = new CancelSessionsWithNoUserPlugin();
            plugin.Run(context);
            Assert.Equal(cancelled, context.Cancel);
        }
    }
}