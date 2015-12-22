using System;

namespace Exceptionless.Services {
    // would have in memory, asp.net session state (Mvc, WebForms, fall back to in memory if session is disabled)
    // redis (Exceptionless.Redis), foundatio (Exceptionless.Foundatio) cache client
    public interface ISessionManager {
        string GetSessionId(string identity);
        string StartSession(string identity);
        void EndSession(string sessionId);
    }
}
