using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Submission;
using Exceptionless.Tests.Log;
using Exceptionless.Tests.Utility;
using Xunit;
using Xunit.Abstractions;
using LogLevel = Exceptionless.Logging.LogLevel;

namespace Exceptionless.Tests.Plugins {
    public class PluginTests {
        private readonly TestOutputWriter _writer;
        public PluginTests(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        private ExceptionlessClient CreateClient() {
            return new ExceptionlessClient(c => {
                c.UseLogger(new XunitExceptionlessLog(_writer) { MinimumLogLevel = LogLevel.Trace   });
                c.ReadFromAttributes();
                c.UserAgent = "testclient/1.0.0.0";

                // Disable updating settings.
                c.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
            });
        }

        [Fact]
        public void ConfigurationDefaults_EnsureNoDuplicateTagsOrData() {
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
        public void ConfigurationDefaults_IgnoredProperties() {
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
        public void ConfigurationDefaults_SerializedProperties() {
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

        [Fact]
        public void EventExclusionPlugin_EventExclusions() {
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
        [InlineData("Test", "Trace", null, null, false)]
        [InlineData("Test", "Off", null, null, true)]
        [InlineData("Test", "Abc", null, null, false)]
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
        public void EventExclusionPlugin_LogLevels(string source, string level, string settingKey, string settingValue, bool cancelled) {
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
        [InlineData("404", "/unknown", "@@404:*", "false", true)]
        [InlineData("404", "/unknown", "@@404:/unknown", "false", true)]
        [InlineData("404", "/unknown", "@@404:/unknown", "true", false)]
        public void EventExclusionPlugin_SourceType(string type, string source, string settingKey, string settingValue, bool cancelled) {
            var client = CreateClient();
            if (settingKey != null)
                client.Configuration.Settings.Add(settingKey, settingValue);

            var ev = new Event { Type = type, Source = source };
            var context = new EventPluginContext(client, ev);
            var plugin = new EventExclusionPlugin();
            plugin.Run(context);
            Assert.Equal(cancelled, context.Cancel);
        }

        [Theory]
        [InlineData(null, null, null, null, false)]
        [InlineData("Test", null, null, null, false)]
        [InlineData("Test", "Trace", null, null, true)]
        [InlineData("Test", "Warn", null, null, false)]
        [InlineData("Test", "Error", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "Debug", false)]
        [InlineData("Test", "Debug", SettingsDictionary.KnownKeys.LogLevelPrefix + "Test", "Debug", false)]
        public void EventExclusionPlugin_LogLevelsWithInfoDefault(string source, string level, string settingKey, string settingValue, bool cancelled) {
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
        public void EventExclusionPlugin_ExceptionType(string settingKey, bool cancelled) {
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

        [Fact]
        public void IgnoreUserAgentPlugin_DiscardBot() {
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

        [Fact]
        public void HandleAggregateExceptionsPlugin_SingleInnerException() {
            var client = CreateClient();
            var plugin = new HandleAggregateExceptionsPlugin();

            var exceptionOne = new Exception("one");
            var exceptionTwo = new Exception("two");

            var context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(exceptionOne);
            plugin.Run(context);
            Assert.False(context.Cancel);

            context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(new AggregateException(exceptionOne));
            plugin.Run(context);
            Assert.False(context.Cancel);
            Assert.Equal(exceptionOne, context.ContextData.GetException());

            context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(new AggregateException(exceptionOne, exceptionTwo));
            plugin.Run(context);
            Assert.True(context.Cancel);
        }

        [Fact]
        public void HandleAggregateExceptionsPlugin_MultipleInnerException() {
            var submissionClient = new InMemorySubmissionClient();
            var client = new ExceptionlessClient("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw");
            client.Configuration.Resolver.Register<ISubmissionClient>(submissionClient);

            var plugin = new HandleAggregateExceptionsPlugin();
            var exceptionOne = new Exception("one");
            var exceptionTwo = new Exception("two");

            var context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(new AggregateException(exceptionOne, exceptionTwo));
            plugin.Run(context);
            Assert.True(context.Cancel);

            client.ProcessQueue();
            Assert.Equal(2, submissionClient.Events.Count);
        }

        [Fact]
        public void ErrorPlugin_WillPreserveLineBreaks() {
            const string message = "Test\r\nLine\r\n\tBreaks";

            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            var client = CreateClient();
            foreach (var plugin in errorPlugins) {
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(new NotSupportedException(message));
                plugin.Run(context);
                Assert.False(context.Cancel);

                Assert.Equal(Event.KnownTypes.Error, context.Event.Type);
                var error = context.Event.GetError();
                if (error != null)
                    Assert.Equal(message, error.Message);
                else
                    Assert.Equal(message, context.Event.GetSimpleError().Message);
            }
        }


        [Fact]
        public void ErrorPlugin_WillPreserveEventType() {
            const string message = "Testing";

            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            var client = CreateClient();
            foreach (var plugin in errorPlugins) {
                var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Log });
                context.ContextData.SetException(new NotSupportedException(message));
                plugin.Run(context);
                Assert.False(context.Cancel);

                Assert.Equal(Event.KnownTypes.Log, context.Event.Type);
                var error = context.Event.GetError();
                if (error != null)
                    Assert.Equal(message, error.Message);
                else
                    Assert.Equal(message, context.Event.GetSimpleError().Message);
            }
        }

        [Fact (Skip = "There is a bug in the .NET Framework where non thrown exceptions with non custom stack traces cannot be computed #116")]
        public void ErrorPlugin_CanHandleExceptionWithOverriddenStackTrace() {
            var client = CreateClient();
            var plugin = new ErrorPlugin();

            var context = new EventPluginContext(client, new Event());
            context.ContextData.SetException(GetExceptionWithOverriddenStackTrace());
            plugin.Run(context);
            Assert.False(context.Cancel);

            var error = context.Event.GetError();
            Assert.True(error.StackTrace.Count > 0);

            context.ContextData.SetException(new ExceptionWithOverriddenStackTrace("test"));
            plugin.Run(context);
            Assert.False(context.Cancel);

            error = context.Event.GetError();
            Assert.True(error.StackTrace.Count > 0);
        }

        [Fact]
        public void ErrorPlugin_DiscardDuplicates() {
            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                var exception = new Exception("Nested", new MyApplicationException("Test") {
                    IgnoredProperty = "Test",
                    RandomValue = "Test"
                });

                var client = CreateClient();
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.False(context.Cancel);

                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);

                context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.True(context.Cancel);

                error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.Null(error);
            }
        }

        public static IEnumerable<object[]> DifferentExceptionDataDictionaryTypes {
            get {
                return new[] {
                    new object[] { null, false, 0 },
                    new object[] { new Dictionary<object, object> { { (object)1, (object)1 } }, true, 1 },
                    new object[] { new Dictionary<PriorityAttribute, PriorityAttribute>() { { new PriorityAttribute(1), new PriorityAttribute(1) } }, false, 1 },
                    new object[] { new Dictionary<int, int> { { 1, 1 } }, false, 1 },
                    new object[] { new Dictionary<bool, bool> { { false, false } }, false, 1 },
                    new object[] { new Dictionary<Guid, Guid> { { Guid.Empty, Guid.Empty } }, false, 1 },
                    new object[] { new Dictionary<IData, IData> { { new SimpleError(), new SimpleError() } }, false, 1 },
                    new object[] { new Dictionary<TestEnum, TestEnum> { { TestEnum.None, TestEnum.None } }, false, 1 },
                    new object[] { new Dictionary<TestStruct, TestStruct> { { new TestStruct(), new TestStruct() } }, false, 1 },
                    new object[] { new Dictionary<string, string> { { "test", "string" } }, true, 1 },
                    new object[] { new Dictionary<string, object> { { "test", "object" } }, true, 1 },
                    new object[] { new Dictionary<string, PriorityAttribute> { { "test", new PriorityAttribute(1) } }, true, 1 },
                    new object[] { new Dictionary<string, Guid> { { "test", Guid.Empty } }, true, 1 },
                    new object[] { new Dictionary<string, IData> { { "test", new SimpleError() } }, true, 1 },
                    new object[] { new Dictionary<string, TestEnum> { { "test", TestEnum.None } }, true, 1 },
                    new object[] { new Dictionary<string, TestStruct> { { "test", new TestStruct() } }, true, 1 },
                    new object[] { new Dictionary<string, int> { { "test", 1 } }, true, 1 },
                    new object[] { new Dictionary<string, bool> { { "test", false } }, true, 1 }
                };
            }
        }

        [Theory]
        [MemberData(nameof(DifferentExceptionDataDictionaryTypes))]
        public void ErrorPlugin_CanProcessDifferentExceptionDataDictionaryTypes(IDictionary data, bool canMarkAsProcessed, int processedDataItemCount) {
            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                if (data != null && data.Contains("@exceptionless"))
                    data.Remove("@exceptionless");

                var exception = new MyApplicationException("Test") { SetsDataProperty = data };
                var client = CreateClient();
                client.Configuration.AddDataExclusions("SetsDataProperty");
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.False(context.Cancel);

                Assert.Equal(canMarkAsProcessed, exception.Data != null && exception.Data.Contains("@exceptionless"));

                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.Equal(processedDataItemCount, error.Data.Count);
            }
        }

        [Fact]
        public void ErrorPlugin_CopyExceptionDataToRootErrorData() {
            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                var exception = new MyApplicationException("Test") {
                    RandomValue = "Test",
                    SetsDataProperty = new Dictionary<object, string> {
                        { 1, 1.GetType().Name },
                        { "test", "test".GetType().Name },
                        { Guid.NewGuid(), typeof(Guid).Name },
                        { false, typeof(bool).Name }
                    }
                };

                var client = CreateClient();
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);
                plugin.Run(context);
                Assert.False(context.Cancel);

                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.Equal(5, error.Data.Count);
            }
        }

        [Fact]
        public void ErrorPlugin_IgnoredProperties() {
            var exception = new MyApplicationException("Test") {
                IgnoredProperty = "Test",
                RandomValue = "Test"
            };

            var errorPlugins = new List<IEventPlugin> {
                new ErrorPlugin(),
                new SimpleErrorPlugin()
            };

            foreach (var plugin in errorPlugins) {
                var client = CreateClient();
                var context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);

                plugin.Run(context);
                var error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.True(error.Data.ContainsKey(Error.KnownDataKeys.ExtraProperties));
                string json = error.Data[Error.KnownDataKeys.ExtraProperties] as string;
                Assert.Equal("{\"IgnoredProperty\":\"Test\",\"RandomValue\":\"Test\"}", json);

                client.Configuration.AddDataExclusions("Ignore*");
                context = new EventPluginContext(client, new Event());
                context.ContextData.SetException(exception);

                plugin.Run(context);
                error = context.Event.GetError() as IData ?? context.Event.GetSimpleError();
                Assert.NotNull(error);
                Assert.True(error.Data.ContainsKey(Error.KnownDataKeys.ExtraProperties));
                json = error.Data[Error.KnownDataKeys.ExtraProperties] as string;
                Assert.Equal("{\"RandomValue\":\"Test\"}", json);
            }
        }

        [Fact]
        public void CanAddPluginConcurrently() {
            var client = CreateClient();
            foreach (var plugin in client.Configuration.Plugins)
                client.Configuration.RemovePlugin(plugin.Key);

            Assert.Empty(client.Configuration.Plugins);

            Parallel.For(0, 1000, i => {
                client.Configuration.AddPlugin<EnvironmentInfoPlugin>();
            });

            Assert.Single(client.Configuration.Plugins);
            client.Configuration.RemovePlugin<EnvironmentInfoPlugin>();
            Assert.Empty(client.Configuration.Plugins);
        }

        [Fact]
        public void EnvironmentInfo_CanRunInParallel() {
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
        public void EnvironmentInfo_ShouldAddSessionStart() {
            var client = CreateClient();
            var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Session });

            var plugin = new EnvironmentInfoPlugin();
            plugin.Run(context);
            Assert.Single(context.Event.Data);
            Assert.NotNull(context.Event.Data[Event.KnownDataKeys.EnvironmentInfo]);
        }

