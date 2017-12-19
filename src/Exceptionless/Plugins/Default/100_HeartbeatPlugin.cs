using System;
using System.Threading;
using Exceptionless.Dependency;

namespace Exceptionless.Plugins.Default {
    [Priority(100)]
    public class HeartbeatPlugin : IEventPlugin, IDisposable {
        private SessionHeartbeat _heartbeat;
        private readonly TimeSpan _interval;

        /// <summary>
        /// Controls whether session heartbeats are sent.
        /// </summary>
        /// <param name="interval">The interval at which heartbeats are sent after the last sent event. The default is 1 minutes.</param>
        public HeartbeatPlugin(TimeSpan? interval = null) {
            _interval = interval.HasValue && interval.Value.Seconds >= 30 ? interval.Value : TimeSpan.FromMinutes(1);
        }

        public void Run(EventPluginContext context) {
            if (String.IsNullOrEmpty(context.Client.Configuration.CurrentSessionIdentifier)) {
                var user = context.Event.GetUserIdentity(context.Client.Configuration.Resolver.GetJsonSerializer());
                if (user == null || String.IsNullOrEmpty(user.Identity))
                    return;

                context.Client.Configuration.CurrentSessionIdentifier = user.Identity;
            }

            if (_heartbeat == null) {
                _heartbeat = new SessionHeartbeat(context.Client.Configuration.CurrentSessionIdentifier, _interval, context.Client);
            } else if (_heartbeat.SessionIdentifier != context.Client.Configuration.CurrentSessionIdentifier) {
                if (_heartbeat != null)
                    _heartbeat.Dispose();

                _heartbeat = new SessionHeartbeat(context.Client.Configuration.CurrentSessionIdentifier, _interval, context.Client);
            } else {
                if (_heartbeat != null)
                    _heartbeat.DelayNext();
            }
        }

        public void Dispose() {
            if (_heartbeat != null) {
                _heartbeat.Dispose();
                _heartbeat = null;
            }
        }
    }

    public class SessionHeartbeat : IDisposable {
        private readonly Timer _timer;
        private readonly TimeSpan _interval;
        private readonly ExceptionlessClient _client;

        public SessionHeartbeat(string sessionIdentifier, TimeSpan interval, ExceptionlessClient client) {
            SessionIdentifier = sessionIdentifier;
            _interval = interval;
            _client = client;
            _timer = new Timer(SendHeartbeat, null, _interval, _interval);
        }
        
        public string SessionIdentifier { get; private set; }
        
        public void DelayNext() {
            _timer.Change(_interval, _interval);
        }

        private void SendHeartbeat(object state) {
            _client.SubmitSessionHeartbeat(SessionIdentifier);
        }

        public void Dispose() {
            _timer.Dispose();
        }
    }
}