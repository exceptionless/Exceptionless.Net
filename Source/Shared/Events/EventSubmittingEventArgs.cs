using System;
using Exceptionless.Plugins;
using Exceptionless.Models;

namespace Exceptionless {
    public class EventSubmittingEventArgs : EventSubmissionEventArgsBase {
        public EventSubmittingEventArgs(ExceptionlessClient client, Event data, ContextData pluginContextData) : base(client, data, pluginContextData) {}
        /// <summary>
        /// Wether the event should be canceled.
        /// </summary>
        public bool Cancel { get; set; }
    }
}