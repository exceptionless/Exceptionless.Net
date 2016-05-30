﻿using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ExceptionLess.AspNetCore
    {
    public class ExceptionlessCoreMiddleware
    {
        
        private readonly RequestDelegate _next;
        private readonly ExceptionlessCoreOptions _exceptionlessCoreOptions;
        private readonly DiagnosticSource _diagnosticSource;
        private readonly ExceptionlessCorePlugIn _exceptionlessCorePlugIn;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessCoreMiddleware"/> class.
        /// </summary>        
        /// <param name="next">The next.</param>        
        /// <param name="diagnosticSource">The diagnostic source.</param>
        /// /// <param name="exceptionlessCoreOptions">The exception Core options.</param>
        /// <param name="exceptionlessCorePlugIn">The exception Core manager.</param>
        public ExceptionlessCoreMiddleware(            
            RequestDelegate next,
            ExceptionlessCoreOptions exceptionlessCoreOptions,
            DiagnosticSource diagnosticSource,
            ExceptionlessCorePlugIn exceptionlessCorePlugIn)
        {
            _next = next;
            _exceptionlessCoreOptions = exceptionlessCoreOptions;
            _diagnosticSource = diagnosticSource;
            _exceptionlessCorePlugIn = exceptionlessCorePlugIn;
            
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

                    await _exceptionlessCorePlugIn.CoreAsync(exceptionContext);

                    if (_diagnosticSource.IsEnabled("Microsoft.AspNet.Diagnostics.HandledException"))
                    {
                        _diagnosticSource.Write("Microsoft.AspNet.Diagnostics.HandledException", new { httpContext = context, exception = ex });
                    }

                    if (!_exceptionlessCoreOptions.RethrowException)
                    {
                        return;
                    }
                }
                catch (Exception ex1)
                {
                    // suppress secondary exceptions, re-throw the original.
                    //_logger.LogError("An exception was thrown attempting to execute the exception Core handler.", ex2);
                    Console.WriteLine(ex1.ToString());

                    }

                throw; // re-throw original if requested.
            }
        }
    }
}
