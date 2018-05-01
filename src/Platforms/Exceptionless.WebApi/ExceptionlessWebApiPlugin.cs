using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Web.Http.Controllers;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.WebApi {
    [Priority(90)]
    internal class ExceptionlessWebApiPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            var actionContext = context.ContextData.GetHttpActionContext();
            var serializer = context.Client.Configuration.Resolver.GetJsonSerializer();
            if (context.Client.Configuration.IncludeUserName)
                AddUser(context, actionContext, serializer);

            if (actionContext == null)
                return;

            var ri = context.Event.GetRequestInfo(serializer);
            if (ri != null)
                return;

            try {
                ri = actionContext.GetRequestInfo(context.Client.Configuration);
            } catch (Exception ex) {
                context.Log.Error(typeof(ExceptionlessWebApiPlugin), ex, "Error adding request info.");
            }

            if (ri == null)
                return;

            context.Event.AddRequestInfo(ri);
            var error = context.Event.GetError(serializer);
            if (error == null || error.Code != "404")
                return;

            context.Event.Type = Event.KnownTypes.NotFound;
            context.Event.Source = ri.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
            if (!context.Client.Configuration.Settings.GetTypeAndSourceEnabled(context.Event.Type, context.Event.Source)) {
                context.Log.Info(String.Format("Cancelling event from excluded type: {0} and source: {1}", context.Event.Type, context.Event.Source));
                context.Cancel = true;
            }
        }

        private static void AddUser(EventPluginContext context, HttpActionContext actionContext, IJsonSerializer serializer) {
            var user = context.Event.GetUserIdentity(serializer);
            if (user != null)
                return;

            var principal = GetPrincipal(actionContext != null ? actionContext.Request : null);
            if (principal != null && principal.Identity.IsAuthenticated)
                context.Event.SetUserIdentity(principal.Identity.Name);
        }

        private static IPrincipal GetPrincipal(HttpRequestMessage request) {
            if (request == null)
                return Thread.CurrentPrincipal;

            const string RequestContextKey = "MS_RequestContext";

            object context;
            if (!request.Properties.TryGetValue(RequestContextKey, out context) || context == null)
                return Thread.CurrentPrincipal;

            if (_principalGetAccessor == null) {
                PropertyInfo principalProperty = context.GetType().GetProperties().SingleOrDefault(obj => obj.Name == "Principal");
                if (principalProperty == null)
                    return Thread.CurrentPrincipal;

                _principalGetAccessor = BuildGetAccessor(principalProperty.GetGetMethod());
            }

            var principal = _principalGetAccessor(context) as IPrincipal;
            return principal ?? Thread.CurrentPrincipal;
        }

        private static Func<object, object> _principalGetAccessor;

        private static Func<object, object> BuildGetAccessor(MethodInfo method) {
            ParameterExpression obj = Expression.Parameter(typeof(object), "o");

            Expression<Func<object, object>> expr = Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(Expression.Convert(obj, method.DeclaringType), method), typeof(object)), obj);

            return expr.Compile();
        }
    }
}