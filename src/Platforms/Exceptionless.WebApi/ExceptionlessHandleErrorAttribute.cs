using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using Exceptionless.Dependency;
using Exceptionless.Plugins;
namespace Exceptionless.WebApi {
    public class ExceptionlessHandleErrorAttribute : IExceptionFilter {
        private readonly ExceptionlessClient _client;

        public bool HasWrappedFilter { get { return WrappedFilter != null; } }

        public IExceptionFilter WrappedFilter { get; set; }

        public bool AllowMultiple { get { return HasWrappedFilter && WrappedFilter.AllowMultiple; } }

        public ExceptionlessHandleErrorAttribute(ExceptionlessClient client = null) {
            _client = client ?? ExceptionlessClient.Default;
        }

        public virtual void OnHttpException(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken) {
            if (HasWrappedFilter)
                WrappedFilter.ExecuteExceptionFilterAsync(actionExecutedContext, cancellationToken);

            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("ExceptionHttpFilter");

            actionExecutedContext.Exception
                .ToExceptionless(contextData, _client)
                .SetHttpActionContext(actionExecutedContext.ActionContext)
                .Submit();
        }

        public Task ExecuteExceptionFilterAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken) {
            _client.Configuration.Resolver.GetLog().Trace("ExecuteExceptionFilterAsync executing...");
            if (actionExecutedContext == null)
                throw new ArgumentNullException("actionExecutedContext");

            OnHttpException(actionExecutedContext, cancellationToken);
            return Task.FromResult(0);
        }
    }
}