using System;
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
        public void ConcreteRegistrationsAreTransient() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<ServiceA>();

            Assert.NotSame(resolver.Resolve<ServiceA>(), resolver.Resolve<ServiceA>());
        }

        [Fact]
        public void FactoryRegistrationsAreTransient() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IServiceA), () => new ServiceA());

            Assert.NotSame(resolver.Resolve<IServiceA>(), resolver.Resolve<IServiceA>());
        }

        [Fact]
        public void CanReplaceRegistrationAfterResolution() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IServiceA, ServiceA>();
            var original = resolver.Resolve<IServiceA>();

            resolver.Register<IServiceA, AlternateServiceA>();

            Assert.IsType<AlternateServiceA>(resolver.Resolve<IServiceA>());
            Assert.NotSame(original, resolver.Resolve<IServiceA>());
        }

        [Fact]
        public void CanUseServiceCollectionRegistrations() {
            var services = new ServiceCollection();
            services.AddSingleton<IServiceA, AlternateServiceA>();
            var resolver = new DefaultDependencyResolver(services);

            Assert.IsType<AlternateServiceA>(resolver.Resolve<IServiceA>());
        }

        [Fact]
        public void CanResolveOpenGenericRegistrations() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IGenericService<>), typeof(GenericService<>));

            var service = resolver.Resolve(typeof(IGenericService<string>));

            Assert.IsType<GenericService<string>>(service);
            Assert.Same(service, resolver.Resolve(typeof(IGenericService<string>)));
        }

        [Fact]
        public void CustomServicesOverrideClientDefaults() {
            var serializer = new DefaultJsonSerializer();
            var services = new ServiceCollection();
            services.AddSingleton<IJsonSerializer>(serializer);

            using var client = new ExceptionlessClient(services);

            Assert.Same(serializer, client.Configuration.Resolver.GetJsonSerializer());
        }

        [Fact]
        public void DisposesContainerOwnedSingletons() {
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDisposableService, DisposableService>();
            var service = resolver.Resolve<IDisposableService>();

            resolver.Dispose();

            Assert.True(service.IsDisposed);
        }

        [Fact]
        public void DoesNotDisposeExplicitInstances() {
            var resolver = new DefaultDependencyResolver();
            var service = new DisposableService();
            resolver.Register<IDisposableService>(service);
            resolver.Resolve<IDisposableService>();

            resolver.Dispose();

            Assert.False(service.IsDisposed);
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

    public interface IDisposableService {
        bool IsDisposed { get; }
    }

    public class DisposableService : IDisposableService, IDisposable {
        public bool IsDisposed { get; private set; }

        public void Dispose() {
            IsDisposed = true;
        }
    }
}
