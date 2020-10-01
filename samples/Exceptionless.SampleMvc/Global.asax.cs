using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Exceptionless.SampleMvc.App_Start;

namespace Exceptionless.SampleMvc {
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication {
        protected void Application_Start() {
            ExceptionlessClient.Default.Configuration.DefaultData["FirstName"] = "Blake";
            ExceptionlessClient.Default.Configuration.DefaultData["IgnoredProperty"] = "Some random data";

            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ExceptionlessClient.Default.Configuration.UseTraceLogger();
            ExceptionlessClient.Default.Configuration.UseReferenceIds();
            ExceptionlessClient.Default.RegisterWebApi(GlobalConfiguration.Configuration);
        }
    }
}