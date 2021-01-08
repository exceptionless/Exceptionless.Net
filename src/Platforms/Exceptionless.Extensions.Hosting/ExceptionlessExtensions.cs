using System;
using System.Reflection;
using Exceptionless.Dependency;
using Exceptionless.Extensions.Hosting;
using Exceptionless.Logging;
using Exceptionless.Serializer;
using Exceptionless.Storage;
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

        /// <summary>
        /// Sets the configuration from .net configuration settings.
        /// </summary>
        /// <param name="config">The configuration object you want to apply the settings to.</param>
        /// <param name="settings">The configuration settings</param>
        public static void ReadFromConfiguration(this ExceptionlessConfiguration config, IConfiguration settings) {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var section = settings.GetSection("Exceptionless");
            if (Boolean.TryParse(section["Enabled"], out bool enabled) && !enabled)
                config.Enabled = false;
            
            string apiKey = section["ApiKey"];
            if (!String.IsNullOrEmpty(apiKey) && apiKey != "API_KEY_HERE")
                config.ApiKey = apiKey;

            string serverUrl = section["ServerUrl"];
            if (!String.IsNullOrEmpty(serverUrl))
                config.ServerUrl = serverUrl;
            
            if (TimeSpan.TryParse(section["QueueMaxAge"], out var queueMaxAge))
                config.QueueMaxAge = queueMaxAge;

            if (Int32.TryParse(section["QueueMaxAttempts"], out int queueMaxAttempts))
                config.QueueMaxAttempts = queueMaxAttempts;
            
            string storagePath = section["StoragePath"];
            if (!String.IsNullOrEmpty(storagePath))
                config.Resolver.Register(typeof(IObjectStorage), () => new FolderObjectStorage(config.Resolver, storagePath));

            string storageSerializer = section["StorageSerializer"];
            if (!String.IsNullOrEmpty(storageSerializer)) {
                try {
                    var serializerType = Type.GetType(storageSerializer);
                    if (!typeof(IStorageSerializer).GetTypeInfo().IsAssignableFrom(serializerType)) {
                        config.Resolver.GetLog().Error(typeof(ExceptionlessConfigurationExtensions), $"The storage serializer {storageSerializer} does not implemented interface {typeof(IStorageSerializer)}.");
                    } else {
                        config.Resolver.Register(typeof(IStorageSerializer), serializerType);
                    }
                } catch (Exception ex) {
                    config.Resolver.GetLog().Error(typeof(ExceptionlessConfigurationExtensions), ex, $"The storage serializer {storageSerializer} type could not be resolved: ${ex.Message}");
                }
            }
            
            if (Boolean.TryParse(section["EnableLogging"], out bool enableLogging) && enableLogging) {
                string logPath = section["LogPath"];
                if (!String.IsNullOrEmpty(logPath))
                    config.UseFileLogger(logPath);
                else if (!String.IsNullOrEmpty(storagePath))
                    config.UseFileLogger(System.IO.Path.Combine(storagePath, "exceptionless.log"));
            }
            
            if (Boolean.TryParse(section["IncludePrivateInformation"], out bool includePrivateInformation) && !includePrivateInformation)
                config.IncludePrivateInformation = false;

            if (Boolean.TryParse(section["ProcessQueueOnCompletedRequest"], out bool processQueueOnCompletedRequest) && processQueueOnCompletedRequest)
                config.ProcessQueueOnCompletedRequest = true;

            foreach (var tag in section.GetSection("DefaultTags").GetChildren())
                config.DefaultTags.Add(tag.Value);
            
            foreach (var data in section.GetSection("DefaultData").GetChildren())
                if (data.Value != null)
                    config.DefaultData[data.Key] = data.Value;
            
            foreach (var setting in section.GetSection("Settings").GetChildren())
                if (setting.Value != null)
                    config.Settings[setting.Key] = setting.Value;
            
            // TODO: Support Registrations
        }
    }
}