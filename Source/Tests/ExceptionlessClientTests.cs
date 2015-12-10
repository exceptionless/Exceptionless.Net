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
                c.UseDebugLogger();
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

        //[Fact]
        //public async Task CanSubmitSimpleEvent() {
        //    var container = AppBuilder.CreateContainer();
        //    using (WebApp.Start(Settings.Current.BaseURL, app => AppBuilder.BuildWithContainer(app, container))) {
        //        var queue = container.GetInstance<IQueue<EventPost>>();
        //        Assert.NotNull(queue);
        //        Assert.Equal(0, queue.GetQueueCount());

        //        var statsCounter = container.GetInstance<IMetricsClient>() as InMemoryMetricsClient;
        //        Assert.NotNull(statsCounter);

        //        EnsureSampleData(container);

        //        var client = CreateClient();
        //        client.SubmitEvent(new Event { Message = "Test" });

        //        var storage = client.Configuration.Resolver.GetFileStorage() as InMemoryObjectStorage;
        //        Assert.NotNull(storage);
        //        Assert.Equal(1, storage.GetObjectList().Count());

        //        Assert.True(statsCounter.WaitForCounter(MetricNames.EventsProcessed, work: client.ProcessQueue));

        //        Assert.Equal(0, queue.GetQueueCount());
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.PostsSubmitted));
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.PostsQueued));
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.PostsParsed));
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.PostsDequeued));
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.EventsProcessed));
        //    }
        //}

        //[Fact]
        //public async Task CanSubmitSimpleException() {
        //    var container = AppBuilder.CreateContainer();
        //    using (WebApp.Start(Settings.Current.BaseURL, app => AppBuilder.BuildWithContainer(app, container))) {
        //        var queue = container.GetInstance<IQueue<EventPost>>();
        //        Assert.NotNull(queue);
        //        Assert.Equal(0, queue.GetQueueCount());

        //        var statsCounter = container.GetInstance<IMetricsClient>() as InMemoryMetricsClient;
        //        Assert.NotNull(statsCounter);

        //        EnsureSampleData(container);

        //        var client = CreateClient();
        //        var clientQueue = client.Configuration.Resolver.GetEventQueue();
        //        // make sure the queue isn't processed until we are ready for it
        //        clientQueue.SuspendProcessing(TimeSpan.FromSeconds(10));
        //        try {
        //            throw new Exception("Simple Exception");
        //        } catch (Exception ex) {
        //            client.SubmitException(ex);
        //        }

        //        var storage = client.Configuration.Resolver.GetFileStorage() as InMemoryObjectStorage; 
        //        Assert.NotNull(storage);
        //        Assert.Equal(1, storage.GetObjectList().Count());

        //        Assert.True(statsCounter.WaitForCounter(MetricNames.EventsProcessed, work: client.ProcessQueue));

        //        Assert.Equal(0, queue.GetQueueCount());
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.PostsSubmitted));
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.PostsQueued));
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.PostsParsed));
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.PostsDequeued));
        //        Assert.Equal(1, statsCounter.GetCount(MetricNames.EventsProcessed));
        //    }
        //}

        //private void EnsureSampleData(Container container) {
        //    var dataHelper = container.GetInstance<DataHelper>();
        //    var userRepository = container.GetInstance<IUserRepository>();
        //    var user = userRepository.GetByEmailAddress("test@exceptionless.io");
        //    if (user == null)
        //        user = userRepository.Add(new User { FullName = "Test User", EmailAddress = "test@exceptionless.io", VerifyEmailAddressToken = Guid.NewGuid().ToString(), VerifyEmailAddressTokenExpiration = DateTime.MaxValue });

        //    dataHelper.CreateTestOrganizationAndProject(user.Id);
        //}

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
        }
    }
}
