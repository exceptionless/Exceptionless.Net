using System;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class SetEnvironmentUserPluginTests : PluginTestBase {
        public SetEnvironmentUserPluginTests(ITestOutputHelper output) : base(output) { }
        
        [Fact]
        public void WillSetIdentity() {
            var client = CreateClient();
            var plugin = new SetEnvironmentUserPlugin();

            var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Log, Message = "test" });
            plugin.Run(context);

            var user = context.Event.GetUserIdentity();
            Assert.Equal(Environment.UserName, user.Identity);
        }
        
        [Fact]
        public void WillRespectIncludeUserName() {
            var client = CreateClient(c => c.IncludeUserName = false);
            var plugin = new SetEnvironmentUserPlugin();

            var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Log, Message = "test" });
            plugin.Run(context);

            Assert.Null(context.Event.GetUserIdentity());
        }

        [Fact]
        public void WillNotUpdateIdentity() {
            var client = CreateClient();
            var plugin = new SetEnvironmentUserPlugin();

            var ev = new Event { Type = Event.KnownTypes.Log, Message = "test" };
            ev.SetUserIdentity(null, "Blake");
            var context = new EventPluginContext(client, ev);
            plugin.Run(context);

            var user = context.Event.GetUserIdentity();
            Assert.Null(user.Identity);
            Assert.Equal("Blake", user.Name);
        }
    }
}