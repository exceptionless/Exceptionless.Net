using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Plugins;
using Exceptionless.Storage;
using Exceptionless.Submission;
using Exceptionless.Tests.Log;
using Exceptionless.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests {
    public class ExceptionlessClientTests {
        private readonly TestOutputWriter _writer;
        public ExceptionlessClientTests(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        private ExceptionlessClient CreateClient() {
            return new ExceptionlessClient(c => {
                c.UseLogger(new XunitExceptionlessLog(_writer) { MinimumLogLevel = LogLevel.Trace });
                c.ReadFromAttributes();
                c.UserAgent = "testclient/1.0.0.0";

                // Disable updating settings.
                c.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
            });
        }

        [Fact]
        public void CanAddMultipleDataObjectsToEvent() {
            var client = CreateClient();
            var ev = client.CreateLog("Test");
            Assert.Equal(ev.Target.Type, Event.KnownTypes.Log);
            ev.AddObject(new Person { Name = "Blake" });
            ev.AddObject(new Person { Name = "Eric" });
            ev.AddObject(new Person { Name = "Ryan" });
            Assert.Equal(3, ev.Target.Data.Count);

            ev.Target.Data.Clear();
            Assert.Empty(ev.Target.Data);

            // The last one in wins.
            ev.AddObject(new Person { Name = "Eric" }, "Blake");
            ev.AddObject(new Person { Name = "Blake" }, "Blake");
            Assert.Single(ev.Target.Data);

            string person = ev.Target.Data["Blake"].ToString();
            Assert.Contains("Blake", person);
        }

        [Fact]
        public void CanFireOnSubmittingEvent() {
            var client = CreateClient();
            var ev = new Event { Message = "Unit Test" };
            var submittingEventArgs = new List<EventSubmittingEventArgs>();

            client.SubmittingEvent += (sender, e) => {
                submittingEventArgs.Add(e);
            }; 

            new EventBuilder(ev, client).Submit();
            Assert.Single(submittingEventArgs);

            new EventBuilder(ev, client, new ContextData()).Submit();
            Assert.Equal(2, submittingEventArgs.Count);  
        }

        [Fact]
        public void CanCallStartupWithCustomSubmissionClient() {
            var client = CreateClient();
            Assert.True(client.Configuration.Resolver.HasRegistration<ISubmissionClient>());
            Assert.True(client.Configuration.Resolver.HasDefaultRegistration<ISubmissionClient, DefaultSubmissionClient>());
            
            client.Configuration.Resolver.Register<ISubmissionClient, MySubmissionClient>();
            Assert.False(client.Configuration.Resolver.HasDefaultRegistration<ISubmissionClient, DefaultSubmissionClient>());
            Assert.True(client.Configuration.Resolver.Resolve<ISubmissionClient>() is MySubmissionClient);

            client.Startup();
            Assert.True(client.Configuration.Resolver.Resolve<ISubmissionClient>() is MySubmissionClient);
            Assert.False(client.Configuration.Resolver.HasDefaultRegistration<ISubmissionClient, DefaultSubmissionClient>());
            client.Shutdown();
        }

        [Fact]
        public void WillNotThrowStackOverflowExceptionDuringOnSubmitting() {
            var client = CreateClient();
            var submittingEventArgs = new List<EventSubmittingEventArgs>();

            client.SubmittingEvent += (sender, e) => {
                submittingEventArgs.Add(e);
                if (e.IsUnhandledError) {
                    new EventBuilder(e.Event, e.Client, e.PluginContextData).AddTags("Unhandled").Submit();
                    e.Cancel = true;
                }
            };

            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            new Exception("Test").ToExceptionless(contextData, client).Submit();
            Assert.Single(submittingEventArgs);  
        }

        [Fact]
        public void CanSubmitManyMessages() {
            var client = CreateClient();
            client.Configuration.Resolver.Register<ISubmissionClient, MySubmissionClient>();
            client.Startup();

            var submissionClient = client.Configuration.Resolver.Resolve<ISubmissionClient>() as MySubmissionClient;
            Assert.NotNull(submissionClient);
            Assert.Equal(0, submissionClient.SubmittedEvents);

            using (var storage = client.Configuration.Resolver.Resolve<IObjectStorage>() as InMemoryObjectStorage) {
                Assert.NotNull(storage);
                Assert.Equal(0, storage.Count);

                const int iterations = 200;
                for (int i = 1; i <= iterations; i++) {
                    _writer.WriteLine($"---- {i} ----");
                    client.CreateLog(typeof(ExceptionlessClientTests).FullName, i.ToString(), LogLevel.Warn)
                        .AddTags("Test")
                        .SetUserIdentity(new UserInfo { Identity = "00001", Name = "test" })
                        .Submit();

                    Assert.InRange(storage.Count, i, i + 1);
                }

                // Count could be higher due to persisted dictionaries via settings manager / other plugins
                Assert.InRange(storage.Count, iterations, iterations + 1);

                client.ProcessQueue();
                Assert.Equal(iterations, submissionClient.SubmittedEvents);

                client.Shutdown();
            }
        }

        private class Person {
            public string Name { get; set; }
        }

        public class MySubmissionClient : ISubmissionClient {
            public int SubmittedEvents { get; private set; }

            public SubmissionResponse PostEvents(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
                SubmittedEvents += events.Count();
                return new SubmissionResponse(202);
            }

            public SubmissionResponse PostUserDescription(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
                return new SubmissionResponse(202);
            }

            public SettingsResponse GetSettings(ExceptionlessConfiguration config, int version, IJsonSerializer serializer) {
                return new SettingsResponse(false);
            }

            public void SendHeartbeat(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config) { }
        }
    }
}
