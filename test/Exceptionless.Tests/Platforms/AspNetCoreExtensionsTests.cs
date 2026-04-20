#if NET10_0_OR_GREATER
using System;
using Exceptionless;
using Exceptionless.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class AspNetCoreExtensionsTests {
        [Fact]
        public void AddExceptionlessExceptionHandler_WhenCalled_RegistersAspNetCoreServices() {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            // Act
            builder.Services.AddExceptionlessExceptionHandler();

            // Assert
            Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IExceptionHandler) &&
                descriptor.ImplementationType == typeof(ExceptionlessExceptionHandler));
        }

        [Fact]
        public void AddExceptionlessExceptionHandler_WhenCalledTwice_DoesNotRegisterDuplicateExceptionHandlers() {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            // Act
            builder.Services.AddExceptionlessExceptionHandler();
            builder.Services.AddExceptionlessExceptionHandler();

            // Assert
            Assert.Single(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IExceptionHandler) &&
                descriptor.ImplementationType == typeof(ExceptionlessExceptionHandler));
        }

        [Fact]
        public void AddExceptionless_WhenCalledWithoutArguments_RegistersClientConfigurationServices() {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            // Act
            builder.Services.AddExceptionless();

            // Assert
            Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(ExceptionlessClient));
            Assert.DoesNotContain(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IExceptionHandler) &&
                descriptor.ImplementationType == typeof(ExceptionlessExceptionHandler));
        }

        [Fact]
        public void WebApplicationBuilder_AddExceptionless_RegistersExceptionHandlerAndResolvesFromDI() {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            // Act
            builder.AddExceptionless();

            // Assert — descriptors registered
            Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(ExceptionlessClient));
            Assert.Contains(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IExceptionHandler) &&
                descriptor.ImplementationType == typeof(ExceptionlessExceptionHandler));

            // Assert — actually resolves from the container
            using var provider = builder.Services.BuildServiceProvider();
            var handlers = provider.GetServices<IExceptionHandler>();
            Assert.Contains(handlers, h => h is ExceptionlessExceptionHandler);
        }

        [Fact]
        public void WebApplicationBuilder_AddExceptionless_WithApiKey_RegistersAndResolves() {
            var builder = WebApplication.CreateBuilder();
            builder.AddExceptionless("test-api-key");

            using var provider = builder.Services.BuildServiceProvider();
            var client = provider.GetRequiredService<ExceptionlessClient>();
            Assert.NotNull(client);

            var handlers = provider.GetServices<IExceptionHandler>();
            Assert.Contains(handlers, h => h is ExceptionlessExceptionHandler);
        }

        [Fact]
        public void WebApplicationBuilder_AddExceptionless_WithConfigure_RegistersAndResolves() {
            var builder = WebApplication.CreateBuilder();
            builder.AddExceptionless(c => c.DefaultData["test"] = "value");

            using var provider = builder.Services.BuildServiceProvider();
            var client = provider.GetRequiredService<ExceptionlessClient>();
            Assert.NotNull(client);

            var handlers = provider.GetServices<IExceptionHandler>();
            Assert.Contains(handlers, h => h is ExceptionlessExceptionHandler);
        }

        [Fact]
        public void WebApplicationBuilder_AddExceptionless_ThenManualCall_DoesNotDuplicate() {
            var builder = WebApplication.CreateBuilder();
            builder.AddExceptionless();
            builder.Services.AddExceptionlessExceptionHandler();

            Assert.Single(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IExceptionHandler) &&
                descriptor.ImplementationType == typeof(ExceptionlessExceptionHandler));
        }
    }
}
#endif