        [Fact]
        public void CanCancel() {
            var client = CreateClient();
            foreach (var plugin in client.Configuration.Plugins)
                client.Configuration.RemovePlugin(plugin.Key);

            client.Configuration.AddPlugin("cancel", 1, ctx => ctx.Cancel = true);
            client.Configuration.AddPlugin("add-tag", 2, ctx => ctx.Event.Tags.Add("Was Not Canceled"));

            var context = new EventPluginContext(client, new Event());
            EventPluginManager.Run(context);
            Assert.True(context.Cancel);
            Assert.Empty(context.Event.Tags);
        }

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

        [Fact]
        public void PrivateInformation_WillSetIdentity() {
            var client = CreateClient();
            var plugin = new SetEnvironmentUserPlugin();

            var context = new EventPluginContext(client, new Event { Type = Event.KnownTypes.Log, Message = "test" });
            plugin.Run(context);

            var user = context.Event.GetUserIdentity();
            Assert.Equal(Environment.UserName, user.Identity);
        }

        [Fact]
        public void PrivateInformation_WillNotUpdateIdentity() {
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

        [Fact]
        public void LazyLoadAndRemovePlugin() {
            var configuration = new ExceptionlessConfiguration(DependencyResolver.Default);
            foreach (var plugin in configuration.Plugins)
                configuration.RemovePlugin(plugin.Key);

            configuration.AddPlugin<ThrowIfInitializedTestPlugin>();
            configuration.RemovePlugin<ThrowIfInitializedTestPlugin>();
        }

        private class ThrowIfInitializedTestPlugin : IEventPlugin, IDisposable {
            public ThrowIfInitializedTestPlugin() {
                throw new Exception("Plugin shouldn't be constructed");
            }

            public void Run(EventPluginContext context) {}

            public void Dispose() {
                throw new Exception("Plugin shouldn't be created or disposed");
            }
        }

        [Fact]
        public void CanDisposePlugin() {
            var configuration = new ExceptionlessConfiguration(DependencyResolver.Default);
            foreach (var plugin in configuration.Plugins)
                configuration.RemovePlugin(plugin.Key);

            Assert.Equal(0, CounterTestPlugin.ConstructorCount);
            Assert.Equal(0, CounterTestPlugin.RunCount);
            Assert.Equal(0, CounterTestPlugin.DisposeCount);

            configuration.AddPlugin<CounterTestPlugin>();
            configuration.AddPlugin<CounterTestPlugin>();

            for (int i = 0; i < 2; i++) {
                foreach (var pluginRegistration in configuration.Plugins)
                    pluginRegistration.Plugin.Run(new EventPluginContext(CreateClient(), new Event()));
            }

            configuration.RemovePlugin<CounterTestPlugin>();
            configuration.RemovePlugin<CounterTestPlugin>();


            Assert.Equal(1, CounterTestPlugin.ConstructorCount);
            Assert.Equal(2, CounterTestPlugin.RunCount);
            Assert.Equal(1, CounterTestPlugin.DisposeCount);
        }

        public class CounterTestPlugin : IEventPlugin, IDisposable {
            public static byte ConstructorCount = 0;
            public static byte RunCount = 0;
            public static byte DisposeCount = 0;

            public CounterTestPlugin() {
                ConstructorCount++;
            }

            public void Run(EventPluginContext context) {
                RunCount++;
            }

            public void Dispose() {
                DisposeCount++;
            }
        }

        [Fact]
        public void VerifyPriority() {
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());
            foreach (var plugin in config.Plugins)
                config.RemovePlugin(plugin.Key);

            Assert.Empty(config.Plugins);
            config.AddPlugin<EnvironmentInfoPlugin>();
            config.AddPlugin<PluginWithPriority11>();
            config.AddPlugin(new PluginWithNoPriority());
            config.AddPlugin("version", 1, ctx => ctx.Event.SetVersion("1.0.0.0"));
            config.AddPlugin("version2", 2, ctx => ctx.Event.SetVersion("1.0.0.0"));
            config.AddPlugin("version3", 3, ctx => ctx.Event.SetVersion("1.0.0.0"));

            var plugins = config.Plugins.ToArray();
            Assert.Equal(typeof(PluginWithNoPriority), plugins[0].Plugin.GetType());
            Assert.Equal("version", plugins[1].Key);
            Assert.Equal("version2", plugins[2].Key);
            Assert.Equal("version3", plugins[3].Key);
            Assert.Equal(typeof(PluginWithPriority11), plugins[4].Plugin.GetType());
            Assert.Equal(typeof(EnvironmentInfoPlugin), plugins[5].Plugin.GetType());
        }

