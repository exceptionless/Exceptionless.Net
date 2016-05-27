using System;
using System.Collections.Generic;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless.Submission {
    public interface ISubmissionClient {
        SubmissionResponse PostEvents(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer);
        SubmissionResponse PostUserDescription(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer);
        SettingsResponse GetSettings(ExceptionlessConfiguration config, int version, IJsonSerializer serializer);
        void SendHeartbeat(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config);
    }
}