using Exceptionless.Models;
using Exceptionless.Plugins;

namespace Exceptionless {
    public class EventSubmittedEventArgs : EventSubmissionEventArgsBase {
        public EventSubmittedEventArgs(ExceptionlessClient client, Event data, ContextData pluginContextData) : base(client, data, pluginContextData) {}
    }
}