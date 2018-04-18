using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Exceptionless.Dependency;
using Exceptionless.Plugins.Default;
using Exceptionless.Web;
using Exceptionless.Web.Extensions;

namespace Exceptionless.Mvc {
    public class ExceptionlessModule : IHttpModule {
        private readonly ExceptionlessClient _client;
        private HttpApplication _app;

        public ExceptionlessModule() {
            _client = ExceptionlessClient.Default;
        }

        public ExceptionlessModule(ExceptionlessClient client = null) {
            _client = client ?? throw new ArgumentNullException();
        }

        public virtual void Init(HttpApplication app) {
            _client.Startup();
            _client.RegisterHttpApplicationErrorHandler(app);
            _client.Configuration.AddPlugin<ExceptionlessWebPlugin>();
            _client.Configuration.AddPlugin<IgnoreUserAgentPlugin>();
            _client.Configuration.Resolver.Register<ILastReferenceIdManager, WebLastReferenceIdManager>();

            _app = app;

            if (!GlobalFilters.Filters.Any(f => f.Instance is ExceptionlessSendErrorsAttribute))
                GlobalFilters.Filters.Add(new ExceptionlessSendErrorsAttribute(_client));
        }

        public void Dispose() {
            _client.Shutdown();
            _client.UnregisterHttpApplicationErrorHandler(_app);
        }
    }
}