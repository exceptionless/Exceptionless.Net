using System;
using System.Collections.Concurrent;

namespace Exceptionless.Plugins {
    [Priority(90)]
    public class SessionIdManagerPlugin : IEventPlugin {
        private ConcurrentDictionary<string, string> _sessionIdMap = new ConcurrentDictionary<string, string>();

        public void Run(EventPluginContext context) {
            string sessionId;

            if (context.Event.IsSessionStart()) {
                // remove old session 
                // add new session id
            } if (context.Event.IsSessionEnd()) {
              // remove old session  
            }


            // check to see if the user changed the sessionid..

            //if (String.IsNullOrEmpty(context.Event.SessionId))
            //    context.Event.SessionId = sessionId;
        }
    }
}