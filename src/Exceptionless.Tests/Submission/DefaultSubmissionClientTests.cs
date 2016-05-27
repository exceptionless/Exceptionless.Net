﻿//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using Exceptionless;
//using Exceptionless.Api;
//using Exceptionless.Core;
//using Exceptionless.Core.AppStats;
//using Exceptionless.Core.Extensions;
//using Exceptionless.Core.Repositories;
//using Exceptionless.Core.Utility;
//using Exceptionless.Models;
//using Exceptionless.Models.Data;
//using Exceptionless.Serializer;
//using Exceptionless.Submission;
//using Microsoft.Owin.Hosting;
//using Nest;
//using SimpleInjector;
//using Xunit;
//using Exceptionless.Core.Queues.Models;
//using Foundatio.Metrics;
//using Foundatio.Queues;

//namespace Exceptionless.Tests.Submission {
//    public class DefaultSubmissionClientTests {
//        private ExceptionlessClient GetClient() {
//            return new ExceptionlessClient(c => {
//                c.ApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw";
//                c.ServerUrl = Settings.Current.BaseURL;
//                c.EnableSSL = false;
//                c.UseDebugLogger();
//            });
//        }

//        [Fact]
//        public void PostEvents() {
//            var container = AppBuilder.CreateContainer();
//            using (WebApp.Start(Settings.Current.BaseURL, app => AppBuilder.BuildWithContainer(app, container))) {
//                EnsureSampleData(container);

//                var events = new List<Event> { new Event { Message = "Testing" } };
//                var configuration = GetClient().Configuration;
//                var serializer = new DefaultJsonSerializer();

//                var client = new DefaultSubmissionClient();
//                var response = client.PostEvents(events, configuration, serializer);
//                Assert.True(response.Success, response.Message);
//                Assert.Null(response.Message);
//            }
//        }

//        [Fact(Skip="Flakey, need a better way to test this")]
//        public async Task PostUserDescription() {
//            var container = AppBuilder.CreateContainer();
//            using (WebApp.Start(Settings.Current.BaseURL, app => AppBuilder.BuildWithContainer(app, container))) {
//                var repository = container.GetInstance<IEventRepository>();
//                repository.RemoveAll();

//                const string referenceId = "fda94ff32921425ebb08b73df1d1d34c";
//                const string badReferenceId = "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz";

//                var statsCounter = container.GetInstance<IMetricsClient>() as InMemoryMetricsClient;
//                var descQueue = container.GetInstance<IQueue<EventUserDescription>>() as InMemoryQueue<EventUserDescription>;

//                Assert.NotNull(statsCounter);

//                EnsureSampleData(container);
                
//                var ev = new Event { Message = "Testing", ReferenceId = referenceId };
//                ev.Data.Add("First Name", "Eric");
//                ev.Data.Add("IsVerified", true);
//                ev.Data.Add("Age", Int32.MaxValue);
//                ev.Data.Add(" Birthday ", DateTime.MinValue);
//                ev.Data.Add("@excluded", DateTime.MinValue);
//                ev.Data.Add("Address", new { State = "Texas" });

//                var events = new List<Event> { ev };
//                var configuration = GetClient().Configuration;
//                var serializer = new DefaultJsonSerializer();

//                var client = new DefaultSubmissionClient();
//                var description = new UserDescription { EmailAddress = "test@noreply.com", Description = "Some description." };
//                Debug.WriteLine("Before Submit Description");
//                statsCounter.DisplayStats();
//                Assert.True(statsCounter.WaitForCounter(MetricNames.EventsUserDescriptionErrors, work: () => {
//                    var response = client.PostUserDescription(referenceId, description, configuration, serializer);
//                    Debug.WriteLine("After Submit Description");
//                    Assert.True(response.Success, response.Message);
//                    Assert.Null(response.Message);
//                }));
//                statsCounter.DisplayStats();
//                Debug.WriteLine(descQueue.GetQueueCount());

//                Debug.WriteLine("Before Post Event");
//                Assert.True(statsCounter.WaitForCounter(MetricNames.EventsProcessed, work: () => {
//                    var response = client.PostEvents(events, configuration, serializer);
//                    Debug.WriteLine("After Post Event");
//                    Assert.True(response.Success, response.Message);
//                    Assert.Null(response.Message);
//                }));
//                statsCounter.DisplayStats();
//                if (statsCounter.GetCount(MetricNames.EventsUserDescriptionProcessed) == 0)
//                    Assert.True(statsCounter.WaitForCounter(MetricNames.EventsUserDescriptionProcessed));

//                container.GetInstance<IElasticClient>().Refresh();
//                ev = repository.GetByReferenceId("537650f3b77efe23a47914f4", referenceId).FirstOrDefault();
//                Assert.NotNull(ev);
//                Assert.NotNull(ev.GetUserDescription());
//                Assert.Equal(description.ToJson(), ev.GetUserDescription().ToJson());

//                Assert.InRange(statsCounter.GetCount(MetricNames.EventsUserDescriptionErrors), 1, 5);
//                Assert.True(statsCounter.WaitForCounter(MetricNames.EventsUserDescriptionErrors, work: () => {
//                    var response = client.PostUserDescription(badReferenceId, description, configuration, serializer);
//                    Assert.True(response.Success, response.Message);
//                    Assert.Null(response.Message);
//                }));
//                statsCounter.DisplayStats();

//                Assert.InRange(statsCounter.GetCount(MetricNames.EventsUserDescriptionErrors), 2, 10);
//            }
//        }

//        [Fact]
//        public void GetSettings() {
//            var container = AppBuilder.CreateContainer();
//            using (WebApp.Start(Settings.Current.BaseURL, app => AppBuilder.BuildWithContainer(app, container))) {
//                EnsureSampleData(container);

//                var configuration = GetClient().Configuration;
//                var serializer = new DefaultJsonSerializer();

//                var client = new DefaultSubmissionClient();
//                var response = client.GetSettings(configuration, serializer);
//                Assert.True(response.Success, response.Message);
//                Assert.NotEqual(-1, response.SettingsVersion);
//                Assert.NotNull(response.Settings);
//                Assert.Null(response.Message);
//            }
//        }

//        private void EnsureSampleData(Container container) {
//            var dataHelper = container.GetInstance<DataHelper>();
//            var userRepository = container.GetInstance<IUserRepository>();
//            var user = userRepository.GetByEmailAddress("test@exceptionless.io");
//            if (user == null)
//                user = userRepository.Add(new User { FullName = "Test User", EmailAddress = "test@exceptionless.io", VerifyEmailAddressToken = Guid.NewGuid().ToString(), VerifyEmailAddressTokenExpiration = DateTime.MaxValue });
//            dataHelper.CreateTestOrganizationAndProject(user.Id);
//        }
//    }
//}