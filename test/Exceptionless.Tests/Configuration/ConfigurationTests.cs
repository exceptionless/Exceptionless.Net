using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Configuration;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Storage;
using Exceptionless.Submission;
using Exceptionless.Tests.Utility;
using Moq;
using Xunit;
using Xunit.Abstractions;

[assembly: Exceptionless("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw", ServerUrl = "http://localhost:45000")]
[assembly: ExceptionlessSetting("testing", "configuration")]
namespace Exceptionless.Tests.Configuration {
    public class ConfigurationTests {
        private readonly TestOutputWriter _writer;
        public ConfigurationTests(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        [Fact]
        public void CanConfigureApiKeyFromClientConstructor() {
            var client = new ExceptionlessClient("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw");
            Assert.NotNull(client);
            Assert.Equal("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw", client.Configuration.ApiKey);
            Assert.True(client.Configuration.IncludePrivateInformation);
            Assert.True(client.Configuration.IncludeUserName);
            Assert.True(client.Configuration.IncludeMachineName);
            Assert.True(client.Configuration.IncludeIpAddress);
            Assert.True(client.Configuration.IncludeCookies);
            Assert.True(client.Configuration.IncludePostData);
            Assert.True(client.Configuration.IncludeQueryString);

            client.Configuration.IncludePrivateInformation = false;
            Assert.False(client.Configuration.IncludePrivateInformation);
            Assert.False(client.Configuration.IncludeUserName);
            Assert.False(client.Configuration.IncludeMachineName);
            Assert.False(client.Configuration.IncludeIpAddress);
            Assert.False(client.Configuration.IncludeCookies);
            Assert.False(client.Configuration.IncludePostData);
            Assert.False(client.Configuration.IncludeQueryString);

            client.Configuration.IncludeMachineName = true;
            Assert.False(client.Configuration.IncludePrivateInformation);
            Assert.False(client.Configuration.IncludeUserName);
            Assert.True(client.Configuration.IncludeMachineName);
            Assert.False(client.Configuration.IncludeIpAddress);
            Assert.False(client.Configuration.IncludeCookies);
            Assert.False(client.Configuration.IncludePostData);
            Assert.False(client.Configuration.IncludeQueryString);
        }

        [Fact]
        public void CanConfigureClientUsingActionMethod() {
            const string version = "1.2.3";
            
            var client = new ExceptionlessClient(c => {
                c.ApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw";
                c.ServerUrl = "http://localhost:45000";
                c.SetVersion(version);
                c.IncludeUserName = false;
            });

            Assert.Equal("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw", client.Configuration.ApiKey);
            Assert.Equal("http://localhost:45000", client.Configuration.ServerUrl);
            Assert.Equal(version, client.Configuration.DefaultData[Event.KnownDataKeys.Version].ToString());

            Assert.True(client.Configuration.IncludePrivateInformation);
            Assert.False(client.Configuration.IncludeUserName);
            Assert.True(client.Configuration.IncludeMachineName);
            Assert.True(client.Configuration.IncludeIpAddress);
            Assert.True(client.Configuration.IncludeCookies);
            Assert.True(client.Configuration.IncludePostData);
            Assert.True(client.Configuration.IncludeQueryString);
        }

        [Fact]
        public void CanReadFromAttributes() {
            var config = new ExceptionlessConfiguration(DependencyResolver.CreateDefault());
            Assert.Null(config.ApiKey);
            Assert.Equal("https://collector.exceptionless.io", config.ServerUrl);
            Assert.Empty(config.Settings);

            config.ReadFromAttributes(typeof(ConfigurationTests).GetTypeInfo().Assembly);
            Assert.Equal("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw", config.ApiKey);
            Assert.Equal("http://localhost:45000", config.ServerUrl);
            Assert.Single(config.Settings);
            Assert.Equal("configuration", config.Settings["testing"]);
        }

        [Fact]
        public void WillLockConfig() {
            var client = new ExceptionlessClient();
            client.Configuration.Resolver.Register<ISubmissionClient, InMemorySubmissionClient>();
            client.Configuration.ApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw";
            client.SubmitEvent(new Event());
            Assert.Throws<ArgumentException>(() => client.Configuration.ApiKey = "blah");
            Assert.Throws<ArgumentException>(() => client.Configuration.ServerUrl = "blah");
        }

        [Fact]
        public void CanUpdateSettingsFromServer() {
            var config = new ExceptionlessConfiguration(DependencyResolver.Default) {
                ApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw",
                Settings = {
                    ["LocalSetting"] = "1",
                    ["LocalSettingToOverride"] = "1"
                }
            };

            var submissionClient = new Mock<ISubmissionClient>();
            submissionClient.Setup(m => m.PostEvents(It.IsAny<IEnumerable<Event>>(), config, It.IsAny<IJsonSerializer>()))
                .Callback(() => SettingsManager.CheckVersion(1, config))
                .Returns(() => new SubmissionResponse(202, "Accepted"));
            submissionClient.Setup(m => m.GetSettings(config, 0, It.IsAny<IJsonSerializer>()))
                .Returns(() => new SettingsResponse(true, new SettingsDictionary { { "Test", "Test" }, { "LocalSettingToOverride", "2" } }, 1));

            config.Resolver.Register<ISubmissionClient>(submissionClient.Object);
            var client = new ExceptionlessClient(config);

            Assert.Equal(2, client.Configuration.Settings.Count);
            Assert.False(client.Configuration.Settings.ContainsKey("Test"));
            Assert.Equal("1", client.Configuration.Settings["LocalSettingToOverride"]);
            client.SubmitEvent(new Event { Type = "Log", Message = "Test" });
            client.ProcessQueue();
            Assert.True(client.Configuration.Settings.ContainsKey("Test"));
            Assert.Equal("2", client.Configuration.Settings["LocalSettingToOverride"]);
            Assert.Equal(3, client.Configuration.Settings.Count);

            var storage = config.Resolver.GetFileStorage() as InMemoryObjectStorage;
            Assert.NotNull(storage);
            Assert.NotNull(config.GetQueueName());
            Assert.True(storage.Exists(Path.Combine(config.GetQueueName(), "server-settings.json")));

            config.Settings.Clear();
            config.ApplySavedServerSettings();
            Assert.True(client.Configuration.Settings.ContainsKey("Test"));
            Assert.Equal("2", client.Configuration.Settings["LocalSettingToOverride"]);
            Assert.Equal(2, client.Configuration.Settings.Count);
        }

        [Fact]
        public void CanGetSettingsMultithreaded() {
            var settings = new SettingsDictionary();
            var result = Parallel.For(0, 20, index => {
                for (int i = 0; i < 10; i++) {
                    string key = $"setting-{i}";
                    if (!settings.ContainsKey(key))
                        settings.Add(key, (index * i).ToString());
                    else
                        settings[key] = (index * i).ToString();
                }
            });

            while (!result.IsCompleted)
                Thread.Yield();
        }

        [Fact]
        public void CanGetLogSettingsMultithreaded() {
            var settings = new SettingsDictionary {
                { "@@log:*", "Info" },
                { "@@log:Source1", "Trace" },
                { "@@log:Source2", "Debug" },
                { "@@log:Source3", "Info" },
                { "@@log:Source4", "Info" }
            };

            var result = Parallel.For(0, 100, index => {
                var level = settings.GetMinLogLevel("Source1");
                _writer.WriteLine("Source1 log level: {0}", level);
            });

            while (!result.IsCompleted)
                Thread.Yield();
        }
        
        [Fact]
        public void LogLevels_GetMinLogLevel_Settings_Order() {
            var settings = new SettingsDictionary {{"@@log:", "Info"}, {"@@log:*", "Debug"}};
            Assert.Equal(LogLevel.Info, settings.GetMinLogLevel(null));
            Assert.Equal(LogLevel.Info, settings.GetMinLogLevel(String.Empty));
            Assert.Equal(LogLevel.Debug, settings.GetMinLogLevel("*"));
            
            settings = new SettingsDictionary {{"@@log:*", "Debug"}, {"@@log:", "Info"}};
            Assert.Equal(LogLevel.Info, settings.GetMinLogLevel(String.Empty));
            Assert.Equal(LogLevel.Debug, settings.GetMinLogLevel("*"));
            
            settings = new SettingsDictionary {
                { "@@log:*", "Fatal" }, 
                { "@@log:", "Debug" }, 
                { "@@log:abc*", "Off" }, 
                { "@@log:abc.de*", "Debug" }, 
                { "@@log:abc.def*", "Info" }, 
                { "@@log:abc.def.ghi", "Trace" }
            };
            
            Assert.Equal(LogLevel.Fatal, settings.GetMinLogLevel("other"));
            Assert.Equal(LogLevel.Debug, settings.GetMinLogLevel(null));
            Assert.Equal(LogLevel.Debug, settings.GetMinLogLevel(String.Empty));
            Assert.Equal(LogLevel.Off, settings.GetMinLogLevel("abc"));
            Assert.Equal(LogLevel.Info, settings.GetMinLogLevel("abc.def"));
            Assert.Equal(LogLevel.Trace, settings.GetMinLogLevel("abc.def.ghi"));
            
            settings = new SettingsDictionary {
                { "@@log:abc.def.ghi", "Trace" },
                { "@@log:abc.def*", "Info" }, 
                { "@@log:abc*", "Off" }
            };
            
            Assert.Equal(LogLevel.Off, settings.GetMinLogLevel("abc"));
            Assert.Equal(LogLevel.Info, settings.GetMinLogLevel("abc.def"));
            Assert.Equal(LogLevel.Trace, settings.GetMinLogLevel("abc.def.ghi"));
        }
    }
}
