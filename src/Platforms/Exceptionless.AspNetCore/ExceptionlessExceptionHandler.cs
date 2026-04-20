using System;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Plugins;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Exceptionless.AspNetCore {
    public sealed class ExceptionlessExceptionHandler : IExceptionHandler {
        private readonly ExceptionlessClient _client;

        public ExceptionlessExceptionHandler(ExceptionlessClient client) {
            _client = client ?? ExceptionlessClient.Default;
        }

        /// <summary>
        /// Key used in <see cref="HttpContext.Items"/> to track the exception already submitted for this request,
        /// preventing duplicate submissions from the diagnostic listener.
        /// </summary>
        public const string HttpContextSubmittedKey = "Exceptionless:ExceptionSubmitted";

        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromResult(false);

            var contextData = new ContextData();
            contextData.MarkAsUnhandledError();
            contextData.SetSubmissionMethod(nameof(ExceptionlessExceptionHandler));

            exception.ToExceptionless(contextData, _client).SetHttpContext(httpContext).Submit();
            httpContext.Items[HttpContextSubmittedKey] = exception;

            return ValueTask.FromResult(false);
        }
    }
}
