using System;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Exceptionless.Tests.Dependency {
    public partial class DependencyTests {
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
        public async Task Resolve_WhenFactoryStartsChildTask_DoesNotLeakActivationToChild() {
            // Arrange
            using var resolver = new DefaultDependencyResolver();
            var release = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<IServiceA> childResolution = null;
            int activations = 0;
            resolver.Register(typeof(IServiceA), () => {
                if (Interlocked.Increment(ref activations) == 1) {
                    childResolution = Task.Run(async () => {
                        await release.Task;
                        return resolver.Resolve<IServiceA>();
                    });
                }

                return new ServiceA();
            });

            // Act
            resolver.Resolve<IServiceA>();
            release.SetResult(null);
            Task timeout = Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Task completed = await Task.WhenAny(childResolution, timeout);
            IServiceA childService = completed == timeout ? null : await childResolution;

            // Assert
            Assert.NotSame(timeout, completed);
            Assert.NotNull(childService);
            Assert.Equal(2, activations);
        }

        [Fact]
        public void Resolve_WhenFactoryWaitsForCrossThreadDependency_DoesNotDeadlock() {
            // Arrange
            using var resolver = new DefaultDependencyResolver();
            resolver.Register<IServiceA, ServiceA>();
            resolver.Register(typeof(IServiceB), () => {
                Task<IServiceA> dependency = Task.Run(() => resolver.Resolve<IServiceA>());
                if (!dependency.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))
                    throw new TimeoutException("Cross-thread dependency resolution timed out.");

                return new ServiceB(dependency.Result, new ServiceC());
            });

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<IServiceB>());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Resolve_WhenFactoryWaitsForCrossThreadSelfResolution_ReportsCircularDependency() {
            // Arrange
            using var resolver = new DefaultDependencyResolver();
            int activations = 0;
            resolver.Register(typeof(IServiceA), () => {
                if (Interlocked.Increment(ref activations) == 1)
                    return Task.Run(() => resolver.Resolve<IServiceA>()).GetAwaiter().GetResult();

                return new ServiceA();
            });

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<IServiceA>());

            // Assert
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("circular dependency", invalidOperationException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Dispose_WhilePublicResolutionIsActive_WaitsAndRejectsNewResolutions() {
            // Arrange
            var service = new CountingDisposable();
            var resolver = new DefaultDependencyResolver();
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            resolver.Register(typeof(ICountingDisposable), () => {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5), cancellationToken);
                return service;
            });
            Task<ICountingDisposable> resolution = Task.Run(() => resolver.Resolve<ICountingDisposable>());
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), cancellationToken));

            // Act
            Task disposal = Task.Run(resolver.Dispose, cancellationToken);
            bool disposalStarted = SpinWait.SpinUntil(() => Record.Exception(() => resolver.Resolve<ServiceC>()) is ObjectDisposedException, TimeSpan.FromSeconds(5));
            bool disposalWaited = !disposal.IsCompleted;
            release.Set();
            ICountingDisposable resolved = await resolution;
            await disposal;

            // Assert
            Assert.True(disposalStarted);
            Assert.True(disposalWaited);
            Assert.Same(service, resolved);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task Dispose_WhileFallbackProviderResolutionIsActive_WaitsForResolution() {
            // Arrange
            var service = new CountingDisposable();
            var resolver = new DefaultDependencyResolver();
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            resolver.Register(typeof(ICountingDisposable), () => {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5), cancellationToken);
                return service;
            });
            IServiceProvider fallbackProvider = resolver.Resolve<ServiceProviderConsumer>().ServiceProvider;
            Task<object> resolution = Task.Run(() => fallbackProvider.GetService(typeof(ICountingDisposable)));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), cancellationToken));

            // Act
            Task disposal = Task.Run(resolver.Dispose, cancellationToken);
            bool disposalStarted = SpinWait.SpinUntil(() => Record.Exception(() => resolver.Resolve<ServiceC>()) is ObjectDisposedException, TimeSpan.FromSeconds(5));
            bool disposalWaited = !disposal.IsCompleted;
            release.Set();
            object resolved = await resolution;
            await disposal;

            // Assert
            Assert.True(disposalStarted);
            Assert.True(disposalWaited);
            Assert.Same(service, resolved);
            Assert.Equal(1, service.DisposeCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Dispose_WhileRegisteredConsumerProviderResolutionIsActive_WaitsForResolution(bool useFactoryRegistration) {
            // Arrange
            var service = new CountingDisposable();
            var services = new ServiceCollection();
            if (useFactoryRegistration)
                services.AddSingleton(provider => new ServiceProviderConsumer(provider));
            else
                services.AddSingleton<ServiceProviderConsumer>();
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            services.AddSingleton<ICountingDisposable>(_ => {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5), cancellationToken);
                return service;
            });
            var resolver = new DefaultDependencyResolver(services);
            IServiceProvider capturedProvider = resolver.Resolve<ServiceProviderConsumer>().ServiceProvider;
            Task<object> resolution = Task.Run(() => capturedProvider.GetService(typeof(ICountingDisposable)));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), cancellationToken));

            // Act
            Task disposal = Task.Run(resolver.Dispose, cancellationToken);
            bool disposalStarted = SpinWait.SpinUntil(() => Record.Exception(() => resolver.Resolve<ServiceC>()) is ObjectDisposedException, TimeSpan.FromSeconds(5));
            bool disposalWaited = !disposal.IsCompleted;
            release.Set();
            object resolved = await resolution;
            await disposal;

            // Assert
            Assert.True(disposalStarted);
            Assert.True(disposalWaited);
            Assert.Same(service, resolved);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public async Task Dispose_WhileScopedResolutionIsActive_WaitsAndPreservesScopeOwnership() {
            // Arrange
            var service = new CountingDisposable();
            var services = new ServiceCollection();
            services.AddSingleton<ServiceProviderConsumer>();
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            services.AddScoped<ICountingDisposable>(_ => {
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(5), cancellationToken);
                return service;
            });
            var resolver = new DefaultDependencyResolver(services);
            IServiceProvider rootProvider = resolver.Resolve<ServiceProviderConsumer>().ServiceProvider;
            IServiceScope scope = rootProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            Task<object> resolution = Task.Run(() => scope.ServiceProvider.GetService(typeof(ICountingDisposable)));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5), cancellationToken));

            // Act
            Task disposal = Task.Run(resolver.Dispose, cancellationToken);
            bool disposalStarted = SpinWait.SpinUntil(() => Record.Exception(() => resolver.Resolve<ServiceC>()) is ObjectDisposedException, TimeSpan.FromSeconds(5));
            bool disposalWaited = !disposal.IsCompleted;
            release.Set();
            object resolved = await resolution;
            await disposal;
            int disposeCountBeforeScope = service.DisposeCount;
            scope.Dispose();

            // Assert
            Assert.True(disposalStarted);
            Assert.True(disposalWaited);
            Assert.Same(service, resolved);
            Assert.Equal(0, disposeCountBeforeScope);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public void Resolve_FromLeasingScopes_PreservesScopedLifetimeIsolation() {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ServiceProviderConsumer>();
            services.AddScoped<IScopedService, ScopedService>();
            services.AddKeyedScoped<IScopedService, ScopedService>("scoped");
            var resolver = new DefaultDependencyResolver(services);
            IServiceProvider rootProvider = resolver.Resolve<ServiceProviderConsumer>().ServiceProvider;
            IServiceScopeFactory scopeFactory = rootProvider.GetRequiredService<IServiceScopeFactory>();
            using IServiceScope firstScope = scopeFactory.CreateScope();
            using IServiceScope secondScope = scopeFactory.CreateScope();

            // Act
            var first = firstScope.ServiceProvider.GetRequiredService<IScopedService>();
            var repeated = firstScope.ServiceProvider.GetRequiredService<IScopedService>();
            var second = secondScope.ServiceProvider.GetRequiredService<IScopedService>();
            var firstKeyed = firstScope.ServiceProvider.GetRequiredKeyedService<IScopedService>("scoped");
            var repeatedKeyed = firstScope.ServiceProvider.GetRequiredKeyedService<IScopedService>("scoped");
            var secondKeyed = secondScope.ServiceProvider.GetRequiredKeyedService<IScopedService>("scoped");

            // Assert
            Assert.Same(first, repeated);
            Assert.NotSame(first, second);
            Assert.Same(firstKeyed, repeatedKeyed);
            Assert.NotSame(firstKeyed, secondKeyed);
            resolver.Dispose();
        }

        [Fact]
        public void Resolve_DirectProviderServices_StayBehindDisposalLease() {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<KeyedServiceProviderConsumer>();
            services.AddKeyedSingleton<IServiceA, ServiceA>("service");
            using var resolver = new DefaultDependencyResolver(services);
            var provider = resolver.Resolve<IServiceProvider>();
            var keyedProvider = resolver.Resolve<IKeyedServiceProvider>();
            var scopeFactory = resolver.Resolve<IServiceScopeFactory>();
            IKeyedServiceProvider capturedKeyedProvider = resolver.Resolve<KeyedServiceProviderConsumer>().ServiceProvider;
            using IServiceScope scope = scopeFactory.CreateScope();

            Assert.NotNull(provider.GetService(typeof(KeyedServiceProviderConsumer)));
            Assert.NotNull(keyedProvider.GetKeyedService(typeof(IServiceA), "service"));
            Assert.NotNull(capturedKeyedProvider.GetKeyedService(typeof(IServiceA), "service"));

            // Act
            resolver.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => provider.GetService(typeof(IServiceA)));
            Assert.Throws<ObjectDisposedException>(() => keyedProvider.GetKeyedService(typeof(IServiceA), "service"));
            Assert.Throws<ObjectDisposedException>(() => capturedKeyedProvider.GetKeyedService(typeof(IServiceA), "service"));
            Assert.Throws<ObjectDisposedException>(() => scopeFactory.CreateScope());
            Assert.Throws<ObjectDisposedException>(() => scope.ServiceProvider.GetService(typeof(IServiceA)));
        }

        [Fact]
        public void Dispose_FromFactory_ThrowsWithoutDisposingResolver() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(IServiceA), () => {
                resolver.Dispose();
                return new ServiceA();
            });

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<IServiceA>());
            resolver.Register<IServiceA, ServiceA>();
            IServiceA service = resolver.Resolve<IServiceA>();
            resolver.Dispose();

            // Assert
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("current resolution", invalidOperationException.Message, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(service);
        }

        [Fact]
        public void Dispose_FromConstructor_ThrowsWithoutDisposingResolver() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDependencyResolver>(resolver);

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<DisposeInConstructor>());
            IServiceA service = resolver.Resolve<ServiceA>();
            resolver.Dispose();

            // Assert
            Assert.Contains("current resolution", exception.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(service);
        }

        [Fact]
        public void Resolve_WithDistinctDisposableFactoryResults_DisposesEachInstanceOnce() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(ICountingDisposable), () => new CountingDisposable());
            var first = resolver.Resolve<ICountingDisposable>();
            var second = resolver.Resolve<ICountingDisposable>();

            // Act
            resolver.Dispose();

            // Assert
            Assert.NotSame(first, second);
            Assert.Equal(1, first.DisposeCount);
            Assert.Equal(1, second.DisposeCount);
        }

        [Fact]
        public void Resolve_WithRepeatedDisposableFactoryResult_RejectsAmbiguousOwnership() {
            // Arrange
            var shared = new CountingDisposable();
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(ICountingDisposable), () => shared);
            var first = resolver.Resolve<ICountingDisposable>();

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<ICountingDisposable>());
            resolver.Dispose();

            // Assert
            Assert.Same(shared, first);
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Register the instance directly", invalidOperationException.Message, StringComparison.Ordinal);
            Assert.Equal(1, shared.DisposeCount);
        }

        [Fact]
        public void Resolve_AfterReplacingFactory_RejectsPreviouslyCapturedDisposable() {
            // Arrange
            var shared = new CountingDisposable();
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(ICountingDisposable), () => shared);
            var first = resolver.Resolve<ICountingDisposable>();
            resolver.Register(typeof(ICountingDisposable), () => shared);

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<ICountingDisposable>());
            resolver.Dispose();

            // Assert
            Assert.Same(shared, first);
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Register the instance directly", invalidOperationException.Message, StringComparison.Ordinal);
            Assert.Equal(1, shared.DisposeCount);
        }

        [Fact]
        public void Resolve_FromOlderProviderAfterReplacingFactory_PreservesOwnershipTracking() {
            // Arrange
            var shared = new CountingDisposable();
            var services = new ServiceCollection();
            services.AddSingleton<ServiceProviderConsumer>();
            services.AddSingleton<ICountingDisposable>(_ => shared);
            var resolver = new DefaultDependencyResolver(services);
            IServiceProvider olderProvider = resolver.Resolve<ServiceProviderConsumer>().ServiceProvider;
            resolver.Register(typeof(ICountingDisposable), () => shared);
            var older = olderProvider.GetService(typeof(ICountingDisposable));

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<ICountingDisposable>());
            resolver.Dispose();

            // Assert
            Assert.Same(shared, older);
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Register the instance directly", invalidOperationException.Message, StringComparison.Ordinal);
            Assert.Equal(1, shared.DisposeCount);
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
        public void Resolve_WithDistinctFactoriesReturningSameDisposable_RejectsAmbiguousOwnership() {
            // Arrange
            var shared = new CountingDisposable();
            var services = new ServiceCollection();
            services.AddSingleton<IFirstCountingDisposable>(_ => shared);
            services.AddSingleton<ISecondCountingDisposable>(_ => shared);
            var resolver = new DefaultDependencyResolver(services);
            var first = resolver.Resolve<IFirstCountingDisposable>();

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<ISecondCountingDisposable>());
            resolver.Dispose();

            // Assert
            Assert.NotNull(first);
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Register the instance directly", invalidOperationException.Message, StringComparison.Ordinal);
            Assert.Equal(1, shared.DisposeCount);
        }

        [Fact]
        public void Resolve_WithSingletonFactory_RebuildsGraphAfterDependencyReplacement() {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IServiceA, ServiceA>();
            services.AddSingleton<IServiceB>(provider => new ServiceB(provider.GetRequiredService<IServiceA>(), new ServiceC()));
            using var resolver = new DefaultDependencyResolver(services);
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
        public void Resolve_WithSharedDisposableFactoryAcrossSnapshots_RejectsAmbiguousOwnership() {
            // Arrange
            var shared = new CountingDisposable();
            var services = new ServiceCollection();
            services.AddSingleton<ICountingDisposable>(_ => shared);
            var resolver = new DefaultDependencyResolver(services);
            var original = resolver.Resolve<ICountingDisposable>();
            resolver.Register<IServiceA, ServiceA>();

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<ICountingDisposable>());
            resolver.Dispose();

            // Assert
            Assert.Same(shared, original);
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Register the instance directly", invalidOperationException.Message, StringComparison.Ordinal);
            Assert.Equal(1, shared.DisposeCount);
        }

        [Fact]
        public void Dispose_WithDistinctFactorySingletonsAcrossSnapshots_DisposesEachInstanceOnce() {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICountingDisposable>(_ => new CountingDisposable());
            var resolver = new DefaultDependencyResolver(services);
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
        public void Dispose_WithAsyncOnlySingleton_DisposesInstance() {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IAsyncDisposableService, AsyncDisposableService>();
            var resolver = new DefaultDependencyResolver(services);
            var service = resolver.Resolve<IAsyncDisposableService>();

            // Act
            Exception exception = Record.Exception(resolver.Dispose);

            // Assert
            Assert.Null(exception);
            Assert.True(service.IsDisposed);
        }

        [Fact]
        public void Resolve_WithRepeatedAsyncDisposableFactoryResult_RejectsAmbiguousOwnership() {
            // Arrange
            var shared = new AsyncDisposableService();
            var services = new ServiceCollection();
            services.AddSingleton<IAsyncDisposableService>(_ => shared);
            var resolver = new DefaultDependencyResolver(services);
            var original = resolver.Resolve<IAsyncDisposableService>();
            resolver.Register<IServiceA, ServiceA>();

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<IAsyncDisposableService>());
            resolver.Dispose();

            // Assert
            Assert.Same(shared, original);
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Register the instance directly", invalidOperationException.Message, StringComparison.Ordinal);
            Assert.True(shared.IsDisposed);
        }

        [Fact]
        public void Resolve_WithSharedKeyedDisposableFactoryAcrossSnapshots_RejectsAmbiguousOwnership() {
            // Arrange
            var shared = new CountingDisposable();
            var services = new ServiceCollection();
            services.AddKeyedSingleton<ICountingDisposable>("shared", (_, _) => shared);
            services.AddTransient<KeyedDisposableConsumer>();
            var resolver = new DefaultDependencyResolver(services);
            var original = resolver.Resolve<KeyedDisposableConsumer>();
            resolver.Register<IServiceA, ServiceA>();

            // Act
            Exception exception = Record.Exception(() => resolver.Resolve<KeyedDisposableConsumer>());
            resolver.Dispose();

            // Assert
            Assert.Same(shared, original.Service);
            var invalidOperationException = Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Register the instance directly", invalidOperationException.Message, StringComparison.Ordinal);
            Assert.Equal(1, shared.DisposeCount);
        }

        [Fact]
        public void Dispose_WhenSnapshotThrows_StillDisposesRemainingSnapshots() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IThrowingDisposable, ThrowingDisposable>();
            var throwing = resolver.Resolve<IThrowingDisposable>();
            resolver.Register<ICountingDisposable, CountingDisposable>();
            var counting = resolver.Resolve<ICountingDisposable>();

            // Act
            Exception exception = Record.Exception(resolver.Dispose);
            Exception repeatedException = Record.Exception(resolver.Dispose);

            // Assert
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Null(repeatedException);
            Assert.Equal(1, throwing.DisposeCount);
            Assert.Equal(1, counting.DisposeCount);
            Assert.Throws<ObjectDisposedException>(() => resolver.Resolve<IServiceA>());
        }

        [Fact]
        public void Dispose_WhenMultipleSnapshotsThrow_AggregatesFailures() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IThrowingDisposable, ThrowingDisposable>();
            var original = resolver.Resolve<IThrowingDisposable>();
            resolver.Register<IServiceA, ServiceA>();
            var replacement = resolver.Resolve<IThrowingDisposable>();

            // Act
            Exception exception = Record.Exception(resolver.Dispose);

            // Assert
            var aggregateException = Assert.IsType<AggregateException>(exception);
            Assert.Equal(2, aggregateException.InnerExceptions.Count);
            Assert.Equal(1, original.DisposeCount);
            Assert.Equal(1, replacement.DisposeCount);
        }

        [Fact]
        public async Task Dispose_WhenOwnedServiceDisposesResolver_RemainsReentrantAndCompletes() {
            // Arrange
            var resolver = new DefaultDependencyResolver();
            resolver.Register<IDependencyResolver>(resolver);
            resolver.Register<IResolverDisposingService, ResolverDisposingService>();
            var service = resolver.Resolve<IResolverDisposingService>();

            // Act
            Task disposal = Task.Run(resolver.Dispose, TestContext.Current.CancellationToken);
            Task completed = await Task.WhenAny(disposal, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            if (completed == disposal)
                await disposal;

            // Assert
            Assert.Same(disposal, completed);
            Assert.True(service.IsDisposed);
        }
    }

    public class ServiceProviderConsumer {
        public ServiceProviderConsumer(IServiceProvider serviceProvider) {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }

    public class KeyedServiceProviderConsumer {
        public KeyedServiceProviderConsumer(IKeyedServiceProvider serviceProvider) {
            ServiceProvider = serviceProvider;
        }

        public IKeyedServiceProvider ServiceProvider { get; }
    }

    public class DisposeInConstructor {
        public DisposeInConstructor(IDependencyResolver resolver) {
            resolver.Dispose();
        }
    }

    public class KeyedDisposableConsumer {
        public KeyedDisposableConsumer([FromKeyedServices("shared")] ICountingDisposable service) {
            Service = service;
        }

        public ICountingDisposable Service { get; }
    }

    public interface IScopedService { }

    public class ScopedService : IScopedService { }

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

    public interface IFirstCountingDisposable : ICountingDisposable { }

    public interface ISecondCountingDisposable : ICountingDisposable { }

    public class CountingDisposable : ICountingDisposable, IFirstCountingDisposable, ISecondCountingDisposable, IDisposable {
        public int DisposeCount { get; private set; }

        public void Dispose() {
            DisposeCount++;
        }
    }

    public interface IThrowingDisposable {
        int DisposeCount { get; }
    }

    public class ThrowingDisposable : IThrowingDisposable, IDisposable {
        public int DisposeCount { get; private set; }

        public void Dispose() {
            DisposeCount++;
            throw new InvalidOperationException("Expected disposal failure.");
        }
    }

    public interface IAsyncDisposableService {
        bool IsDisposed { get; }
    }

    public class AsyncDisposableService : IAsyncDisposableService, IAsyncDisposable {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync() {
            IsDisposed = true;
            return default;
        }
    }

    public interface IResolverDisposingService {
        bool IsDisposed { get; }
    }

    public class ResolverDisposingService : IResolverDisposingService, IDisposable {
        private readonly IDependencyResolver _resolver;

        public ResolverDisposingService(IDependencyResolver resolver) {
            _resolver = resolver;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            _resolver.Dispose();
            IsDisposed = true;
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
