using System;
using Exceptionless.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Exceptionless {
    public static class ExceptionlessExtensions {
        /// <summary>
        /// Ensures that the Exceptionless pending queue is processed before the host shuts down.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseExceptionless(this IHostBuilder hostBuilder) {
            return hostBuilder.ConfigureServices(delegate (HostBuilderContext context, IServiceCollection collection) {
                collection.AddSingleton<IHostedService, ExceptionlessLifetimeService>();
            });
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
                var client = ExceptionlessClient.Default;

                var config = sp.GetService<IConfiguration>();
                if (config != null)
                    client.Configuration.ReadFromConfiguration(config);
                else
                    client.Configuration.ReadFromEnvironmentalVariables();

                configure?.Invoke(client.Configuration);

                return client;
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
            return services.AddSingleton(sp => {
                var client = ExceptionlessClient.Default;

                if (configuration != null)
                    client.Configuration.ReadFromConfiguration(configuration);

                configure?.Invoke(client.Configuration);

                return client;
            });
        }
    }
}