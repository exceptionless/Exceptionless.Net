using System;
using System.Threading;
using Exceptionless.Models.Data;

namespace Exceptionless.Plugins.Default {
    [Priority(100)]
    public class HeartbeatPlugin : IEventPlugin, IDisposable {
        private SessionHeartbeat _heartbeat;

        public void Run(EventPluginContext context) {
            if (context.Event.IsSessionHeartbeat())
                return;
            
            if (context.Event.IsSessionEnd()) {
                _heartbeat?.Dispose();
                _heartbeat = null;
                return;
            }

            var user = context.Event.GetUserIdentity();
            if (String.IsNullOrEmpty(user?.Identity))
                return;
            
            if (_heartbeat == null) {
                _heartbeat = new SessionHeartbeat(user, context.Client);
            } else if (_heartbeat.User.Identity != user.Identity) {
                _heartbeat?.Dispose();
                _heartbeat = new SessionHeartbeat(user, context.Client);
            } else {
                _heartbeat?.DelayNext();
            }
        }

        public void Dispose() {
            _heartbeat?.Dispose();
            _heartbeat = null;
        }
    }

    public class SessionHeartbeat : IDisposable {
        private readonly Timer _timer;
        private readonly int _interval = 30 * 1000;
        private readonly ExceptionlessClient _client;

        public SessionHeartbeat(UserInfo user, ExceptionlessClient client) {
            User = user;
            _client = client;
            _timer = new Timer(SendHeartbeat, null, _interval, _interval);
        }
        
        public UserInfo User { get; }
        
        public void DelayNext() {
            _timer.Change(_interval, _interval);
        }

        private void SendHeartbeat(object state) {
            _client.CreateSessionHeartbeat().SetUserIdentity(User).Submit();
        }

        public void Dispose() {
            _timer.Dispose();
        }
    }
}