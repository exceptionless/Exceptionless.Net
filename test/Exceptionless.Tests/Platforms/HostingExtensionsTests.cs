#if NET10_0_OR_GREATER
using System;
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Extensions.Hosting;
using Exceptionless.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class HostingExtensionsTests {
        [Fact]
        public void AddExceptionless_WhenCalled_RegistersClientAndLifetimeService() {
            // Arrange
            var builder = Host.CreateApplicationBuilder();

            // Act
            builder.AddExceptionless(configuration => configuration.ApiKey = "test-api-key");

            // Assert
            Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(ExceptionlessClient));
            Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(ExceptionlessLifetimeService));
            Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(IHostedService));
            Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(IHostedLifecycleService));

            using var serviceProvider = builder.Services.BuildServiceProvider();
            Assert.Same(
                serviceProvider.GetRequiredService<IHostedService>(),
                serviceProvider.GetRequiredService<IHostedLifecycleService>());
            Assert.Same(
                serviceProvider.GetRequiredService<ExceptionlessLifetimeService>(),
                serviceProvider.GetRequiredService<IHostedService>());
        }

        [Fact]
        public void UseExceptionless_WhenCalledTwice_DoesNotRegisterDuplicateLifetimeServices() {
            // Arrange
            var builder = Host.CreateApplicationBuilder();

            // Act
            builder.UseExceptionless();
            builder.UseExceptionless();

            // Assert
            Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(ExceptionlessLifetimeService));
            Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(IHostedService));
            Assert.Single(builder.Services, descriptor => descriptor.ServiceType == typeof(IHostedLifecycleService));
        }

        [Fact]
        public void AddExceptionless_CreatesIsolatedClientForEachHost() {
            var firstBuilder = Host.CreateApplicationBuilder();
            firstBuilder.AddExceptionless("first-api-key");
            var secondBuilder = Host.CreateApplicationBuilder();
            secondBuilder.AddExceptionless("second-api-key");

            using var firstProvider = firstBuilder.Services.BuildServiceProvider();
            using var secondProvider = secondBuilder.Services.BuildServiceProvider();
            var firstClient = firstProvider.GetRequiredService<ExceptionlessClient>();
            var secondClient = secondProvider.GetRequiredService<ExceptionlessClient>();

            Assert.NotSame(ExceptionlessClient.Default, firstClient);
            Assert.NotSame(firstClient, secondClient);
            Assert.Equal("first-api-key", firstClient.Configuration.ApiKey);
            Assert.Equal("second-api-key", secondClient.Configuration.ApiKey);
        }

        [Fact]
        public void DisposingHostProvider_DisposesHostOwnedClientButNotDefaultClient() {
            var builder = Host.CreateApplicationBuilder();
            builder.AddExceptionless();
            var serviceProvider = builder.Services.BuildServiceProvider();
            var client = serviceProvider.GetRequiredService<ExceptionlessClient>();

            serviceProvider.Dispose();

            Assert.Throws<ObjectDisposedException>(() => client.Configuration.Resolver.GetJsonSerializer());
            Assert.NotNull(ExceptionlessClient.Default.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void DisposingHostProvider_DoesNotDisposeCallerOwnedClient() {
            using var client = new ExceptionlessClient();
            var builder = Host.CreateApplicationBuilder();
            builder.AddExceptionless(client);
            var serviceProvider = builder.Services.BuildServiceProvider();
            serviceProvider.GetRequiredService<ExceptionlessClient>();

            serviceProvider.Dispose();

            Assert.NotNull(client.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void ConfiguredLoggingProvider_DoesNotMutateOrDisposeDefaultClient() {
            string dataKey = Guid.NewGuid().ToString("N");
            Assert.False(ExceptionlessClient.Default.Configuration.DefaultData.ContainsKey(dataKey));

            using (var provider = new ExceptionlessLoggerProvider(configuration => configuration.DefaultData[dataKey] = true))
                Assert.False(ExceptionlessClient.Default.Configuration.DefaultData.ContainsKey(dataKey));

            Assert.NotNull(ExceptionlessClient.Default.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void ConfiguredLoggingProvider_CanBeDisposedMoreThanOnce() {
            var provider = new ExceptionlessLoggerProvider(configuration => configuration.Enabled = false);

            provider.Dispose();
            provider.Dispose();
        }

        [Fact]
        public void LoggingProvider_RejectsNullClient() {
            Assert.Throws<ArgumentNullException>(() => new ExceptionlessLoggerProvider((ExceptionlessClient)null));
        }

        [Fact]
        public void LoggingProvider_CreatesLoggerWithoutOwningExplicitClient() {
            using var client = new ExceptionlessClient(configuration => configuration.Enabled = false);
            var provider = new ExceptionlessLoggerProvider(client);

            Assert.NotNull(provider.CreateLogger("test-category"));
            provider.Dispose();
            provider.Dispose();

            Assert.NotNull(client.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void ConfiguredLoggingProvider_AllowsNullConfigurationCallback() {
            using var provider = new ExceptionlessLoggerProvider((Action<ExceptionlessConfiguration>)null);
        }
    }
}
#endif
