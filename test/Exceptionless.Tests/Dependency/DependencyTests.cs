using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exceptionless.Tests.Dependency {
    public partial class DependencyTests {
        [Fact]
        public void CanRegisterAndResolveTypes() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IServiceA, ServiceA>();
            var s1 = resolver.Resolve<IServiceA>();
            var s2 = resolver.Resolve<IServiceA>();
            Assert.Equal(s1, s2);
        }

        [Fact]
        public void CanResolveUnregisteredType() {
            var resolver = new DefaultDependencyResolver();
            var s1 = resolver.Resolve<ServiceA>();
            Assert.NotNull(s1);
        }

        [Fact]
        public void CanInjectConstructors() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IServiceA, ServiceA>();
            resolver.Register<IServiceB, ServiceB>();
            var a = resolver.Resolve<IServiceA>();
            var b = resolver.Resolve<IServiceB>();
            Assert.Equal(a, b.ServiceA);
            Assert.NotNull(b.ServiceC);
        }

        [Fact]
        public void CanHaveIsolatedContainers() {
            var resolver1 = new DefaultDependencyResolver();
            var resolver2 = new DefaultDependencyResolver();
            resolver1.Register<IServiceA, ServiceA>();
            resolver2.Register<IServiceA, ServiceA>();
            var s1 = resolver1.Resolve<IServiceA>();
            var s2 = resolver2.Resolve<IServiceA>();
            Assert.NotEqual(s1, s2);
        }

        [Fact]
        public void CreateDefault_WithCustomService_OverridesClientDefault() {
            // Arrange
            var serializer = new DefaultJsonSerializer();
            var services = new ServiceCollection();
            services.AddSingleton<IJsonSerializer>(serializer);

            // Act
            using var client = new ExceptionlessClient(services);

            // Assert
            Assert.Same(serializer, client.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void Dispose_WithContainerOwnedSingleton_DisposesInstance() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposableService, DisposableService>();
            var service = resolver.Resolve<IDisposableService>();

            // Act
            resolver.Dispose();

            // Assert
            Assert.True(service.IsDisposed);
        }

        [Fact]
        public void Dispose_WithExplicitInstance_PreservesInstance() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            var service = new DisposableService();
            resolver.Register<IDisposableService>(service);
            resolver.Resolve<IDisposableService>();

            // Act
            resolver.Dispose();

            // Assert
            Assert.False(service.IsDisposed);
        }

        [Fact]
        public void Dispose_WithProviderSnapshots_DisposesEachSnapshotOnce() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<ICountingDisposable, CountingDisposable>();
            var original = resolver.Resolve<ICountingDisposable>();
            resolver.Register<IServiceA, ServiceA>();
            var replacement = resolver.Resolve<ICountingDisposable>();

            // Act
            resolver.Dispose();

            // Assert
            Assert.NotSame(original, replacement);
            Assert.Equal(1, original.DisposeCount);
            Assert.Equal(1, replacement.DisposeCount);
        }

        [Fact]
        public void Register_AfterInitialResolution_ReplacesRegistration() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IServiceA, ServiceA>();
            var original = resolver.Resolve<IServiceA>();

            // Act
            resolver.Register<IServiceA, AlternateServiceA>();
            var replacement = resolver.Resolve<IServiceA>();

            // Assert
            Assert.IsType<AlternateServiceA>(replacement);
            Assert.NotSame(original, replacement);
        }

        [Fact]
        public void Resolve_AfterLateReplacement_CreatesConsistentServiceGraph() {
            // Arrange
            using var resolver = new DefaultDependencyResolver();
            resolver.Register<IServiceA, ServiceA>();
            resolver.Register<IServiceB, ServiceB>();
            var original = resolver.Resolve<IServiceB>();

            // Act
            resolver.Register<IServiceA, AlternateServiceA>();
            var replacement = resolver.Resolve<IServiceB>();

            // Assert
            Assert.IsType<ServiceA>(original.ServiceA);
            Assert.IsType<AlternateServiceA>(replacement.ServiceA);
            Assert.NotSame(original, replacement);
        }

        [Fact]
        public void Resolve_WithFactoryRegistration_ReturnsTransientInstances() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IServiceA), () => new ServiceA());

            // Act
            var first = resolver.Resolve<IServiceA>();
            var second = resolver.Resolve<IServiceA>();

            // Assert
            Assert.NotSame(first, second);
        }

        [Fact]
        public void Resolve_WithMultipleServiceCollectionRegistrations_ReturnsAllServices() {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IServiceA, ServiceA>();
            services.AddSingleton<IServiceA, AlternateServiceA>();
            using var resolver = new DefaultDependencyResolver(services);

            // Act
            var registrations = resolver.Resolve<IEnumerable<IServiceA>>().ToArray();

            // Assert
            Assert.Collection(
                registrations,
                service => Assert.IsType<ServiceA>(service),
                service => Assert.IsType<AlternateServiceA>(service));
        }

        [Fact]
        public void Resolve_WithOpenGenericRegistration_ReturnsClosedService() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IGenericService<>), typeof(GenericService<>));

            // Act
            var service = resolver.Resolve(typeof(IGenericService<string>));
            var repeated = resolver.Resolve(typeof(IGenericService<string>));

            // Assert
            Assert.IsType<GenericService<string>>(service);
            Assert.Same(service, repeated);
        }

        [Theory]
        [InlineData(ServiceLifetime.Singleton)]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        public void Resolve_WithKeyedImplementation_InjectsServiceKey(ServiceLifetime lifetime) {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton("ordinary-dependency");
            services.AddSingleton<IServiceA, ServiceA>();
            services.Add(ServiceDescriptor.DescribeKeyed(typeof(IKeyAwareService), "expected-key", typeof(KeyAwareService), lifetime));
            using var resolver = new DefaultDependencyResolver(services);
            var keyedProvider = resolver.Resolve<IKeyedServiceProvider>();

            // Act
            var service = keyedProvider.GetRequiredKeyedService<IKeyAwareService>("expected-key");

            // Assert
            Assert.Equal("expected-key", service.ServiceKey);
            Assert.Equal("ordinary-dependency", service.Label);
            Assert.NotNull(service.Provider.GetService(typeof(IServiceA)));
        }

        [Fact]
        public void Resolve_WithKeyedFactoryDependingOnDifferentKey_DoesNotReportCircularDependency() {
            // Arrange
            var services = new ServiceCollection();
            services.AddKeyedTransient<IServiceA, ServiceA>("inner");
            services.AddKeyedTransient<IServiceA>("outer", (provider, _) => provider.GetRequiredKeyedService<IServiceA>("inner"));
            using var resolver = new DefaultDependencyResolver(services);
            var keyedProvider = resolver.Resolve<IKeyedServiceProvider>();

            // Act
            Exception exception = Record.Exception(() => keyedProvider.GetRequiredKeyedService<IServiceA>("outer"));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Resolve_WithKeyedFactoryDependingOnUnkeyedService_DoesNotReportCircularDependency() {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<IServiceA, ServiceA>();
            services.AddKeyedTransient<IServiceA>("outer", (provider, _) => provider.GetRequiredService<IServiceA>());
            using var resolver = new DefaultDependencyResolver(services);
            var keyedProvider = resolver.Resolve<IKeyedServiceProvider>();

            // Act
            Exception exception = Record.Exception(() => keyedProvider.GetRequiredKeyedService<IServiceA>("outer"));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Resolve_WithKeyedFactoryDependingOnSameKey_ReportsCircularDependency() {
            // Arrange
            var services = new ServiceCollection();
            services.AddKeyedTransient<IServiceA>("cycle", (provider, _) => provider.GetRequiredKeyedService<IServiceA>("cycle"));
            using var resolver = new DefaultDependencyResolver(services);
            var keyedProvider = resolver.Resolve<IKeyedServiceProvider>();

            // Act
            Exception exception = Record.Exception(() => keyedProvider.GetRequiredKeyedService<IServiceA>("cycle"));

            // Assert
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("circular dependency", invalidOperationException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(ProviderOpenGenericImplementations))]
        public void Resolve_WithOpenGenericProviderDependency_PreservesNativeConstructorInjection(Type implementationType) {
            // Arrange
            using var resolver = new DefaultDependencyResolver();
            var services = new ServiceCollection();
            services.AddSingleton(typeof(IProviderGenericService<>), implementationType);

            // Act
            resolver.Register(typeof(IProviderGenericService<>), implementationType);
            using var collectionResolver = new DefaultDependencyResolver(services);
            object direct = resolver.Resolve(typeof(IProviderGenericService<string>));
            object fromCollection = collectionResolver.Resolve(typeof(IProviderGenericService<string>));

            // Assert
            Assert.Equal(implementationType.MakeGenericType(typeof(string)), direct.GetType());
            Assert.Equal(direct.GetType(), fromCollection.GetType());
            Assert.NotNull(((IProviderGenericService<string>)direct).ProviderDependency);
            Assert.NotNull(((IProviderGenericService<string>)fromCollection).ProviderDependency);
        }

        public static TheoryData<Type> ProviderOpenGenericImplementations => new TheoryData<Type> {
            typeof(ProviderGenericService<>),
            typeof(KeyedProviderGenericService<>),
            typeof(ScopeFactoryGenericService<>)
        };

        [Fact]
        public void Resolve_WithServiceCollectionRegistration_ReturnsRegisteredService() {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IServiceA, AlternateServiceA>();
            var resolver = new DefaultDependencyResolver(services);

            // Act
            var service = resolver.Resolve<IServiceA>();

            // Assert
            Assert.IsType<AlternateServiceA>(service);
        }
    }

    public interface IServiceA {
        void DoWork();
    }

    public class ServiceA : IServiceA {
        public void DoWork() {}
    }

    public class AlternateServiceA : IServiceA {
        public void DoWork() { }
    }

    public interface IServiceB {
        void DoWork();
        IServiceA ServiceA { get; }
        ServiceC ServiceC { get; }
    }

    public class ServiceB : IServiceB {
        public ServiceB(IServiceA serviceA, ServiceC serviceC) {
            ServiceA = serviceA;
            ServiceC = serviceC;
        }

        public void DoWork() { }

        public IServiceA ServiceA { get; private set; }
        public ServiceC ServiceC { get; private set; }
    }

    public class ServiceC {}

    public interface IGenericService<T> { }

    public class GenericService<T> : IGenericService<T> { }

    public interface IKeyAwareService {
        string ServiceKey { get; }
        string Label { get; }
        IServiceProvider Provider { get; }
    }

    public class KeyAwareService : IKeyAwareService {
        public KeyAwareService([ServiceKey] string serviceKey, string label, IServiceProvider provider) {
            ServiceKey = serviceKey;
            Label = label;
            Provider = provider;
        }

        public string ServiceKey { get; }
        public string Label { get; }
        public IServiceProvider Provider { get; }
    }

    public interface IProviderGenericService<T> {
        object ProviderDependency { get; }
    }

    public class ProviderGenericService<T> : IProviderGenericService<T> {
        public ProviderGenericService(IServiceProvider provider) { ProviderDependency = provider; }
        public object ProviderDependency { get; }
    }

    public class KeyedProviderGenericService<T> : IProviderGenericService<T> {
        public KeyedProviderGenericService(IKeyedServiceProvider provider) { ProviderDependency = provider; }
        public object ProviderDependency { get; }
    }

    public class ScopeFactoryGenericService<T> : IProviderGenericService<T> {
        public ScopeFactoryGenericService(IServiceScopeFactory scopeFactory) { ProviderDependency = scopeFactory; }
        public object ProviderDependency { get; }
    }

}
