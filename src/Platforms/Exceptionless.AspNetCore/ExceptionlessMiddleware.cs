using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Exceptionless;
using Exceptionless.Plugins;

namespace Exceptionless.AspNetCore {
    public class ExceptionlessMiddleware {
        private readonly ExceptionlessClient _client;
        private readonly RequestDelegate _next;

        public ExceptionlessMiddleware(RequestDelegate next, ExceptionlessClient client) {
            _client = client ?? ExceptionlessClient.Default;
            _next = next;
        }
        
        public async Task Invoke(HttpContext context) {
            try {
                await _next(context);
            } catch (Exception ex) {
                var contextData = new ContextData();
                contextData.MarkAsUnhandledError();
                contextData.SetSubmissionMethod(nameof(ExceptionlessMiddleware));
                contextData.Add(nameof(HttpContext), context);

                ex.ToExceptionless(contextData, _client).Submit();
                throw;
            }
        }
    }
}