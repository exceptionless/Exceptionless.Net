using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Queue;
using Exceptionless.Submission;
using Exceptionless.Tests.Log;
using Exceptionless.Tests.Utility;
using Xunit;

namespace Exceptionless.Tests.Queue {
    public class DefaultEventQueueTests {
        private readonly TestOutputWriter _writer;
        public DefaultEventQueueTests(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        [Fact]
        public async Task ProcessAsync_RequestEntityTooLarge_RetriesWithSmallerBatches() {
            var submissionClient = new MaxBatchSizeSubmissionClient(10);
            using var client = new ExceptionlessClient(c => {
                c.UseLogger(new XunitExceptionlessLog(_writer) { MinimumLogLevel = LogLevel.Trace });
                c.ReadFromAttributes();
                c.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
                c.Resolver.Register<ISubmissionClient>(submissionClient);
            });

            var queue = client.Configuration.Resolver.GetEventQueue();
            for (int i = 0; i < 50; i++)
                queue.Enqueue(new Event { Type = Event.KnownTypes.Log, Message = i.ToString() });

            await queue.ProcessAsync();

            Assert.Equal(50, submissionClient.SubmittedEvents.Count);
            Assert.Equal(50, submissionClient.SubmittedEvents.Distinct().Count());
            Assert.All(submissionClient.SubmittedBatchSizes, size => Assert.InRange(size, 1, 10));
            Assert.False(((DefaultEventQueue)queue).IsQueueProcessingSuspended);
        }

        private class MaxBatchSizeSubmissionClient : ISubmissionClient {
            private readonly int _maxBatchSize;

            public MaxBatchSizeSubmissionClient(int maxBatchSize) {
                _maxBatchSize = maxBatchSize;
            }

            public List<string> SubmittedEvents { get; } = new List<string>();
            public List<int> SubmittedBatchSizes { get; } = new List<int>();

            public Task<SubmissionResponse> PostEventsAsync(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
                var batch = events.ToList();
                if (batch.Count > _maxBatchSize)
                    return Task.FromResult(new SubmissionResponse(413, "Request Entity Too Large"));

                SubmittedBatchSizes.Add(batch.Count);
                SubmittedEvents.AddRange(batch.Select(e => e.Message));
                return Task.FromResult(new SubmissionResponse(202, "Accepted"));
            }

            public Task<SubmissionResponse> PostUserDescriptionAsync(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
                return Task.FromResult(new SubmissionResponse(200, "OK"));
            }

            public Task<SettingsResponse> GetSettingsAsync(ExceptionlessConfiguration config, int version, IJsonSerializer serializer) {
                return Task.FromResult(new SettingsResponse(true));
            }

            public Task SendHeartbeatAsync(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config) {
                return Task.CompletedTask;
            }
        }
    }
}
