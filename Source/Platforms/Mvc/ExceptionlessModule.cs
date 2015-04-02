using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Exceptionless.Dependency;
using Exceptionless.Web;
using Exceptionless.Web.Extensions;

namespace Exceptionless.Mvc {
    public class ExceptionlessModule : IHttpModule {
        private HttpApplication _app;

        public virtual void Init(HttpApplication app) {
            ExceptionlessClient.Default.Startup();
            ExceptionlessClient.Default.RegisterHttpApplicationErrorHandler(app);
            ExceptionlessClient.Default.Configuration.IncludePrivateInformation = true;
            ExceptionlessClient.Default.Configuration.AddPlugin<ExceptionlessWebPlugin>();
            ExceptionlessClient.Default.Configuration.Resolver.Register<ILastReferenceIdManager, WebLastReferenceIdManager>();
            
            _app = app;

            if (!GlobalFilters.Filters.Any(f => f.Instance is ExceptionlessSendErrorsAttribute))
                GlobalFilters.Filters.Add(new ExceptionlessSendErrorsAttribute());
        }

        public void Dispose() {
            ExceptionlessClient.Default.Shutdown();
            ExceptionlessClient.Default.UnregisterHttpApplicationErrorExceptionHandler(_app);
        }
    }
}