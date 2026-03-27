#if NET10_0_OR_GREATER
using Exceptionless;
using Exceptionless.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class AspNetCoreExtensionsTests {
        [Fact]
        public void AddExceptionless_WhenCalled_RegistersAspNetCoreServices() {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            // Act
            builder.Services.AddExceptionless();

            // Assert
            Assert.Contains(builder.Services, descriptor => descriptor.ServiceType == typeof(IHttpContextAccessor));
            Assert.Contains(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IExceptionHandler) &&
                descriptor.ImplementationType == typeof(ExceptionlessExceptionHandler));
        }

        [Fact]
        public void AddExceptionless_WhenCalledTwice_DoesNotRegisterDuplicateExceptionHandlers() {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            // Act
            builder.Services.AddExceptionless();
            builder.Services.AddExceptionless();

            // Assert
            Assert.Single(builder.Services, descriptor =>
                descriptor.ServiceType == typeof(IExceptionHandler) &&
                descriptor.ImplementationType == typeof(ExceptionlessExceptionHandler));
        }
    }
}
#endif
