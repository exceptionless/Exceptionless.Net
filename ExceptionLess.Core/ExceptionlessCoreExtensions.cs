using ExceptionLess.Core.Interfaces;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExceptionLess.Core
{
    public static class ExceptionlessCoreExtensions
    {
        /// <summary>
        /// Adds the exception filter manager.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddExceptionlessCorePlugIn(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(ExceptionlessCorePlugIn));
            return services;
        }

        /// <summary>
        /// Uses the exception intercept manager.
        /// </summary>
        /// <param name="built">The builder.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionCoreManager(this IApplicationBuilder builder, ExceptionlessCoreOptions options = null)
        {
            var middlewareOptions = options ?? new ExceptionlessCoreOptions()
            {
                RethrowException = false
            };

            return builder.UseMiddleware<ExceptionlessCoreMiddleware>(middlewareOptions);
        }

        /// <summary>
        /// Adds an exception intercept handler.
        /// </summary>
        /// <param name="built">The builder.</param>
        /// <param name="exceptionFilter">The exception handler.</param>
        /// <returns></returns>
        public static IApplicationBuilder AddExceptionlessCoreHandlerError(this IApplicationBuilder builder, IExceptionlessCoreHandlerError exceptionlessCoreHandlerError)
        {
            var exceptionlessCorePlugIn = builder.ApplicationServices.GetService<ExceptionlessCorePlugIn>();
            if (exceptionlessCorePlugIn != null)
            {
                exceptionlessCorePlugIn.AddExceptionlessCoreHandlerError(exceptionlessCoreHandlerError);
            }

            return builder;
        }

        /// <summary>
        /// Adds an exception intercept handler.
        /// </summary>
        /// <param name="built">The builder.</param>
        /// <param name="exceptionFilterType">Type of the exception handler.</param>
        /// <returns></returns>
        public static IApplicationBuilder AddExceptionlessCoreHandlerError(this IApplicationBuilder builder, Type exceptionFilterType)
        {
            var exceptionlessCoreHandlerError = builder.ApplicationServices.GetService(exceptionFilterType);
            if (exceptionlessCoreHandlerError != null)
            {
                var handler = exceptionlessCoreHandlerError as IExceptionlessCoreHandlerError;
                if (handler != null)
                {
                    builder.AddExceptionlessCoreHandlerError(handler);
                }
            }

            return builder;
        }

        /// <summary>
        /// Adds an exception intercept handler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="built">The builder.</param>
        /// <returns></returns>
        public static IApplicationBuilder AddExceptionlessCoreHandlerError<T>(this IApplicationBuilder builder) where T : IExceptionlessCoreHandlerError
        {
            var exceptionlessCoreHandlerError = builder.ApplicationServices.GetService<T>();
            if (exceptionlessCoreHandlerError != null)
            {
                builder.AddExceptionlessCoreHandlerError(exceptionlessCoreHandlerError);
            }

            return builder;
        }
    }
}
