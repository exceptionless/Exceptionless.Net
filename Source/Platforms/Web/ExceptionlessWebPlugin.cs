using System;
using System.Web;
using Exceptionless.Plugins;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless.Web {
    internal class ExceptionlessWebPlugin : IEventPlugin {
        private const string TAGS_HTTP_CONTEXT_NAME = "Exceptionless.Tags";

        public void Run(EventPluginContext context) {
            HttpContextBase httpContext = context.Data.GetHttpContext();

            // if the context is not passed in, try and grab it
            if (httpContext == null && HttpContext.Current != null)
                httpContext = HttpContext.Current.ToWrapped();

            if (httpContext == null)
                return;

            // ev.ExceptionlessClientInfo.Platform = ".NET Web";
            if (context.Client.Configuration.IncludePrivateInformation
                && httpContext.User != null
                && httpContext.User.Identity.IsAuthenticated)
                context.Event.SetUserIdentity(httpContext.User.Identity.Name);

            var tags = httpContext.Items[TAGS_HTTP_CONTEXT_NAME] as TagSet;
            if (tags != null)
                context.Event.Tags.UnionWith(tags);

            RequestInfo requestInfo = null;
            try {
                requestInfo = httpContext.GetRequestInfo(context.Client.Configuration);
            } catch (Exception ex) {
                context.Log.Error(typeof(ExceptionlessWebPlugin), ex, "Error adding request info.");
            }

            if (requestInfo == null)
                return;

            var httpException = context.Data.GetException() as HttpException;
            if (httpException != null) {
                int httpCode = httpException.GetHttpCode();
                if (httpCode == 404) {
                    context.Event.Type = Event.KnownTypes.NotFound;
                    context.Event.Source = requestInfo.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
                    context.Event.Data.Clear();
                }
            }

            context.Event.AddRequestInfo(requestInfo);
        }
    }
}