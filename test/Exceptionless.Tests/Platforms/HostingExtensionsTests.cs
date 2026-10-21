#if NET10_0_OR_GREATER
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Configuration;
using Exceptionless.Dependency;
using Exceptionless.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Exceptionless.Tests.Platforms {
    public class HostingExtensionsTests {
        [Theory]
        [InlineData(null, "Staging")]
        [InlineData("production", "production")]
        [InlineData("", null)]
        [InlineData(" ", null)]
        [InlineData("prod\ninvalid", null)]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", null)]
        public void AddExceptionless_DeploymentEnvironment_UsesHostAsFallback(string? configuredEnvironment, string? expectedEnvironment) {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Staging" });
            var client = new ExceptionlessClient();
            client.Configuration.Environment = configuredEnvironment;
            builder.AddExceptionless(client);

            using var services = builder.Services.BuildServiceProvider();
            Assert.Equal(expectedEnvironment, services.GetRequiredService<ExceptionlessClient>().Configuration.Environment);
        }

        [Fact]
        public void AddExceptionless_HostFallback_DoesNotBecomeExplicitConfiguration() {
            using var client = new ExceptionlessClient();
            var staging = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Staging" });
            staging.AddExceptionless(client);
            Assert.Equal("Staging", client.Configuration.Environment);
            Assert.False(client.Configuration.IsEnvironmentConfigured);

            var production = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Production" });
            production.AddExceptionless(client);
            Assert.Equal("Production", client.Configuration.Environment);
            Assert.False(client.Configuration.IsEnvironmentConfigured);
        }

        [Fact]
        public void ReadFromConfiguration_Environment_LoadsTrimmedValueAndPreservesMissingSetting() {
            // Arrange
            using var client = new ExceptionlessClient();
            client.Configuration.Environment = "staging";
            var missing = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Exceptionless:ApiKey"] = "test-api-key" }).Build();
            var configured = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Exceptionless:Environment"] = " Production " }).Build();

            // Act
            client.Configuration.ReadFromConfiguration(missing);
            string? afterMissing = client.Configuration.Environment;
            client.Configuration.ReadFromConfiguration(configured);

            // Assert
            Assert.Equal("staging", afterMissing);
            Assert.Equal("Production", client.Configuration.Environment);
            Assert.True(client.Configuration.IsEnvironmentConfigured);
        }

        [Theory]
        [InlineData(" Staging ", "Staging")]
        [InlineData(null, "Production")]
        [InlineData("", null)]
        public void AddExceptionless_ConfigurationEnvironment_TakesPrecedenceOverHost(string? configuredEnvironment, string? expectedEnvironment) {
            // Arrange
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Production" });
            if (configuredEnvironment != null)
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Exceptionless:Environment"] = configuredEnvironment });

            try {
                // Act
                builder.Services.AddExceptionless(builder.Configuration);
                using var services = builder.Services.BuildServiceProvider();
                var client = services.GetRequiredService<ExceptionlessClient>();

                // Assert
                Assert.Equal(expectedEnvironment, client.Configuration.Environment);
            } finally {
                ExceptionlessClient.Default.Configuration.Environment = null;
            }
        }

        [Fact]
        public void AddExceptionless_ConfigureCallback_TakesPrecedenceOverConfigurationAndHost() {
            // Arrange
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Production" });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Exceptionless:Environment"] = "staging" });

            try {
                // Act
                builder.Services.AddExceptionless(configuration => configuration.SetEnvironment("qa-west"));
                using var services = builder.Services.BuildServiceProvider();
                var client = services.GetRequiredService<ExceptionlessClient>();

                // Assert
                Assert.Equal("qa-west", client.Configuration.Environment);
            } finally {
                ExceptionlessClient.Default.Configuration.Environment = null;
            }
        }

        [Fact]
        public void AddExceptionless_ProvidedClient_RemainsOwnedByCaller() {
            var resolver = new Mock<IDependencyResolver>();
            using var client = new ExceptionlessClient(new ExceptionlessConfiguration(resolver.Object));
            var builder = Host.CreateApplicationBuilder();
            builder.AddExceptionless(client);

            using (var services = builder.Services.BuildServiceProvider()) {
                Assert.Same(client, services.GetRequiredService<ExceptionlessClient>());
            }

            resolver.Verify(r => r.Dispose(), Times.Never);
        }

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
    }
}
#endif
