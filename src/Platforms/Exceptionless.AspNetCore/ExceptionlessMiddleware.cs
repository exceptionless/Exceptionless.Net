using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

                ex.ToExceptionless(contextData, _client).SetHttpContext(context).Submit();
                throw;
            }

            if (context.Response?.StatusCode == 404) {
                string path = context.Request.Path.HasValue ? context.Request.Path.Value : "/";
                _client.CreateNotFound(path).SetHttpContext(context).Submit();
            }
        }
    }
}