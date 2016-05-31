using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Exceptionless;
using Exceptionless.AspNetCore;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless.AspNetCore {
    public static class ApplicationBuilderExtensions {
        public static IApplicationBuilder UseExceptionless(this IApplicationBuilder app, ExceptionlessClient client = null) {
            if (client == null)
                client = ExceptionlessClient.Default;

            client.Startup();
            client.Configuration.AddPlugin<ExceptionlessAspNetCorePlugin>();
            //client.Configuration.AddPlugin<IgnoreUserAgentPlugin>();
            //client.Configuration.Resolver.Register<ILastReferenceIdManager, WebLastReferenceIdManager>();

            return app.UseMiddleware<ExceptionlessMiddleware>(client);
        }
        
        /// <summary>
        /// Adds the current request info.
        /// </summary>
        /// <param name="context">The http context to gather information from.</param>
        /// <param name="config">The config.</param>
        public static RequestInfo GetRequestInfo(this HttpContext context, ExceptionlessConfiguration config) {
            return RequestInfoCollector.Collect(context, config.DataExclusions);
        }

        /// <summary>
        /// Adds the current request info as extended data to the event.
        /// </summary>
        /// <param name="ev">The event model.</param>
        /// <param name="context">The http context to gather information from.</param>
        /// <param name="config">The config.</param>
        public static Event AddRequestInfo(this Event ev, HttpContext context, ExceptionlessConfiguration config = null) {
            if (context == null)
                return ev;

            if (config == null)
                config = ExceptionlessClient.Default.Configuration;

            ev.AddRequestInfo(context.GetRequestInfo(config));

            return ev;
        }

        /// <summary>
        /// Adds the current request info as extended data to the event.
        /// </summary>
        /// <param name="builder">The event builder.</param>
        /// <param name="context">The http context to gather information from.</param>
        public static EventBuilder AddRequestInfo(this EventBuilder builder, HttpContext context) {
            builder.Target.AddRequestInfo(context, builder.Client.Configuration);
            return builder;
        }

        internal static HttpContext GetHttpContext(this IDictionary<string, object> data) {
            if (!data.ContainsKey(nameof(HttpContext)))
                return null;

            return data[nameof(HttpContext)] as HttpContext;
        }
    }
}