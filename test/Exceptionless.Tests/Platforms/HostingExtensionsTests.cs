#if NET10_0_OR_GREATER
using System.Linq;
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
            Assert.Contains(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(ExceptionlessLifetimeService));
        }

        [Fact]
        public void UseExceptionless_WhenCalledTwice_DoesNotRegisterDuplicateLifetimeServices() {
            // Arrange
            var builder = Host.CreateApplicationBuilder();

            // Act
            builder.UseExceptionless();
            builder.UseExceptionless();

            // Assert
            Assert.Single(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(ExceptionlessLifetimeService));
        }
    }
}
#endif
