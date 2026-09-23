using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Exceptionless.Dependency {
    public sealed class DefaultDependencyResolver : IDependencyResolver {
        private readonly object _lock = new object();
        private readonly IServiceCollection _services;
        private readonly AsyncLocal<ActivationFrame> _activeActivation = new AsyncLocal<ActivationFrame>();
        private readonly AsyncLocal<ResolutionFrame> _activeResolution = new AsyncLocal<ResolutionFrame>();
        private readonly AsyncLocal<DisposalFrame> _activeDisposal = new AsyncLocal<DisposalFrame>();
        // Each provider snapshot owns the factory results it creates. Track disposable identities
        // so a factory cannot accidentally give the same object to multiple disposal captures.
        private readonly HashSet<ServiceDescriptor> _factories = new HashSet<ServiceDescriptor>();
        private readonly HashSet<object> _factoryDisposables = new HashSet<object>(ReferenceComparer.Instance);
        // Microsoft DI providers are immutable. Registrations made after resolution create a
        // new coherent provider snapshot; older snapshots stay alive so services already
        // returned to callers are not disposed underneath them.
        private readonly List<ServiceProvider> _providerSnapshots = new List<ServiceProvider>();
        private ServiceProvider _provider;
        private int _activeResolutions;
        private bool _disposeStarted;
        private bool _disposeCompleted;

        /// <summary>
        /// Creates an empty resolver backed by Microsoft.Extensions.DependencyInjection.
        /// </summary>
        public DefaultDependencyResolver() : this(new ServiceCollection()) { }

        /// <summary>
        /// Creates a resolver backed by a copy of the supplied service descriptors.
        /// </summary>
        /// <param name="services">Services to make available to the resolver.</param>
        public DefaultDependencyResolver(IServiceCollection services) {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            _services = new ServiceCollection();
            AddServices(services);
        }

        public object Resolve([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType) {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            ResolutionFrame resolution = EnterResolution(out ServiceProvider provider);
            try {
                if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IKeyedServiceProvider) || serviceType == typeof(IServiceScopeFactory))
                    return new FallbackServiceProvider(this, provider).GetService(serviceType);

                var service = provider.GetService(serviceType);
                if (service != null)
                    return service;

                return CanActivate(serviceType) ? CreateInstance(provider, serviceType, serviceType) : null;
            } finally {
                ExitResolution(resolution);
            }
        }

        public void Register(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type concreteType) {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (concreteType == null)
                throw new ArgumentNullException(nameof(concreteType));
            ValidateOpenGenericImplementation(concreteType);
            if (!CanAssign(serviceType, concreteType))
                throw new ArgumentException($"Type '{concreteType.FullName}' cannot be assigned to service '{serviceType.FullName}'.", nameof(concreteType));

            lock (_lock) {
                ThrowIfDisposed();
                Remove(serviceType);

                bool singleton = serviceType.IsInterface || serviceType.IsAbstract;
                if (serviceType.IsGenericTypeDefinition) {
                    _services.Add(singleton
                        ? ServiceDescriptor.Singleton(serviceType, concreteType)
                        : ServiceDescriptor.Transient(serviceType, concreteType));
                } else {
                    Func<IServiceProvider, object> factory = provider => CreateInstance(provider, serviceType, concreteType);
                    _services.Add(singleton
                        ? ServiceDescriptor.Singleton(serviceType, factory)
                        : ServiceDescriptor.Transient(serviceType, factory));
                }

                InvalidateProvider();
            }
        }

        public void Register(Type serviceType, Func<object> activator) {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (activator == null)
                throw new ArgumentNullException(nameof(activator));

            lock (_lock) {
                ThrowIfDisposed();
                Remove(serviceType);
                AddFactory(ServiceDescriptor.Transient(serviceType, _ => activator()));
                InvalidateProvider();
            }
        }

        public void RegisterInstance(Type serviceType, object instance) {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (!serviceType.IsInstanceOfType(instance))
                throw new ArgumentException($"Instance of type '{instance.GetType().FullName}' cannot be assigned to service '{serviceType.FullName}'.", nameof(instance));

            lock (_lock) {
                ThrowIfDisposed();
                Remove(serviceType);
                _services.Add(ServiceDescriptor.Singleton(serviceType, instance));
                InvalidateProvider();
            }
        }

        internal void RegisterSingleton(Type serviceType, Func<object> activator) {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (activator == null)
                throw new ArgumentNullException(nameof(activator));

            lock (_lock) {
                ThrowIfDisposed();
                Remove(serviceType);
                AddFactory(ServiceDescriptor.Singleton(serviceType, _ => activator()));
                InvalidateProvider();
            }
        }

        internal void AddServices(IEnumerable<ServiceDescriptor> services) {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            lock (_lock) {
                ThrowIfDisposed();
                foreach (var service in services) {
                    Type implementationType = service.IsKeyedService ? service.KeyedImplementationType : service.ImplementationType;
                    ValidateOpenGenericImplementation(implementationType);
                    _services.Add(service);
                    if (IsFactory(service))
                        _factories.Add(service);
                }

                InvalidateProvider();
            }
        }

        public void Dispose() {
            ServiceProvider[] providers;
            lock (_lock) {
                if (IsResolving())
                    throw new InvalidOperationException("The dependency resolver cannot be disposed while the current resolution is still active.");

                if (_disposeStarted) {
                    if (IsDisposing())
                        return;

                    while (!_disposeCompleted)
                        Monitor.Wait(_lock);

                    return;
                }

                _disposeStarted = true;
                while (_activeResolutions > 0)
                    Monitor.Wait(_lock);

                providers = _providerSnapshots.ToArray();
                _providerSnapshots.Clear();
                _factories.Clear();
                _factoryDisposables.Clear();
                _provider = null;
            }

            List<Exception> exceptions = null;
            DisposalFrame previousDisposal = _activeDisposal.Value;
            var currentDisposal = new DisposalFrame(previousDisposal);
            _activeDisposal.Value = currentDisposal;
            try {
                for (int index = providers.Length - 1; index >= 0; index--) {
                    try {
                        providers[index].DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    } catch (Exception ex) {
                        if (exceptions == null)
                            exceptions = new List<Exception>();

                        exceptions.Add(ex);
                    }
                }
            } finally {
                currentDisposal.Deactivate();
                _activeDisposal.Value = previousDisposal;
                lock (_lock) {
                    _disposeCompleted = true;
                    Monitor.PulseAll(_lock);
                }
            }

            if (exceptions == null)
                return;

            if (exceptions.Count == 1)
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();

            throw new AggregateException("One or more dependency resolver snapshots could not be disposed.", exceptions);
        }

        private ServiceProvider GetProvider() {
            if (_provider != null)
                return _provider;

            IServiceCollection services = new ServiceCollection();
            foreach (var service in _services)
                services.Add(GetProviderDescriptor(service));

            _provider = services.BuildServiceProvider();
            _providerSnapshots.Add(_provider);
            return _provider;
        }

        private ServiceDescriptor GetProviderDescriptor(ServiceDescriptor service) {
            if (_factories.Contains(service) && service.IsKeyedService) {
                return ServiceDescriptor.DescribeKeyed(service.ServiceType, service.ServiceKey, (provider, key) =>
                    Activate(service.ServiceType, () => TrackFactoryResult(service, service.KeyedImplementationFactory(new FallbackServiceProvider(this, provider), key)), key), service.Lifetime);
            }

            if (_factories.Contains(service)) {
                return ServiceDescriptor.Describe(service.ServiceType, provider =>
                    Activate(service.ServiceType, () => TrackFactoryResult(service, service.ImplementationFactory(new FallbackServiceProvider(this, provider)))), service.Lifetime);
            }

            Type implementationType = service.IsKeyedService ? service.KeyedImplementationType : service.ImplementationType;
            if (implementationType == null || implementationType.ContainsGenericParameters)
                return service;

            if (service.IsKeyedService) {
                return ServiceDescriptor.DescribeKeyed(service.ServiceType, service.ServiceKey, (provider, key) =>
                    CreateInstance(provider, service.ServiceType, implementationType, key), service.Lifetime);
            }

            return ServiceDescriptor.Describe(service.ServiceType, provider =>
                CreateInstance(provider, service.ServiceType, implementationType), service.Lifetime);
        }

        private object TrackFactoryResult(ServiceDescriptor service, object instance) {
            if (!(instance is IDisposable) && !(instance is IAsyncDisposable))
                return instance;

            lock (_lock) {
                if (!_factories.Contains(service))
                    return instance;

                if (!_factoryDisposables.Add(instance)) {
                    throw new InvalidOperationException($"The factory for service type '{service.ServiceType.FullName}' returned the same disposable instance more than once. Register the instance directly so its ownership is unambiguous.");
                }
            }

            return instance;
        }

        private ResolutionFrame EnterResolution(out ServiceProvider provider) {
            ResolutionFrame previous = _activeResolution.Value;
            var current = new ResolutionFrame(previous);
            lock (_lock) {
                ThrowIfDisposed();
                provider = GetProvider();
                _activeResolutions++;
            }

            _activeResolution.Value = current;
            return current;
        }

        private ResolutionFrame EnterResolution() {
            ResolutionFrame previous = _activeResolution.Value;
            var current = new ResolutionFrame(previous);
            lock (_lock) {
                ThrowIfDisposed();
                _activeResolutions++;
            }

            _activeResolution.Value = current;
            return current;
        }

        private void ExitResolution(ResolutionFrame resolution) {
            resolution.Deactivate();
            _activeResolution.Value = resolution.Parent;
            lock (_lock) {
                _activeResolutions--;
                if (_activeResolutions == 0)
                    Monitor.PulseAll(_lock);
            }
        }

        private void AddFactory(ServiceDescriptor service) {
            _services.Add(service);
            _factories.Add(service);
        }

        private object CreateInstance(IServiceProvider provider, Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type concreteType, object serviceKey = null) {
            return Activate(serviceType, () => ActivatorUtilities.CreateInstance(new FallbackServiceProvider(this, provider), concreteType), serviceKey);
        }

        private object Activate(Type serviceType, Func<object> activator, object serviceKey = null) {
            if (IsActive(serviceType, serviceKey))
                throw CreateCircularDependencyException(serviceType);

            ActivationFrame previous = _activeActivation.Value;
            var current = new ActivationFrame(serviceType, serviceKey, previous);
            _activeActivation.Value = current;
            try {
                return activator();
            } finally {
                current.Deactivate();
                _activeActivation.Value = previous;
            }
        }

        private bool IsActive(Type serviceType, object serviceKey = null) {
            for (ActivationFrame current = _activeActivation.Value; current != null; current = current.Parent) {
                if (current.IsActive && current.ServiceType == serviceType && Equals(current.ServiceKey, serviceKey))
                    return true;
            }

            return false;
        }

        private void Remove(Type serviceType) {
            for (int index = _services.Count - 1; index >= 0; index--) {
                ServiceDescriptor service = _services[index];
                if (service.ServiceType != serviceType)
                    continue;

                _services.RemoveAt(index);
            }
        }

        private void InvalidateProvider() {
            _provider = null;
        }

        private void ThrowIfDisposed() {
            if (_disposeStarted)
                throw new ObjectDisposedException(nameof(DefaultDependencyResolver));
        }

        private bool IsResolving() {
            for (ResolutionFrame current = _activeResolution.Value; current != null; current = current.Parent) {
                if (current.IsActive)
                    return true;
            }

            return false;
        }

        private bool IsDisposing() {
            for (DisposalFrame current = _activeDisposal.Value; current != null; current = current.Parent) {
                if (current.IsActive)
                    return true;
            }

            return false;
        }

        private static bool CanActivate(Type type) {
            return !type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters;
        }

        private static bool IsFactory(ServiceDescriptor service) {
            return service.IsKeyedService
                ? service.KeyedImplementationFactory != null
                : service.ImplementationFactory != null;
        }

        private static void ValidateOpenGenericImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType) {
            if (implementationType == null || !implementationType.ContainsGenericParameters)
                return;

            foreach (var constructor in implementationType.GetConstructors()) {
                foreach (var parameter in constructor.GetParameters()) {
                    Type parameterType = parameter.ParameterType;
                    if (parameterType == typeof(IServiceProvider) || parameterType == typeof(IKeyedServiceProvider) || parameterType == typeof(IServiceScopeFactory)) {
                        throw new NotSupportedException($"Open-generic implementation type '{implementationType.FullName}' cannot directly request '{parameterType.FullName}'. Register a closed implementation or use a factory so provider access remains disposal-safe.");
                    }
                }
            }
        }

        private static InvalidOperationException CreateCircularDependencyException(Type serviceType) {
            return new InvalidOperationException($"A circular dependency was detected for service of type '{serviceType.FullName}'.");
        }

        private static bool CanAssign(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type concreteType) {
            if (!serviceType.IsGenericTypeDefinition)
                return serviceType.IsAssignableFrom(concreteType);

            if (!concreteType.IsGenericTypeDefinition)
                return false;

            if (serviceType.IsInterface)
                return concreteType.GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == serviceType);

            for (var current = concreteType; current != null; current = current.BaseType) {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == serviceType)
                    return true;
            }

            return false;
        }

        private sealed class FallbackServiceProvider : IServiceProvider, IKeyedServiceProvider {
            private readonly DefaultDependencyResolver _resolver;
            private readonly IServiceProvider _provider;

            public FallbackServiceProvider(DefaultDependencyResolver resolver, IServiceProvider provider) {
                _resolver = resolver;
                _provider = provider;
            }

            [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "The unannotated IServiceProvider contract cannot express constructor requirements. NativeAOT rejects this dynamic fallback before activation; AOT callers must register the service.")]
            public object GetService(Type serviceType) {
                ResolutionFrame resolution = _resolver.EnterResolution();
                try {
                    if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IKeyedServiceProvider))
                        return this;

                    if (serviceType == typeof(IServiceScopeFactory)) {
                        var scopeFactory = (IServiceScopeFactory)_provider.GetService(serviceType);
                        return scopeFactory == null ? null : new LeasingServiceScopeFactory(_resolver, scopeFactory);
                    }

                    if (_resolver.IsActive(serviceType))
                        throw CreateCircularDependencyException(serviceType);

                    var service = _provider.GetService(serviceType);
                    if (service != null)
                        return service;

                    if (!CanActivate(serviceType))
                        return null;

