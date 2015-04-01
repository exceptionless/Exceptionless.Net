using System;
using Exceptionless.Models;

namespace Exceptionless.Enrichments {
    public interface IEventEnrichment {
        /// <summary>
        /// Enrich the event with additional information.
        /// </summary>
        /// <param name="context">Context information.</param>
        /// <param name="ev">Event to enrich.</param>
        void Enrich(EventEnrichmentContext context, Event ev);
    }
}