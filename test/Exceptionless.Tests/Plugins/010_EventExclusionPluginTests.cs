using System;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Plugins {
    public class EventExclusionPluginTests : PluginTestBase {
        public EventExclusionPluginTests(ITestOutputHelper output) : base(output) { }
        
        [Fact]
        public void EventExclusions() {
            var client = CreateClient();
            var plugin = new EventExclusionPlugin();

            // ignore any event that has a value of 2
            client.Configuration.AddEventExclusion(e => e.Value.GetValueOrDefault() != 2);

            var ev = new Event { Value = 1 };
            var context = new EventPluginContext(client, ev);
            plugin.Run(context);
            Assert.False(context.Cancel);

            ev.Value = 2;
            context = new EventPluginContext(client, ev);
            plugin.Run(context);
            Assert.True(context.Cancel);
        }

        [Theory]
        [InlineData(null, null, null, null, false)]
        [InlineData("Test", null, null, null, false)]
        [InlineData("Test", "Trace", null, null, true)]
        [InlineData("Test", "Off", null, null, true)]
        [InlineData("Test", "Abc", null, null, false)]
        [InlineData(null, "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix, "Off", true)]
        [InlineData(null, "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "*", "Off", true)]
        [InlineData("", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix, "Off", true)] // Becomes Global Log Level
        [InlineData("", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "*", "Off", true)]
        [InlineData("Test", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "Debug", true)]
        [InlineData("Test", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "false", true)]
        [InlineData("Test", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "no", true)]
        [InlineData("Test", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "0", true)]
        [InlineData("Test", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "true", false)]
        [InlineData("Test", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "yes", false)]
        [InlineData("Test", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "1", false)]
        [InlineData("Test", "Info", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "Debug", false)]
        [InlineData("Test", "Trace", SettingsDictionary.KnownKeys.LogLevelPrefix + "*", "Debug", true)]
        [InlineData("Test", "Warn", SettingsDictionary.KnownKeys.LogLevelPrefix + "*", "Debug", false)]
        [InlineData("Test", "Warn", SettingsDictionary.KnownKeys.LogLevelPrefix + "*", "Off", true)]
        public void LogLevels(string source, string level, string settingKey, string settingValue, bool cancelled) {
            var client = CreateClient();
            if (settingKey != null)
                client.Configuration.Settings.Add(settingKey, settingValue);

            var ev = new Event { Type = Event.KnownTypes.Log, Source = source };
            if (!String.IsNullOrEmpty(level))
                ev.SetProperty(Event.KnownDataKeys.Level, level);

            var context = new EventPluginContext(client, ev);
            var plugin = new EventExclusionPlugin();
            plugin.Run(context);
            Assert.Equal(cancelled, context.Cancel);
        }
        
        [Theory]
        [InlineData(null, null, null, null, false)]
        [InlineData("usage", null, null, null, false)]
        [InlineData("usage", "test", null, null, false)]
        [InlineData("usage", "test", "@@usage:Test", "true", false)]
        [InlineData("usage", "test", "@@usage:Test", "false", true)]
        [InlineData("usage", "EX-FEAT: 1234567890", "@@usage:EX-FEAT: 1234567890", "false", true)]
        [InlineData("usage", "test", "@@usage:*", "false", true)]
        [InlineData("404", null, "@@404:*", "false", true)]
        [InlineData("404", null, "@@404:", "false", true)]
        [InlineData("404", "", "@@404:", "false", true)]
        [InlineData("404", "/unknown", "@@404:*", "false", true)]
        [InlineData("404", "/unknown", "@@404:/unknown", "false", true)]
        [InlineData("404", "/unknown", "@@404:/unknown", "true", false)]
        [InlineData("404", "/example.php", "@@404:*.php", "false", true)]
        public void SourceType(string type, string source, string settingKey, string settingValue, bool cancelled) {
            var client = CreateClient();
            if (settingKey != null)
                client.Configuration.Settings.Add(settingKey, settingValue);

            var ev = new Event { Type = type, Source = source };
            var context = new EventPluginContext(client, ev);
            var plugin = new EventExclusionPlugin();
            plugin.Run(context);
            Assert.Equal(cancelled, context.Cancel);
            Assert.Equal(source, ev.Source);
        }

        [Theory]
        [InlineData(null, null, null, null, false)]
        [InlineData("Test", null, null, null, false)]
        [InlineData("Test", "Trace", null, null, true)]
        [InlineData("Test", "Warn", null, null, false)]
        [InlineData("Test", "Error", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "Debug", false)]
        [InlineData("Test", "Debug", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "Debug", false)]
        public void LogLevelsWithInfoDefault(string source, string level, string settingKey, string settingValue, bool cancelled) {
            var client = CreateClient();
            client.Configuration.Settings.Add(SettingsDictionary.KnownKeys.LogLevelPrefix + "*", "Info");
            if (settingKey != null)
                client.Configuration.Settings.Add(settingKey, settingValue);

            var ev = new Event { Type = Event.KnownTypes.Log, Source = source };
            if (!String.IsNullOrEmpty(level))
                ev.SetProperty(Event.KnownDataKeys.Level, level);

            var context = new EventPluginContext(client, ev);
            var plugin = new EventExclusionPlugin();
            plugin.Run(context);
            Assert.Equal(cancelled, context.Cancel);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("@@error:TestException", false)]
        [InlineData("@@error:Exception", false)]
        [InlineData("@@error:System.Exception", true)]
        [InlineData("@@error:*Exception", true)]
        [InlineData("@@error:*", true)]
        public void ExceptionType(string settingKey, bool cancelled) {
            var client = CreateClient();
            if (settingKey != null)
                client.Configuration.Settings.Add(settingKey, Boolean.FalseString);

            var plugin = new EventExclusionPlugin();
            var context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(GetException());
            plugin.Run(context);
            Assert.Equal(cancelled, context.Cancel);

            context.ContextData.SetException(GetNestedSimpleException());
            plugin.Run(context);
            Assert.Equal(cancelled, context.Cancel);
        }
    }
}