        [Fact]
        public void ViewPriority() {
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());
            foreach (var plugin in config.Plugins)
                _writer.WriteLine(plugin);
        }

        [Fact]
        public void VerifyDeduplication() {
            var client = CreateClient();
            var errorPlugin = new ErrorPlugin();

            EventPluginContext mergedContext = null;
            using (var duplicateCheckerPlugin = new DuplicateCheckerPlugin(TimeSpan.FromMilliseconds(40))) {
                for (int index = 0; index < 10; index++) {
                    var builder = GetException().ToExceptionless();
                    var context = new EventPluginContext(client, builder.Target, builder.PluginContextData);

                    errorPlugin.Run(context);
                    duplicateCheckerPlugin.Run(context);

                    if (index == 0) {
                        Assert.False(context.Cancel);
                        Assert.Null(context.Event.Count);
                    } else {
                        Assert.True(context.Cancel);
                        if (index == 1)
                            mergedContext = context;
                    }
                }
            }

            Thread.Sleep(100);
            Assert.Equal(9, mergedContext.Event.Count.GetValueOrDefault());
        }

        [Fact]
        public void VerifyDeduplicationPluginWillCallSubmittingHandler() {
            var client = CreateClient();
            foreach (var plugin in client.Configuration.Plugins)
                client.Configuration.RemovePlugin(plugin.Key);
            client.Configuration.AddPlugin(new DuplicateCheckerPlugin(TimeSpan.FromMilliseconds(75)));

            int submittingEventHandlerCalls = 0;
            client.SubmittingEvent += (sender, args) => {
                Interlocked.Increment(ref submittingEventHandlerCalls);
            };

            for (int index = 0; index < 3; index++) {
                client.SubmitLog("test");
                if (index > 0)
                    continue;

                Assert.Equal(1, submittingEventHandlerCalls);
            }

            Thread.Sleep(100);
            Assert.Equal(2, submittingEventHandlerCalls);
        }

