using System;
using Exceptionless.Logging;
using Exceptionless.Queue;
using Exceptionless.Serializer;
using Exceptionless.Services;
using Exceptionless.Storage;
using Exceptionless.Submission;
using Microsoft.Extensions.DependencyInjection;

namespace Exceptionless.Dependency {
    public class DependencyResolver {
        private static readonly Lazy<IDependencyResolver> _defaultResolver = new Lazy<IDependencyResolver>(CreateDefault);

        private static IDependencyResolver _resolver;

        public static IDependencyResolver Default {
            get { return _resolver ?? _defaultResolver.Value; }
            set { _resolver = value; }
        }

        public static IDependencyResolver CreateDefault() {
            var resolver = new DefaultDependencyResolver();
            RegisterDefaultServices(resolver);
            return resolver;
        }

        /// <summary>
        /// Creates a resolver containing the default Exceptionless services followed by application overrides.
        /// </summary>
        /// <param name="services">Services that should be added to or override the defaults.</param>
        public static IDependencyResolver CreateDefault(IServiceCollection services) {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var resolver = (DefaultDependencyResolver)CreateDefault();
            resolver.AddServices(services);
            return resolver;
        }

        public static void RegisterDefaultServices(IDependencyResolver resolver) {
            resolver.Register(resolver);

            if (resolver is DefaultDependencyResolver defaultResolver) {
                resolver.Register<IObjectStorage, InMemoryObjectStorage>();
                resolver.Register<IExceptionlessLog, NullExceptionlessLog>();
                resolver.Register<IJsonSerializer, DefaultJsonSerializer>();
                resolver.Register<IStorageSerializer, DefaultJsonSerializer>();
                resolver.Register<IEventQueue, DefaultEventQueue>();
                resolver.Register<ISubmissionClient, DefaultSubmissionClient>();
                resolver.Register<IEnvironmentInfoCollector, DefaultEnvironmentInfoCollector>();
                resolver.Register<ILastReferenceIdManager, DefaultLastReferenceIdManager>();
                defaultResolver.RegisterSingleton(typeof(PersistedDictionary), () => new PersistedDictionary("client-data.json", resolver.Resolve<IObjectStorage>(), resolver.Resolve<IJsonSerializer>()));
                return;
            }

            var fileStorage = new Lazy<IObjectStorage>(() => resolver.Resolve<InMemoryObjectStorage>());
            resolver.Register(typeof(IObjectStorage), () => fileStorage.Value);

            var exceptionlessLog = new Lazy<IExceptionlessLog>(() => resolver.Resolve<NullExceptionlessLog>());
            resolver.Register(typeof(IExceptionlessLog), () => exceptionlessLog.Value);

            var jsonSerializer = new Lazy<IJsonSerializer>(() => resolver.Resolve<DefaultJsonSerializer>());
            resolver.Register(typeof(IJsonSerializer), () => jsonSerializer.Value);

            var storageSerializer = new Lazy<IStorageSerializer>(() => resolver.Resolve<DefaultJsonSerializer>());
            resolver.Register(typeof(IStorageSerializer), () => storageSerializer.Value);

            var eventQueue = new Lazy<IEventQueue>(() => resolver.Resolve<DefaultEventQueue>());
            resolver.Register(typeof(IEventQueue), () => eventQueue.Value);

            var submissionClient = new Lazy<ISubmissionClient>(() => resolver.Resolve<DefaultSubmissionClient>());
            resolver.Register(typeof(ISubmissionClient), () => submissionClient.Value);

            var environmentInfoCollector = new Lazy<IEnvironmentInfoCollector>(() => resolver.Resolve<DefaultEnvironmentInfoCollector>());
            resolver.Register(typeof(IEnvironmentInfoCollector), () => environmentInfoCollector.Value);

            var lastClientIdManager = new Lazy<ILastReferenceIdManager>(() => resolver.Resolve<DefaultLastReferenceIdManager>());
            resolver.Register(typeof(ILastReferenceIdManager), () => lastClientIdManager.Value);

            var persistedClientData = new Lazy<PersistedDictionary>(() => new PersistedDictionary("client-data.json", resolver.Resolve<IObjectStorage>(), resolver.Resolve<IJsonSerializer>()));
            resolver.Register(typeof(PersistedDictionary), () => persistedClientData.Value);
        }
    }
}
