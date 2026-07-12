using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Exceptionless.Dependency {
    public sealed class DefaultDependencyResolver : IDependencyResolver {
        private readonly object _lock = new object();
        private readonly IServiceCollection _services;
        private readonly List<ServiceProvider> _providers = new List<ServiceProvider>();
        private ServiceProvider _provider;
        private bool _disposed;

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

        public object Resolve(Type serviceType) {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            lock (_lock) {
                ThrowIfDisposed();

                var provider = GetProvider();
                var service = provider.GetService(serviceType);
                if (service != null)
                    return service;

                return CanActivate(serviceType) ? CreateInstance(provider, serviceType) : null;
            }
        }

        public void Register(Type serviceType, Type concreteType) {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (concreteType == null)
                throw new ArgumentNullException(nameof(concreteType));
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
                    Func<IServiceProvider, object> factory = provider => CreateInstance(provider, concreteType);
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
                _services.Add(ServiceDescriptor.Transient(serviceType, _ => activator()));
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
                _services.Add(ServiceDescriptor.Singleton(serviceType, _ => activator()));
                InvalidateProvider();
            }
        }

        internal void AddServices(IEnumerable<ServiceDescriptor> services) {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            lock (_lock) {
                ThrowIfDisposed();
                foreach (var service in services)
                    _services.Add(service);
                InvalidateProvider();
            }
        }

        public void Dispose() {
            lock (_lock) {
                if (_disposed)
                    return;

                _disposed = true;
                foreach (var provider in _providers)
                    provider.Dispose();

                _providers.Clear();
                _provider = null;
            }
        }

        private ServiceProvider GetProvider() {
            if (_provider != null)
                return _provider;

            _provider = _services.BuildServiceProvider();
            _providers.Add(_provider);
            return _provider;
        }

        private object CreateInstance(IServiceProvider provider, Type concreteType) {
            return ActivatorUtilities.CreateInstance(new FallbackServiceProvider(this, provider), concreteType);
        }

        private void Remove(Type serviceType) {
            for (int index = _services.Count - 1; index >= 0; index--) {
                if (_services[index].ServiceType == serviceType)
                    _services.RemoveAt(index);
            }
        }

        private void InvalidateProvider() {
            _provider = null;
        }

        private void ThrowIfDisposed() {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DefaultDependencyResolver));
        }

        private static bool CanActivate(Type type) {
            return !type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters;
        }

        private static bool CanAssign(Type serviceType, Type concreteType) {
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

        private sealed class FallbackServiceProvider : IServiceProvider {
            private readonly DefaultDependencyResolver _resolver;
            private readonly IServiceProvider _provider;

            public FallbackServiceProvider(DefaultDependencyResolver resolver, IServiceProvider provider) {
                _resolver = resolver;
                _provider = provider;
            }

            public object GetService(Type serviceType) {
                if (serviceType == typeof(IServiceProvider))
                    return this;

                var service = _provider.GetService(serviceType);
                if (service != null)
                    return service;

                return CanActivate(serviceType) ? _resolver.CreateInstance(_provider, serviceType) : null;
            }
        }
    }
}
