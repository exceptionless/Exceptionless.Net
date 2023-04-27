using System.Collections.Generic;
using System.Threading.Tasks;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless.Submission {
    public interface ISubmissionClient {
        Task<SubmissionResponse> PostEventsAsync(IEnumerable<Event> events, ExceptionlessConfiguration config, IJsonSerializer serializer);
        Task<SubmissionResponse> PostUserDescriptionAsync(string referenceId, UserDescription description, ExceptionlessConfiguration config, IJsonSerializer serializer);
        Task<SettingsResponse> GetSettingsAsync(ExceptionlessConfiguration config, int version, IJsonSerializer serializer);
        Task SendHeartbeatAsync(string sessionIdOrUserId, bool closeSession, ExceptionlessConfiguration config);
    }
}