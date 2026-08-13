using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exceptionless.Tests.Dependency {
    public class DependencyTests {
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
        public void Resolve_WithCircularDependencies_ThrowsInvalidOperationException() {
            // Arrange
            using var resolver = new DefaultDependencyResolver();
            resolver.Register<ICircularServiceA, CircularServiceA>();
            resolver.Register<ICircularServiceB, CircularServiceB>();

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<ICircularServiceA>());

            // Assert
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("circular dependency", invalidOperationException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Resolve_WithCircularFactory_ThrowsInvalidOperationException() {
            // Arrange
            using var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IServiceA), () => resolver.Resolve<IServiceA>());

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<IServiceA>());

            // Assert
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("circular dependency", invalidOperationException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Resolve_WithConcreteRegistration_ReturnsTransientInstances() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<ServiceA>();

            // Act
            var first = resolver.Resolve<ServiceA>();
            var second = resolver.Resolve<ServiceA>();

            // Assert
            Assert.NotSame(first, second);
        }

        [Fact]
        public async Task Resolve_WithConcurrentProviderResolution_DoesNotReportCircularDependency() {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ServiceProviderConsumer>();
            using var resolver = new DefaultDependencyResolver(services);
            using var firstEntered = new ManualResetEventSlim();
            using var bothEntered = new CountdownEvent(2);
            using var release = new ManualResetEventSlim();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            int activations = 0;
            resolver.Register(typeof(IServiceA), () => {
                if (Interlocked.Increment(ref activations) == 1)
                    firstEntered.Set();

                bothEntered.Signal();
                release.Wait(TimeSpan.FromSeconds(5), cancellationToken);
                return new ServiceA();
            });
            IServiceProvider serviceProvider = resolver.Resolve<ServiceProviderConsumer>().ServiceProvider;

            // Act
            Task<object> first = Task.Run(() => serviceProvider.GetService(typeof(IServiceA)));
            bool firstEnteredInTime = firstEntered.Wait(TimeSpan.FromSeconds(5), cancellationToken);
            Task<object> second = Task.Run(() => serviceProvider.GetService(typeof(IServiceA)));
            bool enteredConcurrently = bothEntered.Wait(TimeSpan.FromSeconds(5), cancellationToken);
            release.Set();
            object[] resolved = null;
            Exception exception = await Record.ExceptionAsync(async () => resolved = await Task.WhenAll(first, second));

            // Assert
            Assert.True(firstEnteredInTime);
            Assert.True(enteredConcurrently);
            Assert.Null(exception);
            Assert.Equal(2, activations);
            Assert.All(resolved, Assert.NotNull);
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
        public void Resolve_WithIServiceProviderDependency_ReturnsUsableProvider() {
            // Arrange
            using var resolver = new DefaultDependencyResolver();
            resolver.Register<IServiceA, ServiceA>();

            // Act
            var consumer = resolver.Resolve<ServiceProviderConsumer>();
            var service = consumer.ServiceProvider.GetService(typeof(IServiceA));

            // Assert
            Assert.Same(resolver.Resolve<IServiceA>(), service);
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

    public class ServiceProviderConsumer {
        public ServiceProviderConsumer(IServiceProvider serviceProvider) {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }

    public interface IGenericService<T> { }

    public class GenericService<T> : IGenericService<T> { }

    public interface IDisposableService {
        bool IsDisposed { get; }
    }

    public class DisposableService : IDisposableService, IDisposable {
        public bool IsDisposed { get; private set; }

        public void Dispose() {
            IsDisposed = true;
        }
    }

    public interface ICountingDisposable {
        int DisposeCount { get; }
    }

    public class CountingDisposable : ICountingDisposable, IDisposable {
        public int DisposeCount { get; private set; }

        public void Dispose() {
            DisposeCount++;
        }
    }

    public interface ICircularServiceA { }

    public interface ICircularServiceB { }

    public class CircularServiceA : ICircularServiceA {
        public CircularServiceA(ICircularServiceB service) { }
    }

    public class CircularServiceB : ICircularServiceB {
        public CircularServiceB(ICircularServiceA service) { }
    }
}