        [Fact]
        public void VerifyDeduplicationMultithreaded() {
            var client = CreateClient();
            var errorPlugin = new ErrorPlugin();

            var contexts = new ConcurrentBag<EventPluginContext>();
            using (var duplicateCheckerPlugin = new DuplicateCheckerPlugin(TimeSpan.FromMilliseconds(100))) {
                var result = Parallel.For(0, 10, index => {
                    var builder = GetException().ToExceptionless();
                    var context = new EventPluginContext(client, builder.Target, builder.PluginContextData);
                    contexts.Add(context);

                    errorPlugin.Run(context);
                    duplicateCheckerPlugin.Run(context);
                });

                while (!result.IsCompleted)
                    Thread.Sleep(1);
            }

            Thread.Sleep(150);
            Assert.Equal(1, contexts.Count(c => !c.Cancel));
            Assert.Equal(9, contexts.Count(c => c.Cancel));
            Assert.Equal(9, contexts.Sum(c => c.Event.Count.GetValueOrDefault()));
        }

        [Fact]
        public void VerifyDeduplicationFromFiles() {
            var client = CreateClient();
            var errorPlugin = new ErrorPlugin();

            foreach (var ev in ErrorDataReader.GetEvents()) {
                using (var duplicateCheckerPlugin = new DuplicateCheckerPlugin(TimeSpan.FromMilliseconds(20))) {

                    for (int index = 0; index < 2; index++) {
                        var contextData = new ContextData();
                        var context = new EventPluginContext(client, ev, contextData);

                        errorPlugin.Run(context);
                        duplicateCheckerPlugin.Run(context);

                        if (index == 0) {
                            Assert.False(context.Cancel);
                            Assert.Null(context.Event.Count);
                        } else {
                            Assert.True(context.Cancel);
                            Thread.Sleep(50);
                            Assert.Equal(1, context.Event.Count);
                        }
                    }
                }
            }
        }

