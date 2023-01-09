using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Submission;

namespace Exceptionless.Tests.Utility {
    public class InMemorySubmissionClient : ISubmissionClient {
        public InMemorySubmissionClient() {
            Events = new List<Event>();
        }

        public List<Event> Events { get; private set; } 

        public Task<SubmissionResponse> PostEventsAsync(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            var data = events.ToList();
            data.ForEach(e => {
                if (e.Date == DateTimeOffset.MinValue)
                    e.Date = DateTimeOffset.Now;

                if (String.IsNullOrEmpty(e.Type))
                    e.Type = Event.KnownTypes.Log;
            });

            Events.AddRange(data);
            return Task.FromResult(new SubmissionResponse(202, "Accepted"));
        }

        public Task<SubmissionResponse> PostUserDescriptionAsync(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            var ev = Events.FirstOrDefault(e => e.ReferenceId == referenceId);
            if (ev == null)
                return Task.FromResult(new SubmissionResponse(404, "Not Found"));

            ev.Data[Event.KnownDataKeys.UserDescription] = description;

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
