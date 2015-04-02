using System;
using Exceptionless.Plugins;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless.Nancy {
    internal class ExceptionlessNancyPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            //error.ExceptionlessClientInfo.Platform = "Nancy";

            var nancyContext = context.ContextData.GetNancyContext();
            if (nancyContext == null)
                return;

            if (nancyContext.CurrentUser != null && context.Client.Configuration.IncludePrivateInformation)
                context.Event.SetUserIdentity(nancyContext.CurrentUser.UserName);

            RequestInfo requestInfo = null;
            try {
                requestInfo = nancyContext.GetRequestInfo(context.Client.Configuration);
            } catch (Exception ex) {
                context.Log.Error(typeof(ExceptionlessNancyPlugin), ex, "Error adding request info.");
            }

            if (requestInfo == null)
                return;

            if (context.Event.Type == Event.KnownTypes.NotFound) {
                context.Event.Source = requestInfo.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
                context.Event.Data.Clear();
            }

            context.Event.AddRequestInfo(requestInfo);
        }
    }
}