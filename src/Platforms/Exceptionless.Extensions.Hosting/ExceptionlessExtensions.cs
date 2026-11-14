using System;
using Exceptionless.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Exceptionless {
    public static class ExceptionlessExtensions {
        /// <summary>
        /// Ensures that the Exceptionless pending queue is processed before the host shuts down.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseExceptionless(this IHostBuilder hostBuilder) {
            return hostBuilder.ConfigureServices(delegate (HostBuilderContext context, IServiceCollection collection) {
                collection.AddExceptionlessLifetimeService();
            });
        }

        /// <summary>
        /// Ensures that the Exceptionless pending queue is processed before the host shuts down using the .NET 8+ builder API.
        /// </summary>
        public static IHostApplicationBuilder UseExceptionless(this IHostApplicationBuilder builder) {
            builder.Services.AddExceptionlessLifetimeService();
            return builder;
        }

        /// <summary>
        /// Adds the given pre-configured <see cref="ExceptionlessClient"/> to the host builder and registers lifecycle hooks.
        /// </summary>
        public static IHostApplicationBuilder AddExceptionless(this IHostApplicationBuilder builder, ExceptionlessClient client) {
            builder.Services.AddExceptionless(client);
            builder.Services.AddExceptionlessLifetimeService();
            return builder;
        }

        /// <summary>
        /// Adds an <see cref="ExceptionlessClient"/> to the host builder and registers lifecycle hooks.
        /// </summary>
        public static IHostApplicationBuilder AddExceptionless(this IHostApplicationBuilder builder, string apiKey) {
            builder.Services.AddExceptionless(apiKey);
            builder.Services.AddExceptionlessLifetimeService();
            return builder;
        }

        /// <summary>
        /// Adds an <see cref="ExceptionlessClient"/> to the host builder using the builder configuration and registers lifecycle hooks.
        /// </summary>
        public static IHostApplicationBuilder AddExceptionless(this IHostApplicationBuilder builder, Action<ExceptionlessConfiguration> configure = null) {
            builder.Services.AddExceptionless(builder.Configuration, configure);
            builder.Services.AddExceptionlessLifetimeService();
            return builder;
        }

        /// <summary>
        /// Adds the given pre-configured <see cref="ExceptionlessClient"/> to the services collection as a singleton.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ExceptionlessClient"/> instance to as a singleton.</param>
        /// <param name="client">The pre-configured <see cref="ExceptionlessClient"/> instance</param>
        /// <returns></returns>
        public static IServiceCollection AddExceptionless(this IServiceCollection services, ExceptionlessClient client) {
            return services.AddSingleton(client);
        }

        /// <summary>
        /// Adds an <see cref="ExceptionlessClient"/> instance to the services collection as a singleton using the specified api key.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ExceptionlessClient"/> instance to as a singleton.</param>
        /// <param name="apiKey">The Exceptionless api key to use.</param>
        /// <returns></returns>
        public static IServiceCollection AddExceptionless(this IServiceCollection services, string apiKey) {
            return AddExceptionless(services, c => c.ApiKey = apiKey);
        }

        /// <summary>
        /// Adds an <see cref="ExceptionlessClient"/> instance to the services collection as a singleton.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ExceptionlessClient"/> instance to as a singleton.</param>
        /// <param name="configure">Allows altering the <see cref="ExceptionlessClient"/> configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddExceptionless(this IServiceCollection services, Action<ExceptionlessConfiguration> configure = null) {
            return services.AddSingleton(sp => {
                return new ExceptionlessClient(clientConfiguration => {
                    var configuration = sp.GetService<IConfiguration>();
                    if (configuration != null)
                        clientConfiguration.ReadFromConfiguration(configuration);
                    else
                        clientConfiguration.ReadFromEnvironmentalVariables();

                    configure?.Invoke(clientConfiguration);
                });
            });
        }

        /// <summary>
        /// Adds an <see cref="ExceptionlessClient"/> instance to the services collection as a singleton.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ExceptionlessClient"/> instance to as a singleton.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> to use to configure the <see cref="ExceptionlessClient"/> instance.</param>
        /// <param name="configure">Allows altering the <see cref="ExceptionlessClient"/> configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddExceptionless(this IServiceCollection services, IConfiguration configuration, Action<ExceptionlessConfiguration> configure = null) {
            return services.AddSingleton(_ => {
                return new ExceptionlessClient(clientConfiguration => {
                    if (configuration != null)
                        clientConfiguration.ReadFromConfiguration(configuration);

                    configure?.Invoke(clientConfiguration);
                });
            });
        }

        private static IServiceCollection AddExceptionlessLifetimeService(this IServiceCollection services) {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(ExceptionlessLifetimeService)))
                return services;

            services.AddSingleton<ExceptionlessLifetimeService>();
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<ExceptionlessLifetimeService>());
            services.AddSingleton<IHostedLifecycleService>(sp => sp.GetRequiredService<ExceptionlessLifetimeService>());

            return services;
        }
    }
}
