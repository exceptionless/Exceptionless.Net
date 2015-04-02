using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Web.Http.Controllers;
using Exceptionless.Plugins;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Models.Data;

namespace Exceptionless.WebApi {
    internal class ExceptionlessWebApiPlugin : IEventPlugin {
        public void Run(EventPluginContext context) {
            if (!context.ContextData.ContainsKey("HttpActionContext"))
                return;

            HttpActionContext actionContext = context.ContextData.GetHttpActionContext();
            if (actionContext == null)
                return;

            IPrincipal principal = GetPrincipal(actionContext.Request);
            if (context.Client.Configuration.IncludePrivateInformation && principal != null && principal.Identity.IsAuthenticated)
                context.Event.SetUserIdentity(principal.Identity.Name);

            RequestInfo requestInfo = null;
            try {
                requestInfo = actionContext.GetRequestInfo(context.Client.Configuration);
            } catch (Exception ex) {
                context.Log.Error(typeof(ExceptionlessWebApiPlugin), ex, "Error adding request info.");
            }

            if (requestInfo == null)
                return;

            var error = context.Event.GetError();
            if (error != null && error.Code == "404") {
                context.Event.Type = Event.KnownTypes.NotFound;
                context.Event.Source = requestInfo.GetFullPath(includeHttpMethod: true, includeHost: false, includeQueryString: false);
                context.Event.Data.Clear();
            }

            context.Event.AddRequestInfo(requestInfo);
        }

        private static IPrincipal GetPrincipal(HttpRequestMessage request) {
            if (request == null)
                throw new ArgumentNullException("request");

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