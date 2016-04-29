using System;
using System.Collections.Generic;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Submission;

namespace Exceptionless.SampleConsole {
    public class InMemorySubmissionClient : ISubmissionClient {
        private readonly Dictionary<string, object> _eventContainer = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _userDescriptionContainer = new Dictionary<string, object>();
        private readonly Dictionary<string, DateTime> _heartbeatContainer = new Dictionary<string, DateTime>();

        public SubmissionResponse PostEvents(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            foreach (Event e in events) {
                string data = serializer.Serialize(e);
                string referenceId = !string.IsNullOrWhiteSpace(e.ReferenceId) ? e.ReferenceId : Guid.NewGuid().ToString("D");
                _eventContainer[referenceId] = data;
            }

            return new SubmissionResponse(200);
        }

        public SubmissionResponse PostUserDescription(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer) {
            string data = serializer.Serialize(description);
            _userDescriptionContainer[referenceId] = data;
            return new SubmissionResponse(200);
        }

        public SettingsResponse GetSettings(ExceptionlessConfiguration config, IJsonSerializer serializer) {
            return new SettingsResponse(true);
        }

        public void SendHeartbeat(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config) {
            _heartbeatContainer[sessionIdOrUserId] = DateTime.UtcNow;
        }
    }
}