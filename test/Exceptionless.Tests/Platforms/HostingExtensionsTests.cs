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
        public void AddExceptionless_WithMultipleHosts_CreatesIsolatedClients() {
            // Arrange
            var firstBuilder = Host.CreateApplicationBuilder();
            firstBuilder.AddExceptionless("first-api-key");
            var secondBuilder = Host.CreateApplicationBuilder();
            secondBuilder.AddExceptionless("second-api-key");

            // Act
            using var firstProvider = firstBuilder.Services.BuildServiceProvider();
            using var secondProvider = secondBuilder.Services.BuildServiceProvider();
            var firstClient = firstProvider.GetRequiredService<ExceptionlessClient>();
            var secondClient = secondProvider.GetRequiredService<ExceptionlessClient>();

            // Assert
            Assert.NotSame(ExceptionlessClient.Default, firstClient);
            Assert.NotSame(firstClient, secondClient);
            Assert.Equal("first-api-key", firstClient.Configuration.ApiKey);
            Assert.Equal("second-api-key", secondClient.Configuration.ApiKey);
        }

        [Fact]
        public void ConfiguredLoggingProvider_WhenDisposed_PreservesDefaultClient() {
            // Arrange
            string dataKey = Guid.NewGuid().ToString("N");
            bool containedBefore = ExceptionlessClient.Default.Configuration.DefaultData.ContainsKey(dataKey);

            // Act
            using (var provider = new ExceptionlessLoggerProvider(configuration => configuration.DefaultData[dataKey] = true))
                provider.CreateLogger("test-category");

            // Assert
            Assert.False(containedBefore);
            Assert.False(ExceptionlessClient.Default.Configuration.DefaultData.ContainsKey(dataKey));
            Assert.NotNull(ExceptionlessClient.Default.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void ConfiguredLoggingProvider_WhenDisposedRepeatedly_RemainsSafe() {
            // Arrange
            var provider = new ExceptionlessLoggerProvider(configuration => configuration.Enabled = false);

            // Act
            Exception exception = Record.Exception(() => {
                provider.Dispose();
                provider.Dispose();
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void ConfiguredLoggingProvider_WithNullCallback_CreatesProvider() {
            // Arrange
            Action<ExceptionlessConfiguration> callback = null;

            // Act
            using var provider = new ExceptionlessLoggerProvider(callback);

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public void Dispose_WithCallerOwnedClient_PreservesClient() {
            // Arrange
            using var client = new ExceptionlessClient();
            var builder = Host.CreateApplicationBuilder();
            builder.AddExceptionless(client);
            var serviceProvider = builder.Services.BuildServiceProvider();
            serviceProvider.GetRequiredService<ExceptionlessClient>();

            // Act
            serviceProvider.Dispose();

            // Assert
            Assert.NotNull(client.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void Dispose_WithHostOwnedClient_DisposesOnlyHostClient() {
            // Arrange
            var builder = Host.CreateApplicationBuilder();
            builder.AddExceptionless();
            var serviceProvider = builder.Services.BuildServiceProvider();
            var client = serviceProvider.GetRequiredService<ExceptionlessClient>();

            // Act
            serviceProvider.Dispose();
            Exception exception = Record.Exception(() => client.Configuration.Resolver.GetJsonSerializer());

            // Assert
            Assert.IsType<ObjectDisposedException>(exception);
            Assert.NotNull(ExceptionlessClient.Default.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void LoggingProvider_WithExplicitClient_CreatesLoggerWithoutOwningClient() {
            // Arrange
            using var client = new ExceptionlessClient(configuration => configuration.Enabled = false);
            var provider = new ExceptionlessLoggerProvider(client);

            // Act
            var logger = provider.CreateLogger("test-category");
            provider.Dispose();
            provider.Dispose();

            // Assert
            Assert.NotNull(logger);
            Assert.NotNull(client.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void LoggingProvider_WithNullClient_ThrowsArgumentNullException() {
            // Arrange
            ExceptionlessClient client = null;

            // Act
            Exception exception = Record.Exception(() => new ExceptionlessLoggerProvider(client));

            // Assert
            Assert.IsType<ArgumentNullException>(exception);
        }
    }
}
#endif
