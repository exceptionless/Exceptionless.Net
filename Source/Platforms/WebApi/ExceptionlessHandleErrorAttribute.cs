using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
using TaskExtensions = Exceptionless.Threading.Tasks.TaskExtensions;

namespace Exceptionless.WebApi {
    public class ExceptionlessHandleErrorAttribute : IExceptionFilter {
        public bool HasWrappedFilter { get { return WrappedFilter != null; } }

        public IExceptionFilter WrappedFilter { get; set; }
        public bool AllowMultiple { get { return HasWrappedFilter && WrappedFilter.AllowMultiple; } }

        public virtual void OnHttpException(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken) {
            if (HasWrappedFilter)
                WrappedFilter.ExecuteExceptionFilterAsync(actionExecutedContext, cancellationToken);

            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("ExceptionHttpFilter");
            contextData.Add("HttpActionContext", actionExecutedContext.ActionContext);

            actionExecutedContext.Exception.ToExceptionless(contextData).Submit();
        }

        public Task ExecuteExceptionFilterAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken) {
            ExceptionlessClient.Default.Configuration.Resolver.GetLog().Trace("ExecuteExceptionFilterAsync executing...");
            if (actionExecutedContext == null)
                throw new ArgumentNullException(nameof(actionExecutedContext));

            OnHttpException(actionExecutedContext, cancellationToken);
            return TaskExtensions.Completed();
        }
    }
}