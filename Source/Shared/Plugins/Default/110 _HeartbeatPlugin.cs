using System;
using System.Threading;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Queue;

namespace Exceptionless.Plugins.Default {
    [Priority(110)]
    public class HeartbeatPlugin : IEventPlugin, IDisposable {
        private readonly IEventQueue _eventQueue;
        private readonly IExceptionlessLog _log;
        private Timer _timer;
        private Event _lastEvent;

        public HeartbeatPlugin(IEventQueue eventQueue, IExceptionlessLog log) {
            _eventQueue = eventQueue;
            _log = log;
        }

        public void Run(EventPluginContext context) {
            var sessionIdentifier = context.Event.SessionId ?? context.Event.GetUserIdentity()?.Identity;
            if (String.IsNullOrEmpty(sessionIdentifier) || context.Event.IsSessionEnd()) {
                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                _lastEvent = null;
                return;
            }
            
            if (_timer == null)
                _timer = new Timer(OnEnqueueHeartbeat, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            else
                _timer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _lastEvent = context.Event;
        }

        private void OnEnqueueHeartbeat(object state) {
            var heartbeatEvent = CreateHeartbeatEvent(_lastEvent);
            if (heartbeatEvent == null)
                return;

            _log.Trace(nameof(HeartbeatPlugin), $"Enqueuing heartbeat for session: {_lastEvent.SessionId}");
            _eventQueue.Enqueue(heartbeatEvent);
        }

        private Event CreateHeartbeatEvent(Event source) {
            if (source == null)
                return null;

            var heartbeatEvent = new Event {
                Date = DateTimeOffset.Now,
                Type = Event.KnownTypes.Heartbeat,
                SessionId = source.SessionId
            };

            heartbeatEvent.SetUserIdentity(source.GetUserIdentity());

            return heartbeatEvent;
        }

        public void Dispose() {
            if (_timer == null)
                return;
            
            _timer.Dispose();
            _timer = null;
        }
    }
}