using System;
using System.Collections.Generic;

namespace Exceptionless.Plugins {
    [Priority(100)]
    public class SessionIdManagerPlugin : IEventPlugin {
        private Dictionary<string, string> _sessionIdMap = new Dictionary<string, string>();

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