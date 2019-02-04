using System;
using System.Threading;
using System.Web;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Web {
    [Priority(90)]
    internal class ExceptionlessWebPlugin : IEventPlugin {
        private const string TAGS_HTTP_CONTEXT_NAME = "Exceptionless.Tags";

        public void Run(EventPluginContext context) {
            var httpContext = context.ContextData.GetHttpContext();

            // if the context is not passed in, try and grab it
            if (httpContext == null && HttpContext.Current != null)
                httpContext = HttpContext.Current.ToWrapped();

            var serializer = context.Client.Configuration.Resolver.GetJsonSerializer();
            if (context.Client.Configuration.IncludeUserName)
                AddUser(context, httpContext, serializer);

            if (httpContext == null)
                return;

            var tags = httpContext.Items[TAGS_HTTP_CONTEXT_NAME] as TagSet;
            if (tags != null)
                context.Event.Tags.UnionWith(tags);

            var ri = context.Event.GetRequestInfo(serializer);
            if (ri != null)
                return;

            try {
                ri = httpContext.GetRequestInfo(context.Client.Configuration);
            } catch (Exception ex) {
                context.Log.Error(typeof(ExceptionlessWebPlugin), ex, "Error adding request info.");
            }

            if (ri == null)
                return;

            context.Event.AddRequestInfo(ri);
            var httpException = context.ContextData.GetException() as HttpException;
            if (httpException == null)
                return;

            int httpCode = httpException.GetHttpCode();
            if (httpCode != 404)
                return;

            context.Event.Type = Event.KnownTypes.NotFound;
            context.Event.Source = ri.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
            if (!context.Client.Configuration.Settings.GetTypeAndSourceEnabled(context.Event.Type, context.Event.Source)) {
                context.Log.Info(String.Format("Cancelling event from excluded type: {0} and source: {1}", context.Event.Type, context.Event.Source));
                context.Cancel = true;
            }
        }

        private static void AddUser(EventPluginContext context, HttpContextBase httpContext, IJsonSerializer serializer) {
            var user = context.Event.GetUserIdentity(serializer);
            if (user != null)
                return;

            if (httpContext != null && httpContext.User != null && httpContext.User.Identity.IsAuthenticated)
                context.Event.SetUserIdentity(httpContext.User.Identity.Name);
            else if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity.IsAuthenticated)
                context.Event.SetUserIdentity(Thread.CurrentPrincipal.Identity.Name);
        }
    }
}