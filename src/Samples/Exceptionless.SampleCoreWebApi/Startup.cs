using Exceptionless.SampleCoreWebApi.ExceptionlessCore;
using Exceptionless.SampleCoreWebApi.ExceptionlessCore.ExceptionLoggers;
using Exceptionless.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Exceptionless.SampleCoreWebApi
    {
    public class Startup
        {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
            {
            services.AddExceptionlessCorePlugIn();
            services.AddMvc();
            }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        /// </summary>
        /// <param name="app">The application.</param>
        public void Configure(IApplicationBuilder app)
            {
            // *** samples 
            // the order of addition determines the sequence of how each Exception Intercept Handler gets called. 
            app.UseExceptionCorePlugIn(new ExceptionlessCoreOptions());
            app.AddExceptionlessCoreHandlerError(new ExceptionInitializer(new ExceptionCategorizer()));

            //app.AddExceptionlessCoreHandlerError(new ExceptionJIRALogger());
            //app.AddExceptionlessCoreHandlerError(new ExceptionDbLogger());

            app.AddExceptionlessCoreHandlerError(new ExceptionFinalizer());

            // OR if intercepts are defined in the IoC
           
            //app.AddExceptionInterceptHandler<ExceptionInitializer>();
            //app.AddExceptionInterceptHandler<ExceptionDbLogger>();
            //app.AddExceptionInterceptHandler<ExceptionJIRALogger>();
            //app.AddExceptionInterceptHandler(typeof(ExceptionFinalizer));

            // needed for WebApi Controllers. 
            // Note: make sure this line is added after the exception core pluging has been added.

            app.UseMvc();
            }
        }
    }




//var builder = new ConfigurationBuilder()
//                .SetBasePath(env.ContentRootPath)
//                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
//                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

//            if (env.IsEnvironment("Development"))
//            {
//                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
//                builder.AddApplicationInsightsSettings(developerMode: true);
//            }

//            builder.AddEnvironmentVariables();
//            Configuration = builder.Build();
//        }

//        public IConfigurationRoot Configuration { get; }

//// This method gets called by the runtime. Use this method to add services to the container
//public void ConfigureServices(IServiceCollection services)
//    {
//    // Add framework services.
//    services.AddApplicationInsightsTelemetry(Configuration);

//    services.AddMvc();
//    }

//// This method gets called by the runtime. Use this method to configure the HTTP request pipeline
//public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
//    {
//    loggerFactory.AddConsole(Configuration.GetSection("Logging"));
//    loggerFactory.AddDebug();

//    app.UseApplicationInsightsRequestTelemetry();

//    app.UseApplicationInsightsExceptionTelemetry();

//    app.UseMvc();