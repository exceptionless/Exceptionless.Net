using System;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Plugins {
    public class EventPluginContext {
        public EventPluginContext(ExceptionlessClient client, Event ev, ContextData contextData = null) {
            Client = client;
            Event = ev;
            Data = contextData ?? new ContextData();
        }

        public ExceptionlessClient Client { get; private set; }
        public Event Event { get; private set; }
        public IDependencyResolver Resolver { get { return Client.Configuration.Resolver; }}
        public ContextData Data { get; private set; }

        public IExceptionlessLog Log {
            get { return Resolver.GetLog(); }
        }

        /// <summary>
        /// Used to cancel event submission.
        /// </summary>
        public bool Cancel { get; set; }
    }
}