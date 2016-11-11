using System;
using Microsoft.AspNetCore.Http;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.AspNetCore;

namespace Exceptionless.AspNetCore {
    [Priority(90)]
    internal class ExceptionlessAspNetCorePlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            var httpContext = context.ContextData.GetHttpContext();
            var serializer = context.Client.Configuration.Resolver.GetJsonSerializer();
            if (context.Client.Configuration.IncludePrivateInformation)
                AddUser(context, httpContext, serializer);

            if (httpContext == null)
                return;

            var ri = context.Event.GetRequestInfo(serializer);
            if (ri != null)
                return;

            try {
                ri = httpContext.GetRequestInfo(context.Client.Configuration);
            } catch (Exception ex) {
                context.Log.Error(typeof(ExceptionlessAspNetCorePlugin), ex, "Error adding request info.");
            }

            if (ri == null)
                return;

            var error = context.Event.GetError(serializer);
            if (error != null && error.Code == "404") {
                context.Event.Type = Event.KnownTypes.NotFound;
                context.Event.Source = ri.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
            }

            context.Event.AddRequestInfo(ri);
        }

        private static void AddUser(EventPluginContext context, HttpContext httpContext, IJsonSerializer serializer) {
            var user = context.Event.GetUserIdentity(serializer);
            if (user != null)
                return;

            // TODO: Should we fall back to Thread.CurrentPrincipal?
            var principal = httpContext?.User;
            if (principal != null && principal.Identity.IsAuthenticated)
                context.Event.SetUserIdentity(principal.Identity.Name);
        }
    }
}