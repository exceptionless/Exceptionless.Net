﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Filters;
using Exceptionless.ExtendedData;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Plugins.Default;
using Exceptionless.WebApi;

namespace Exceptionless {
    public static class ExceptionlessWebApiExtensions {
        /// <summary>
        /// Reads configuration settings, configures various plugins and wires up to platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        /// <param name="config">The HttpConfiguration instance.</param>
        public static void RegisterWebApi(this ExceptionlessClient client, HttpConfiguration config) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.Startup();
            client.Configuration.AddPlugin<ExceptionlessWebApiPlugin>();
            client.Configuration.AddPlugin<IgnoreUserAgentPlugin>();

            config.Services.Add(typeof(IExceptionLogger), new ExceptionlessExceptionLogger());

            ReplaceHttpErrorHandler(config, client);
        }

        /// <summary>
        /// Unregisters platform specific exception handlers.
        /// </summary>
        /// <param name="client">The ExceptionlessClient.</param>
        public static async Task UnregisterWebApiAsync(this ExceptionlessClient client) {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            await client.ShutdownAsync().ConfigureAwait(false);
            client.Configuration.RemovePlugin<ExceptionlessWebApiPlugin>();
        }

        private static void ReplaceHttpErrorHandler(HttpConfiguration config, ExceptionlessClient client) {
            FilterInfo filter = config.Filters.FirstOrDefault(f => f.Instance is IExceptionFilter);
            var handler = new ExceptionlessHandleErrorAttribute(client);

            if (filter != null) {
                if (filter.Instance is ExceptionlessHandleErrorAttribute)
                    return;

                config.Filters.Remove(filter.Instance);

                handler.WrappedFilter = (IExceptionFilter)filter.Instance;
            }

            config.Filters.Add(handler);
        }

        /// <summary>
        /// Adds the current request info.
        /// </summary>
        /// <param name="context">The http action context to gather information from.</param>
        /// <param name="config">The config.</param>
        public static RequestInfo GetRequestInfo(this HttpActionContext context, ExceptionlessConfiguration config) {
            return RequestInfoCollector.Collect(context, config);
        }

        /// <summary>
        /// Adds the current request info as extended data to the event.
        /// </summary>
        /// <param name="ev">The event model.</param>
        /// <param name="context">The http action context to gather information from.</param>
        /// <param name="config">The config.</param>
        public static Event AddHttpRequestInfo(this Event ev, HttpActionContext context, ExceptionlessConfiguration config = null) {
            if (context == null)
                return ev;

            if (config == null)
                config = ExceptionlessClient.Default.Configuration;

            ev.AddRequestInfo(context.GetRequestInfo(config));

            return ev;
        }

        internal static HttpActionContext GetHttpActionContext(this IDictionary<string, object> data) {
            if (data.TryGetValue("HttpActionContext", out object context))
                return context as HttpActionContext;

            return null;
        }

        public static EventBuilder SetHttpActionContext(this EventBuilder builder, HttpActionContext context) {
            builder.PluginContextData["HttpActionContext"] = context;
            return builder;
        }
    }
}