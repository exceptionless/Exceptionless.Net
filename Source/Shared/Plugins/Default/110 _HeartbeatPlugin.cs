using System;
using System.Collections.Generic;
using System.Threading;
using Exceptionless;
using Exceptionless.Models.Data;

namespace Exceptionless.Plugins.Default {
    [Priority(110)]
    public class HeartbeatPlugin : IEventPlugin, IDisposable {
        private readonly Dictionary<string, SessionHeartbeat> _sessionHeartbeats = new Dictionary<string, SessionHeartbeat>();
        private readonly object _lock = new object();

        public void Run(EventPluginContext context) {
            if (context.Event.IsSessionHeartbeat())
                return;
            
            var sessionIdentifier = context.Event.SessionId;
            if (String.IsNullOrEmpty(sessionIdentifier))
                return;

            lock (_lock) {
                if (!_sessionHeartbeats.ContainsKey(sessionIdentifier)) {
                    _sessionHeartbeats.Add(sessionIdentifier, new SessionHeartbeat(sessionIdentifier, context.Client));
                } else if (context.Event.IsSessionEnd()) {
                    _sessionHeartbeats[sessionIdentifier].Dispose();
                    _sessionHeartbeats.Remove(sessionIdentifier);
                } else {
                    _sessionHeartbeats[sessionIdentifier].DelayNext();
                }
            }
        }

        public void Dispose() {
            lock (_lock) {
                foreach (var kvp in _sessionHeartbeats)
                    kvp.Value.Dispose();
            }

            _sessionHeartbeats.Clear();
        }
    }

    public class SessionHeartbeat : IDisposable {
        private readonly Timer _timer;
        private readonly int _interval = 30 * 1000;
        private readonly ExceptionlessClient _client;

        public SessionHeartbeat(string sessionId, ExceptionlessClient client) {
            SessionId = sessionId;
            _client = client;
            _timer = new Timer(SendHeartbeat, null, _interval, _interval);
        }
        
        public string SessionId { get; set; }
        
        public void DelayNext() {
            _timer.Change(_interval, _interval);
        }

        private void SendHeartbeat(object state) {
            _client.SubmitSessionHeartbeat(SessionId);
        }

        public void Dispose() {
            _timer?.Dispose();
        }
    }
}