using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class IgnoreUserAgentPluginTests : PluginTestBase {
        public IgnoreUserAgentPluginTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void DiscardBot() {
            var client = CreateClient();
            client.Configuration.AddUserAgentBotPatterns("*Bot*");
            var plugin = new IgnoreUserAgentPlugin();

            var ev = new Event();
            var context = new EventPluginContext(client, ev);
            plugin.Run(context);
            Assert.False(context.Cancel);

            ev.AddRequestInfo(new RequestInfo { UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_3) AppleWebKit/601.4.4 (KHTML, like Gecko) Version/9.0.3 Safari/601.4.4" });
            context = new EventPluginContext(client, ev);
            plugin.Run(context);
            Assert.False(context.Cancel);

            ev.AddRequestInfo(new RequestInfo { UserAgent = "Mozilla/5.0 (compatible; bingbot/2.0 +http://www.bing.com/bingbot.htm)" });
            context = new EventPluginContext(client, ev);
            plugin.Run(context);
            Assert.True(context.Cancel);
        }
    }
}