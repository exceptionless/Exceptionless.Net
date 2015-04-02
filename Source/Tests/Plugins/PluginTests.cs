using System;
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Xunit;

namespace Exceptionless.Tests.Plugins {
    public class PluginTests {
        [Fact]
        public void ConfigurationDefaults_EnsureNoDuplicateTagsOrData() {
            var client = new ExceptionlessClient();
            var context = new EventPluginContext(client, new Event());

            var plugin = new ConfigurationDefaultsPlugin();
            plugin.Run(context);
            Assert.Equal(0, context.Event.Tags.Count);

            client.Configuration.DefaultTags.Add(Event.KnownTags.Critical);
            plugin.Run(context);
            Assert.Equal(1, context.Event.Tags.Count);
            Assert.Equal(0, context.Event.Data.Count);

            client.Configuration.DefaultData.Add("Message", new { Exceptionless = "Is Awesome!" });
            for (int index = 0; index < 2; index++) {
                plugin.Run(context);
                Assert.Equal(1, context.Event.Tags.Count);
                Assert.Equal(1, context.Event.Data.Count);
            }
        }

        [Theory(Skip = "TODO: This needs to be skipped until the client is sending session start and end.")]
        [InlineData(Event.KnownTypes.Error)]
        [InlineData(Event.KnownTypes.FeatureUsage)]
        [InlineData(Event.KnownTypes.Log)]
        [InlineData(Event.KnownTypes.NotFound)]
        [InlineData(Event.KnownTypes.SessionEnd)]
        public void EnvironmentInfo_IncorrectEventType(string eventType) {
            var client = new ExceptionlessClient();
            var context = new EventPluginContext(client, new Event { Type = eventType });

            var plugin = new EnvironmentInfoPlugin();
            plugin.Run(context);
            Assert.Equal(0, context.Event.Data.Count);
        }

        [Fact]
        public void EnvironmentInfo_ShouldAddSessionStart() {
            var client = new ExceptionlessClient();
            var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.SessionStart });
         
            var plugin = new EnvironmentInfoPlugin();
            plugin.Run(context);
            Assert.Equal(1, context.Event.Data.Count);
            Assert.NotNull(context.Event.Data[Event.KnownDataKeys.EnvironmentInfo]);
        }

        [Fact]
        public void CanCancel() {
            var client = new ExceptionlessClient();
            foreach (var plugin in client.Configuration.Plugins)
                client.Configuration.RemovePlugin(plugin.Key);

            client.Configuration.AddPlugin(ctx => ctx.Cancel = true);
            client.Configuration.AddPlugin(ctx => ctx.Event.Tags.Add("Was Not Canceled"));

            var context = new EventPluginContext(client, new Event());
            EventPluginManager.Run(context);
            Assert.True(context.Cancel);
            Assert.Equal(0, context.Event.Tags.Count);
        }

        [Fact]
        public void VerifyPriority() {
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());
            foreach (var plugin in config.Plugins)
                config.RemovePlugin(plugin.Key);

            Assert.Equal(0, config.Plugins.Count());
            config.AddPlugin<EnvironmentInfoPlugin>();
            config.AddPlugin<PluginWithPriority11>();
            config.AddPlugin<PluginWithNoPriority>();
            config.AddPlugin("version3", 1, ctx => ctx.Event.SetVersion("1.0.0.0"));
            config.AddPlugin("version", 1, ctx => ctx.Event.SetVersion("1.0.0.0"));
            config.AddPlugin("version2", 1, ctx => ctx.Event.SetVersion("1.0.0.0"));

            var plugins = config.Plugins.ToArray();
            Assert.Equal(typeof(PluginWithNoPriority), plugins[0].Plugin.GetType());
            Assert.Equal("version3", plugins[1].Key);
            Assert.Equal("version", plugins[2].Key);
            Assert.Equal("version2", plugins[3].Key);
            Assert.Equal(typeof(PluginWithPriority11), plugins[4].Plugin.GetType());
            Assert.Equal(typeof(EnvironmentInfoPlugin), plugins[5].Plugin.GetType());
        }

        public class PluginWithNoPriority : IEventPlugin {
            public void Run(EventPluginContext context) {}
        }

        [Priority(11)]
        public class PluginWithPriority11 : IEventPlugin {
            public void Run(EventPluginContext context) {}
        }
    }
}