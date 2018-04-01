﻿using System;
using System.Collections.Generic;
using Exceptionless.Plugins;
using Exceptionless.Plugins.Default;
using Exceptionless.ExtendedData;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Nancy;
using Nancy;
using Nancy.Bootstrapper;

namespace Exceptionless {
    public static class ExceptionlessNancyExtensions {
        private const string NANCY_CONTEXT = "NancyContext";

        /// <summary>
        /// Reads configuration settings, configures various plugins and wires up to platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        /// <param name="pipelines">The IPipelines instance.</param>
        public static void RegisterNancy(this ExceptionlessClient client, IPipelines pipelines) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.Startup();
            client.Configuration.AddPlugin<ExceptionlessNancyPlugin>();
            client.Configuration.AddPlugin<IgnoreUserAgentPlugin>();

            pipelines.OnError += OnError;
            pipelines.AfterRequest += AfterRequest;
        }

        /// <summary>
        /// Unregisters platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static void UnregisterNancy(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.Shutdown();
            client.Configuration.RemovePlugin<ExceptionlessNancyPlugin>();
        }

        private static Response OnError(NancyContext context, Exception exception) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("NancyPipelineException");
            contextData.Add(NANCY_CONTEXT, context);

            exception.ToExceptionless(contextData).Submit();

            return context.Response;
        }

        private static void AfterRequest(NancyContext context) {
            var contextData = new ContextData { { NANCY_CONTEXT, context } };
            if (context.Response.StatusCode == HttpStatusCode.NotFound)
                ExceptionlessClient.Default.SubmitEvent(new Event { Type = Event.KnownTypes.NotFound }, contextData);
        }

        /// <summary>
        /// Adds the current request info.
        /// </summary>
        /// <param name="context">The nancy context to gather information from.</param>
        /// <param name="config">The config.</param>
        public static RequestInfo GetRequestInfo(this NancyContext context, ExceptionlessConfiguration config) {
            return RequestInfoCollector.Collect(context, config);
        }

        public static Event AddRequestInfo(this Event ev, NancyContext context) {
            if (context == null)
                return ev;

            ev.AddRequestInfo(context.GetRequestInfo(ExceptionlessClient.Default.Configuration));

            return ev;
        }

        /// <summary>
        /// Adds the current request info as extended data to the event.
        /// </summary>
        /// <param name="builder">The event builder.</param>
        /// <param name="context">The nancy context to gather information from.</param>
        public static EventBuilder AddRequestInfo(this EventBuilder builder, NancyContext context) {
            builder.Target.AddRequestInfo(context);
            return builder;
        }

        internal static NancyContext GetNancyContext(this IDictionary<string, object> data) {
            if (!data.ContainsKey(NANCY_CONTEXT))
                return null;

            return data[NANCY_CONTEXT] as NancyContext;
        }
    }
}