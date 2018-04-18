using System;
using System.Web.Mvc;
using Exceptionless.Plugins;

namespace Exceptionless.Mvc {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ExceptionlessSendErrorsAttribute : FilterAttribute, IExceptionFilter {

        private readonly ExceptionlessClient _client;

        public ExceptionlessSendErrorsAttribute(ExceptionlessClient client) {
            _client = client ?? throw new ArgumentNullException();
        }

        public ExceptionlessSendErrorsAttribute() {
            _client = ExceptionlessClient.Default;
        }

        public void OnException(ExceptionContext filterContext) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("SendErrorsAttribute");
            contextData.Add("HttpContext", filterContext.HttpContext);

            filterContext.Exception.ToExceptionless(contextData, _client).Submit();
        }
    }
}
