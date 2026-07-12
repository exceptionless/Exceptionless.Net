#if NET10_0_OR_GREATER
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Extensions.Hosting;
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
        public void DisposingHostProvider_DoesNotDisposeDefaultClient() {
            // Arrange
            var builder = Host.CreateApplicationBuilder();
            builder.AddExceptionless();

            // Act
            var serviceProvider = builder.Services.BuildServiceProvider();
            serviceProvider.GetRequiredService<ExceptionlessClient>();
            serviceProvider.Dispose();

            // Assert
            Assert.NotNull(ExceptionlessClient.Default.Configuration.Resolver.GetJsonSerializer());
        }
    }
}
#endif
