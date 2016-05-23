using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ExceptionLess.Core;
using ExceptionLess.Core.Interfaces;

namespace ExceptionLess.Core
{
    public static class ExceptionlessCoreExtensions
    {
        /// <summary>
        /// Adds the AspNetCore service.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddExceptionlessCorePlugIn(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(ExceptionlessCorePlugIn));
            return services;
        }

        /// <summary>
        /// Uses the AspNetCore App Builder.
        /// </summary>
        /// <param name="built">The builder.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionCorePlugIn(this IApplicationBuilder builder, ExceptionlessCoreOptions options = null)
        {
            var middlewareOptions = options ?? new ExceptionlessCoreOptions()
            {
                RethrowException = false
            };

            return builder.UseMiddleware<ExceptionlessCoreMiddleware>(middlewareOptions);
        }

        /// <summary>
        /// Adds an AspNetCore exception handler.
        /// </summary>
        /// <param name="built">The builder.</param>
        /// <param name="exceptionlessCoreErrorHandler">The exception handler.</param>
        /// <returns></returns>
        public static IApplicationBuilder AddExceptionlessCoreHandlerError(this IApplicationBuilder builder, IExceptionlessCoreErrorHandler exceptionlessCoreErrorHandler)
        {
            var exceptionlessCorePlugIn = builder.ApplicationServices.GetService<ExceptionlessCorePlugIn>();
            if (exceptionlessCorePlugIn != null)
            {
                exceptionlessCorePlugIn.AddExceptionlessCoreHandlerError(exceptionlessCoreErrorHandler);
            }

            return builder;
        }

        /// <summary>
        /// Adds an AspNetCore exception handler.
        /// </summary>
        /// <param name="built">The builder.</param>
        /// <param name="exceptionFilterType">Type of the exception handler.</param>
        /// <returns></returns>
        public static IApplicationBuilder AddExceptionlessCoreHandlerError(this IApplicationBuilder builder, Type exceptionFilterType)
        {
            var exceptionlessCoreHandlerError = builder.ApplicationServices.GetService(exceptionFilterType);
            if (exceptionlessCoreHandlerError != null)
            {
                var handler = exceptionlessCoreHandlerError as IExceptionlessCoreErrorHandler;
                if (handler != null)
                {
                    builder.AddExceptionlessCoreHandlerError(handler);
                }
            }

            return builder;
        }

        /// <summary>
        /// Adds an AspNetCore exception handler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="built">The builder.</param>
        /// <returns></returns>
        public static IApplicationBuilder AddExceptionlessCoreHandlerError<T>(this IApplicationBuilder builder) where T : IExceptionlessCoreErrorHandler
        {
            var exceptionlessCoreErrorHandler = builder.ApplicationServices.GetService<T>();
            if (exceptionlessCoreErrorHandler != null)
            {
                builder.AddExceptionlessCoreHandlerError(exceptionlessCoreErrorHandler);
            }

            return builder;
        }
    }
}
