using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Submission;

namespace Exceptionless.SampleWeb {
    public class InMemorySubmissionClient : ISubmissionClient {
        private readonly Dictionary<string, object> _eventContainer = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _userDescriptionContainer = new Dictionary<string, object>();
        private readonly Dictionary<string, DateTime> _heartbeatContainer = new Dictionary<string, DateTime>();

        public Task<SubmissionResponse> PostEventsAsync(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            foreach (var e in events) {
                string data = serializer.Serialize(e);
                string referenceId = !String.IsNullOrWhiteSpace(e.ReferenceId) ? e.ReferenceId : Guid.NewGuid().ToString("D");
                _eventContainer[referenceId] = data;
            }

            return Task.FromResult(new SubmissionResponse(200));
        }

        public Task<SubmissionResponse> PostUserDescriptionAsync(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            string data = serializer.Serialize(description);
            _userDescriptionContainer[referenceId] = data;
            return Task.FromResult(new SubmissionResponse(200));
        }

        public Task<SettingsResponse> GetSettingsAsync(ExceptionlessConfiguration config, int version, IJsonSerializer serializer) {
            return Task.FromResult(new SettingsResponse(true));
        }

        public Task SendHeartbeatAsync(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config) {
            _heartbeatContainer[sessionIdOrUserId] = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }
}