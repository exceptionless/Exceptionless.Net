using System.Collections.Generic;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class ConfigurationDefaultsPluginTests : PluginTestBase {
        public ConfigurationDefaultsPluginTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void EnsureNoDuplicateTagsOrData() {
            var client = CreateClient();
            var context = new EventPluginContext(client, new Event());

            var plugin = new ConfigurationDefaultsPlugin();
            plugin.Run(context);
            Assert.Empty(context.Event.Tags);

            client.Configuration.DefaultTags.Add(Event.KnownTags.Critical);
            plugin.Run(context);
            Assert.Single(context.Event.Tags);
            Assert.Empty(context.Event.Data);

            client.Configuration.DefaultData.Add("Message", new { Exceptionless = "Is Awesome!" });
            for (int index = 0; index < 2; index++) {
                plugin.Run(context);
                Assert.Single(context.Event.Tags);
                Assert.Single(context.Event.Data);
            }
        }

        [Fact]
        public void IgnoredProperties() {
            var client = CreateClient();
            client.Configuration.DefaultData.Add("Message", "Test");

            var context = new EventPluginContext(client, new Event());
            var plugin = new ConfigurationDefaultsPlugin();
            plugin.Run(context);
            Assert.Single(context.Event.Data);
            Assert.Equal("Test", context.Event.Data["Message"]);

            client.Configuration.AddDataExclusions("Ignore*");
            client.Configuration.DefaultData.Add("Ignored", "Test");
            plugin.Run(context);
            Assert.Single(context.Event.Data);
            Assert.Equal("Test", context.Event.Data["Message"]);
        }

        [Fact]
        public void SerializedProperties() {
            var client = CreateClient();
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.EnvironmentInfo, new EnvironmentInfo { MachineName = "blake" });
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.Error, new Error { Message = "blake" });
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.Level, "Debug");
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.ManualStackingInfo, "blake");
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.RequestInfo, new RequestInfo { Host = "blake" });
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.SimpleError, new SimpleError { Message = "blake" });
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.SubmissionMethod, "test");
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.TraceLog, new List<string>());
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.UserDescription, new UserDescription("blake@test.com", "blake"));
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.UserInfo, new UserInfo("blake"));
            client.Configuration.DefaultData.Add(Event.KnownDataKeys.Version, "1.0");

            var serializer = client.Configuration.Resolver.GetJsonSerializer();
            var context = new EventPluginContext(client, new Event());
            var plugin = new ConfigurationDefaultsPlugin();
            plugin.Run(context);
            Assert.Equal(11, context.Event.Data.Count);
            Assert.True(context.Event.Data[Event.KnownDataKeys.EnvironmentInfo] is string);
            Assert.Equal("blake", context.Event.GetEnvironmentInfo().MachineName);
            Assert.Equal("blake", context.Event.GetEnvironmentInfo(serializer).MachineName);
            Assert.True(context.Event.Data[Event.KnownDataKeys.Error] is string);
            Assert.Equal("blake", context.Event.GetError().Message);
            Assert.Equal("blake", context.Event.GetError(serializer).Message);
            Assert.Equal("Debug", context.Event.Data[Event.KnownDataKeys.Level]);
            Assert.Equal("blake", context.Event.Data[Event.KnownDataKeys.ManualStackingInfo]);
            Assert.True(context.Event.Data[Event.KnownDataKeys.RequestInfo] is string);
            Assert.Equal("blake", context.Event.GetRequestInfo().Host);
            Assert.Equal("blake", context.Event.GetRequestInfo(serializer).Host);
            Assert.True(context.Event.Data[Event.KnownDataKeys.SimpleError] is string);
            Assert.Equal("blake", context.Event.GetSimpleError().Message);
            Assert.Equal("blake", context.Event.GetSimpleError(serializer).Message);
            Assert.Equal("test", context.Event.Data[Event.KnownDataKeys.SubmissionMethod]);
            Assert.True(context.Event.Data[Event.KnownDataKeys.TraceLog] is string);
            Assert.True(context.Event.Data[Event.KnownDataKeys.UserDescription] is string);
            Assert.Equal("blake", context.Event.GetUserDescription().Description);
            Assert.Equal("blake", context.Event.GetUserDescription(serializer).Description);
            Assert.True(context.Event.Data[Event.KnownDataKeys.UserInfo] is string);
            Assert.Equal("blake", context.Event.GetUserIdentity().Identity);
            Assert.Equal("blake", context.Event.GetUserIdentity(serializer).Identity);
            Assert.Equal("1.0", context.Event.Data[Event.KnownDataKeys.Version]);

            context.Event.SetUserIdentity(new UserInfo("blake"));
            Assert.Equal("blake", context.Event.GetUserIdentity().Identity);
            Assert.Equal("blake", context.Event.GetUserIdentity(serializer).Identity);
        }
    }
}