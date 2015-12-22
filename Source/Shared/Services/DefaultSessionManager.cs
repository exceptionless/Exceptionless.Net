using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptionless.Services {
    public class DefaultSessionManager : ISessionManager {
        private readonly Dictionary<string, string> _sessionMap = new Dictionary<string, string>();
        private readonly object _lockObject = new object();

        public string GetSessionId(string identity) {
            lock (_lockObject) {
                if (_sessionMap.ContainsKey(identity))
                    return _sessionMap[identity];
            }

            return null;
        }

        public string StartSession(string identity) {
            lock (_lockObject) {
                string sessionId = Guid.NewGuid().ToString("N");
                lock (_lockObject)
                    _sessionMap[identity] = sessionId;

                return sessionId;
            }
        }

        public void EndSession(string sessionId) {
            lock (_lockObject) {
                var entry = _sessionMap.FirstOrDefault(kvp => kvp.Value == sessionId);
                if (!String.IsNullOrEmpty(entry.Key))
                    _sessionMap.Remove(entry.Key);
            }
        }
    }
}