using System;
using Exceptionless.Plugins;
using Exceptionless.Models;

namespace Exceptionless {
    public class EventSubmittingEventArgs : EventArgs {
        public EventSubmittingEventArgs(ExceptionlessClient client, Event data, ContextData pluginContextData) {
            Client = client;
            Event = data;
            PluginContextData = pluginContextData;
        }

        /// <summary>
        /// The client instance that is submitting the event.
        /// </summary>
        public ExceptionlessClient Client { get; private set; }

        /// <summary>
        /// The event that is being submitted.
        /// </summary>
        public Event Event { get; private set; }
        
        /// <summary>
        /// Any contextual data objects to be used by Exceptionless plugins to gather default
        /// information to add to the event data.
        /// </summary>
        public ContextData PluginContextData { get; private set; }

        /// <summary>
        /// Wether the event is an unhandled error.
        /// </summary>
        public bool IsUnhandledError {
            get { return PluginContextData != null && PluginContextData.IsUnhandledError; }
        }

        /// <summary>
        /// Wether the event should be canceled.
        /// </summary>
        public bool Cancel { get; set; }
    }
}