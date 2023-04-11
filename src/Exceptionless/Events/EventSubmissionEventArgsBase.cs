using System;
using Exceptionless.Models;
using Exceptionless.Plugins;

namespace Exceptionless {
    public abstract class EventSubmissionEventArgsBase : EventArgs {
        protected EventSubmissionEventArgsBase(ExceptionlessClient client, Event data, ContextData pluginContextData) {
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
        /// Whether the event is an unhandled error.
        /// </summary>
        public bool IsUnhandledError {
            get { return PluginContextData != null && PluginContextData.IsUnhandledError; }
        }
    }
}