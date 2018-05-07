using System;
using Exceptionless.Plugins;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless.Nancy {
    [Priority(90)]
    internal class ExceptionlessNancyPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            //error.ExceptionlessClientInfo.Platform = "Nancy";

            var nancyContext = context.ContextData.GetNancyContext();
            if (nancyContext == null)
                return;

            if (nancyContext.CurrentUser != null && context.Client.Configuration.IncludeUserName)
                context.Event.SetUserIdentity(nancyContext.CurrentUser.UserName);

            RequestInfo ri = null;
            try {
                ri = nancyContext.GetRequestInfo(context.Client.Configuration);
            } catch (Exception ex) {
                context.Log.Error(typeof(ExceptionlessNancyPlugin), ex, "Error adding request info.");
            }

            if (ri == null)
                return;

            context.Event.AddRequestInfo(ri);
            if (context.Event.Type != Event.KnownTypes.NotFound)
                return;

            context.Event.Source = ri.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
            if (!context.Client.Configuration.Settings.GetTypeAndSourceEnabled(context.Event.Type, context.Event.Source)) {
                context.Log.Info(String.Format("Cancelling event from excluded type: {0} and source: {1}", context.Event.Type, context.Event.Source));
                context.Cancel = true;
            }
        }
    }
}