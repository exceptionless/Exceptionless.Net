using System.Web.Http.ExceptionHandling;
using Exceptionless.Plugins;

namespace Exceptionless.WebApi {
    public class ExceptionlessExceptionLogger : ExceptionLogger {
        public override void Log(ExceptionLoggerContext context) {
            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod("ExceptionLogger");

            context.Exception
                .ToExceptionless(contextData)
                .SetHttpActionContext(context.ExceptionContext.ActionContext)
                .Submit();
        }
    }
}