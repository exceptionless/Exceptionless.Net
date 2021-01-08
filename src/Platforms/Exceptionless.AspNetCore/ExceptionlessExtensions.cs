using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Exceptionless.AspNetCore;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Plugins.Default;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Exceptionless {
    public static class ExceptionlessExtensions {
        /// <summary>
        /// Adds the Exceptionless middleware for capturing unhandled exceptions and ensures that the Exceptionless pending queue is processed before the host shuts down.
        /// </summary>
        /// <param name="app">The target <see cref="IApplicationBuilder"/> to add Exceptionless to.</param>
        /// <param name="client">Optional pre-configured <see cref="ExceptionlessClient"/> instance to use. If not specified (recommended), the <see cref="ExceptionlessClient"/>
        /// instance registered in the services collection will be used.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionless(this IApplicationBuilder app, ExceptionlessClient client = null) {
            if (client == null)
                client = app.ApplicationServices.GetService<ExceptionlessClient>() ?? ExceptionlessClient.Default;

            // Can be registered in Startup.ConfigureServices via services.AddHttpContextAccessor();
            // this is necessary to obtain Session and Request information outside of ExceptionlessMiddleware
            var contextAccessor = app.ApplicationServices.GetService<IHttpContextAccessor>();

            client.Startup();
            client.Configuration.AddPlugin(new ExceptionlessAspNetCorePlugin(contextAccessor));
            client.Configuration.AddPlugin<IgnoreUserAgentPlugin>();
            //client.Configuration.Resolver.Register<ILastReferenceIdManager, WebLastReferenceIdManager>();

            var diagnosticListener = app.ApplicationServices.GetRequiredService<DiagnosticListener>();
            diagnosticListener?.SubscribeWithAdapter(new ExceptionlessDiagnosticListener(client));

            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() => client.ProcessQueue());

            return app.UseMiddleware<ExceptionlessMiddleware>(client);
        }

        [Obsolete("UseExceptionless should be called without an overload, ExceptionlessClient should be configured when adding to services collection using AddExceptionless")]
        public static IApplicationBuilder UseExceptionless(this IApplicationBuilder app, Action<ExceptionlessConfiguration> configure) {
            var client = app.ApplicationServices.GetService<ExceptionlessClient>() ?? ExceptionlessClient.Default;
            configure?.Invoke(client.Configuration);
            return app.UseExceptionless(client);
        }

        [Obsolete("UseExceptionless should be called without an overload, ExceptionlessClient should be configured when adding to services collection using AddExceptionless")]
        public static IApplicationBuilder UseExceptionless(this IApplicationBuilder app, IConfiguration configuration) {
            var client = app.ApplicationServices.GetService<ExceptionlessClient>() ?? ExceptionlessClient.Default;
            client.Configuration.ReadFromConfiguration(configuration);
            return app.UseExceptionless(client);
        }

        [Obsolete("UseExceptionless should be called without an overload, ExceptionlessClient should be configured when adding to services collection using AddExceptionless")]
        public static IApplicationBuilder UseExceptionless(this IApplicationBuilder app, string apiKey) {
            var client = app.ApplicationServices.GetService<ExceptionlessClient>() ?? ExceptionlessClient.Default;
            client.Configuration.ApiKey = apiKey;
            return app.UseExceptionless(client);
        }

        /// <summary>
        /// Adds the current request info.
        /// </summary>
        /// <param name="context">The http context to gather information from.</param>
        /// <param name="config">The config.</param>
        public static RequestInfo GetRequestInfo(this HttpContext context, ExceptionlessConfiguration config) {
            return RequestInfoCollector.Collect(context, config);
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

        internal static HttpContext GetHttpContext(this IDictionary<string, object> data) {
            object context;
            if (data.TryGetValue("HttpContext", out context))
                return context as HttpContext;

            return null;
        }

        public static EventBuilder SetHttpContext(this EventBuilder builder, HttpContext context) {
            builder.PluginContextData["HttpContext"] = context;
            return builder;
        }
    }
}
