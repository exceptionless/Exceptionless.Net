using System;
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

            if (httpContext == null)
                return;
            
            var serializer = context.Client.Configuration.Resolver.GetJsonSerializer();
            if (context.Client.Configuration.IncludePrivateInformation && httpContext.User != null && httpContext.User.Identity.IsAuthenticated) {
                var user = context.Event.GetUserIdentity(serializer);
                if (user == null)
                    context.Event.SetUserIdentity(httpContext.User.Identity.Name);
            }

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

            var httpException = context.ContextData.GetException() as HttpException;
            if (httpException != null) {
                int httpCode = httpException.GetHttpCode();
                if (httpCode == 404) {
                    context.Event.Type = Event.KnownTypes.NotFound;
                    context.Event.Source = ri.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
                }
            }

            context.Event.AddRequestInfo(ri);
        }
    }
}