#if NET8_0_OR_GREATER
                    if (!RuntimeFeature.IsDynamicCodeSupported)
                        throw new NotSupportedException($"Type '{serviceType.FullName}' must be registered before it can be resolved in a NativeAOT application.");
#endif

                    return _resolver.CreateInstance(_provider, serviceType, serviceType);
                } finally {
                    _resolver.ExitResolution(resolution);
                }
            }

            public object GetKeyedService(Type serviceType, object serviceKey) {
                ResolutionFrame resolution = _resolver.EnterResolution();
                try {
                    if (_resolver.IsActive(serviceType, serviceKey))
                        throw CreateCircularDependencyException(serviceType);

                    if (!(_provider is IKeyedServiceProvider keyedProvider))
                        throw new InvalidOperationException("The underlying dependency provider does not support keyed services.");

                    return keyedProvider.GetKeyedService(serviceType, serviceKey);
                } finally {
                    _resolver.ExitResolution(resolution);
                }
            }

            public object GetRequiredKeyedService(Type serviceType, object serviceKey) {
                object service = GetKeyedService(serviceType, serviceKey);
                if (service == null)
                    throw new InvalidOperationException($"No keyed service for type '{serviceType.FullName}' and key '{serviceKey}' has been registered.");

                return service;
            }
        }

        private sealed class LeasingServiceScopeFactory : IServiceScopeFactory {
            private readonly DefaultDependencyResolver _resolver;
            private readonly IServiceScopeFactory _scopeFactory;

            public LeasingServiceScopeFactory(DefaultDependencyResolver resolver, IServiceScopeFactory scopeFactory) {
                _resolver = resolver;
                _scopeFactory = scopeFactory;
            }

            public IServiceScope CreateScope() {
                ResolutionFrame resolution = _resolver.EnterResolution();
                try {
                    return new LeasingServiceScope(_resolver, _scopeFactory.CreateScope());
                } finally {
                    _resolver.ExitResolution(resolution);
                }
            }
        }

        private sealed class LeasingServiceScope : IServiceScope, IAsyncDisposable {
            private readonly IServiceScope _scope;

            public LeasingServiceScope(DefaultDependencyResolver resolver, IServiceScope scope) {
                _scope = scope;
                ServiceProvider = new FallbackServiceProvider(resolver, scope.ServiceProvider);
            }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose() {
                _scope.Dispose();
            }

            public ValueTask DisposeAsync() {
                if (_scope is IAsyncDisposable asyncDisposable)
                    return asyncDisposable.DisposeAsync();

                _scope.Dispose();
                return default;
            }
        }

        private sealed class ActivationFrame {
            private int _isActive = 1;

            public ActivationFrame(Type serviceType, object serviceKey, ActivationFrame parent) {
                ServiceType = serviceType;
                ServiceKey = serviceKey;
                Parent = parent;
            }

            public Type ServiceType { get; }
            public object ServiceKey { get; }
            public ActivationFrame Parent { get; }
            // ExecutionContext can copy this frame into child tasks. Deactivation makes those
            // inherited copies harmless after the synchronous factory or constructor returns.
            public bool IsActive => Volatile.Read(ref _isActive) != 0;

            public void Deactivate() {
                Volatile.Write(ref _isActive, 0);
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object> {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object left, object right) {
                return ReferenceEquals(left, right);
            }

            public int GetHashCode(object instance) {
                return RuntimeHelpers.GetHashCode(instance);
            }
        }

        private sealed class ResolutionFrame {
            private int _isActive = 1;

            public ResolutionFrame(ResolutionFrame parent) {
                Parent = parent;
            }

            public ResolutionFrame Parent { get; }
            public bool IsActive => Volatile.Read(ref _isActive) != 0;

            public void Deactivate() {
                Volatile.Write(ref _isActive, 0);
            }
        }

        private sealed class DisposalFrame {
            private int _isActive = 1;

            public DisposalFrame(DisposalFrame parent) {
                Parent = parent;
            }

            public DisposalFrame Parent { get; }
            public bool IsActive => Volatile.Read(ref _isActive) != 0;

            public void Deactivate() {
                Volatile.Write(ref _isActive, 0);
            }
        }
    }
}