        private ExceptionWithOverriddenStackTrace GetExceptionWithOverriddenStackTrace(string message = "Test") {
            try {
                throw new ExceptionWithOverriddenStackTrace(message);
            } catch (ExceptionWithOverriddenStackTrace ex) {
                return ex;
            }
        }

        private Exception GetException(string message = "Test") {
            try {
                throw new Exception(message);
            } catch (Exception ex) {
                return ex;
            }
        }

        private Exception GetNestedSimpleException(string message = "Test") {
            try {
                throw new Exception("nested " + message);
            } catch (Exception ex) {
                return new ApplicationException(message, ex);
            }
        }

        [Fact]
        public void RunBenchmark() {
            var summary = BenchmarkRunner.Run<DeduplicationBenchmarks>();

            foreach (var report in summary.Reports) {
                _writer.WriteLine(report.ToString());

                double benchmarkMedianMilliseconds = report.ResultStatistics != null ? report.ResultStatistics.Median / 1000000 : 0;
                _writer.WriteLine(String.Format("{0} - {1:0.00}ms", report.Benchmark.DisplayInfo, benchmarkMedianMilliseconds));
            }
        }

        public class PluginWithNoPriority : IEventPlugin {
            public void Run(EventPluginContext context) {}
        }

        [Priority(11)]
        public class PluginWithPriority11 : IEventPlugin {
            public void Run(EventPluginContext context) {}
        }

        private enum TestEnum {
            None = 1
        }

        private struct TestStruct {
            public int Id { get; set; }
        }

        public class MyApplicationException : Exception {
            public MyApplicationException(string message) : base(message) {
                SetsDataProperty = Data;
            }

            public string IgnoredProperty { get; set; }

            public string RandomValue { get; set; }

            public IDictionary SetsDataProperty { get; set; }

            public override IDictionary Data { get { return SetsDataProperty; }  }
        }

        [Serializable]
        public class ExceptionWithOverriddenStackTrace : Exception {
            private readonly string _stackTrace = Environment.StackTrace;
            public ExceptionWithOverriddenStackTrace(string message) : base(message) { }
            public override string StackTrace => _stackTrace;
        }
    }
}