using Microsoft.AspNet.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http.Features;
using System.Runtime.Remoting.Contexts;
using Microsoft.Net.Http.Headers;

namespace ExceptionLess.Core
{
    public class ExceptionlessCoreMiddleware
    {
        
        private readonly RequestDelegate _next;
        private readonly ExceptionlessCoreOptions _exceptionlessCoreOptions;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ExceptionlessCorePlugIn _exceptionCoreManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessCoreMiddleware"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="next">The next.</param>
        /// <param name="options">The options.</param>
        /// <param name="diagnosticSource">The diagnostic source.</param>
        /// <param name="exceptionlessCorePlugIn">The exception Core manager.</param>
        public ExceptionlessCoreMiddleware(
            ILoggerFactory loggerFactory,
            RequestDelegate next,
            ExceptionlessCoreOptions exceptionlessCoreOptions,
            DiagnosticSource diagnosticSource,
            ExceptionlessCorePlugIn exceptionlessCorePlugIn)
        {
            _next = next;
            _exceptionlessCoreOptions = exceptionlessCoreOptions;
            _diagnosticSource = diagnosticSource;
            _exceptionCoreManager = exceptionlessCorePlugIn;
            
        }

        /// <summary>
        /// Invokes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                try
                {
                    if (!_exceptionlessCoreOptions.RethrowException)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = 500;
                        context.Response.OnStarting(state =>
                        {
                            var response = (HttpResponse)state;
                            response.Headers[HeaderNames.CacheControl] = "no-cache";
                            response.Headers[HeaderNames.Pragma] = "no-cache";
                            response.Headers[HeaderNames.Expires] = "-1";
                            response.Headers.Remove(HeaderNames.ETag);
                            return Task.FromResult(0);
                        },
                        context.Response);
                    }

                    var exceptionHandlerFeature = new ExceptionHandlerFeature()
                    {
                        Error = ex,
                    };

                    context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);

                    var exceptionContext = new ExceptionlessCoreHandlerError()
                    {
                        Context = context,
                        Exception = ex
                    };

                    await _exceptionCoreManager.CoreAsync(exceptionContext);

                    if (_diagnosticSource.IsEnabled("Microsoft.AspNet.Diagnostics.HandledException"))
                    {
                        _diagnosticSource.Write("Microsoft.AspNet.Diagnostics.HandledException", new { httpContext = context, exception = ex });
                    }

                    if (!_exceptionlessCoreOptions.RethrowException)
                    {
                        return;
                    }
                }
                catch (Exception ex2)
                {
                    // suppress secondary exceptions, re-throw the original.
                    //_logger.LogError("An exception was thrown attempting to execute the exception Core handler.", ex2);
                }

                throw; // re-throw original if requested.
            }
        }
    }
}
