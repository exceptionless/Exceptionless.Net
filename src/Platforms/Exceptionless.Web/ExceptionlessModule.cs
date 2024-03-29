using System.Web;
using Exceptionless.Dependency;
using Exceptionless.Plugins.Default;
using Exceptionless.Web.Extensions;

namespace Exceptionless.Web {
    public class ExceptionlessModule : IHttpModule {
        private HttpApplication _app;

        public virtual void Init(HttpApplication app) {
            ExceptionlessClient.Default.Startup();
            ExceptionlessClient.Default.RegisterHttpApplicationErrorHandler(app);
            ExceptionlessClient.Default.Configuration.AddPlugin<ExceptionlessWebPlugin>();
            ExceptionlessClient.Default.Configuration.AddPlugin<IgnoreUserAgentPlugin>();
            ExceptionlessClient.Default.Configuration.Resolver.Register<ILastReferenceIdManager, WebLastReferenceIdManager>();

            _app = app;
        }

        public void Dispose() {
            ExceptionlessClient.Default.ShutdownAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            ExceptionlessClient.Default.UnregisterHttpApplicationErrorHandler(_app);
        }
    }
}