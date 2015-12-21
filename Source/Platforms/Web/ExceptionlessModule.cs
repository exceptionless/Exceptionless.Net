using System;
using System.Web;
using Exceptionless.Dependency;
using Exceptionless.Web.Extensions;

namespace Exceptionless.Web {
    public class ExceptionlessModule : IHttpModule {
        private HttpApplication _app;

        public virtual void Init(HttpApplication app) {
            ExceptionlessClient.Default.Startup();
            ExceptionlessClient.Default.RegisterHttpApplicationErrorHandler(app);
            ExceptionlessClient.Default.Configuration.AddPlugin<ExceptionlessWebPlugin>();
            ExceptionlessClient.Default.Configuration.Resolver.Register<ILastReferenceIdManager, WebLastReferenceIdManager>();
            
            _app = app;
        }

        public void Dispose() {
            ExceptionlessClient.Default.Shutdown();
            ExceptionlessClient.Default.UnregisterHttpApplicationErrorExceptionHandler(_app);
        }
    }
}