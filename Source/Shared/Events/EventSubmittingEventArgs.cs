using System;
using Exceptionless.Enrichments;
using Exceptionless.Models;

namespace Exceptionless {
    public class EventSubmittingEventArgs : EventArgs {
        public EventSubmittingEventArgs(ExceptionlessClient client, Event data, ContextData enrichmentContextData) {
            Client = client;
            Event = data;
            EnrichmentContextData = enrichmentContextData;
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
        /// Any contextual data objects to be used by Exceptionless enrichments to gather default
        /// information to add to the event data.
        /// </summary>
        public ContextData EnrichmentContextData { get; private set; }

        /// <summary>
        /// Wether the event is an unhandled error.
        /// </summary>
        public bool IsUnhandledError {
            get { return EnrichmentContextData != null && EnrichmentContextData.IsUnhandledError; }
        }

        /// <summary>
        /// Wether the event should be canceled.
        /// </summary>
        public bool Cancel { get; set; }
    }
}