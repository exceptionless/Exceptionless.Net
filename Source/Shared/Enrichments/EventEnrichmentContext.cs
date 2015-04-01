using System;
using Exceptionless.Dependency;
using Exceptionless.Logging;

namespace Exceptionless.Enrichments {
    public class EventEnrichmentContext {
        public EventEnrichmentContext(ExceptionlessClient client, ContextData contextData = null) {
            Client = client;
            Data = contextData ?? new ContextData();
        }

        public ExceptionlessClient Client { get; private set; }
        public IDependencyResolver Resolver { get { return Client.Configuration.Resolver; }}
        public ContextData Data { get; private set; }

        public IExceptionlessLog Log {
            get { return Resolver.GetLog(); }
        }
    }
}