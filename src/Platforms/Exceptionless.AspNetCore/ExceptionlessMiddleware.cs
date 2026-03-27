using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Exceptionless.AspNetCore {
    public class ExceptionlessMiddleware {
        private readonly ExceptionlessClient _client;
        private readonly RequestDelegate _next;

        public ExceptionlessMiddleware(RequestDelegate next, ExceptionlessClient client) {
            _client = client ?? ExceptionlessClient.Default;
            _next = next;
        }

        public async Task Invoke(HttpContext context) {
            if (_client.Configuration.ProcessQueueOnCompletedRequest) {
                context.Response.OnCompleted(async () => {
                    await _client.ProcessQueueAsync();
                });
            }

            await _next(context);

            if (context.Response?.StatusCode == 404) {
                string path = context.Request.Path.HasValue ? context.Request.Path.Value : "/";
                _client.CreateNotFound(path).SetHttpContext(context).Submit();
            }
        }
    }
}
