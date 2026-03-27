using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Exceptionless.AspNetCore;
using Exceptionless.Models;
using Exceptionless.Models.Data;
using Exceptionless.Plugins.Default;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Exceptionless {
    public static class ExceptionlessExtensions {
        /// <summary>
        /// Registers the Exceptionless <see cref="IExceptionHandler"/> for capturing unhandled exceptions.
        /// Call this in your service configuration alongside <c>app.UseExceptionHandler()</c>.
        /// </summary>
        public static IServiceCollection AddExceptionlessAspNetCore(this IServiceCollection services) {
            services.AddHttpContextAccessor();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IExceptionHandler, ExceptionlessExceptionHandler>());
            return services;
        }

        /// <summary>
        /// Adds the Exceptionless middleware for 404 tracking and queue processing,
        /// subscribes to diagnostic events, and configures ASP.NET Core plugins.
        /// </summary>
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
            diagnosticListener?.Subscribe(new ExceptionlessDiagnosticListener(client));

            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() => client.ProcessQueueAsync().ConfigureAwait(false).GetAwaiter().GetResult());

            return app.UseMiddleware<ExceptionlessMiddleware>(client);
        }

        /// <summary>
        /// Adds the current request info.
        /// </summary>
        /// <param name="context">The http context to gather information from.</param>
        /// <param name="config">The config.</param>
        /// <param name="isUnhandledError">Whether this is an unhandled error. POST data is only collected for unhandled errors to avoid consuming the request stream.</param>
        public static RequestInfo GetRequestInfo(this HttpContext context, ExceptionlessConfiguration config, bool isUnhandledError = false) {
            return RequestInfoCollector.Collect(context, config, isUnhandledError);
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
            if (data.TryGetValue("HttpContext", out object context))
                return context as HttpContext;

            return null;
        }

        public static EventBuilder SetHttpContext(this EventBuilder builder, HttpContext context) {
            builder.PluginContextData["HttpContext"] = context;
            return builder;
        }
    }
}
