using System;
using System.Collections.Generic;
using Exceptionless.Dependency;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Plugins;
using Exceptionless.Submission;
using Xunit;

namespace Exceptionless.Tests {
    public class ExceptionlessClientTests {
        private ExceptionlessClient CreateClient() {
            return new ExceptionlessClient(c => {
                c.UseTraceLogger();
                c.ReadFromAttributes();
                c.UserAgent = "testclient/1.0.0.0";
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
            Assert.Equal(ev.Target.Data.Count, 3);

            ev.Target.Data.Clear();
            Assert.Equal(ev.Target.Data.Count, 0);

            // The last one in wins.
            ev.AddObject(new Person { Name = "Eric" }, "Blake");
            ev.AddObject(new Person { Name = "Blake" }, "Blake");
            Assert.Equal(ev.Target.Data.Count, 1);

            var person = ev.Target.Data["Blake"].ToString();
            Assert.True(person.Contains("Blake"));
        }

        [Fact]
        public void CanFireOnSubmittingEvent() {
            var client = CreateClient();
            var ev = new Event { Message = "Unit Test" };
            var list = new List<EventSubmittingEventArgs>();

            client.SubmittingEvent += (sender, e) => {
                list.Add(e);
            }; 

            new EventBuilder(ev, client).Submit();
            Assert.Equal(1, list.Count);

            new EventBuilder(ev, client, new ContextData()).Submit();
            Assert.Equal(2, list.Count);  
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

        [Fact (Skip = "This test shows off throwing a stack overflow exception: Issue #26")]
        public void WillThrowStackOverflowExceptionDuringOnSubmitting() {
            var client = CreateClient();
            var list = new List<EventSubmittingEventArgs>();

            client.SubmittingEvent += (sender, e) => {
                list.Add(e);
                if (e.IsUnhandledError) {
                    new EventBuilder(e.Event, e.Client, e.PluginContextData).AddTags("Unhandled").Submit();
                    e.Cancel = true;
                }
            };

            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            new Exception("Test").ToExceptionless(contextData, client).Submit();
            Assert.Equal(2, list.Count);  
        }
        
        private class Person {
            public string Name { get; set; }
        }

        public class MySubmissionClient : ISubmissionClient {
            public SubmissionResponse PostEvents(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
                throw new NotImplementedException();
            }

            public SubmissionResponse PostUserDescription(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
                throw new NotImplementedException();
            }

            public SettingsResponse GetSettings(ExceptionlessConfiguration config, IJsonSerializer serializer) {
                throw new NotImplementedException();
            }

            public void SendHeartbeat(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config) {
                throw new NotImplementedException();
            }
        }
    }
}
