using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ExceptionLess.AspNetCore;
using Exceptionless.SampleCoreMvc.ExceptionlessCore;

namespace Exceptionless.SampleCoreMvc
    {
    public class Startup
        {
        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
            {
                services.AddExceptionlessCorePlugIn();
            }

        /// <summary>
        /// Configures the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        public void Configure(IApplicationBuilder app)
            {
            // make sure that this line is injected first before adding the Exception Jandler PlugIn
            app.UseDeveloperExceptionPage();

            // *** samples 
            // the order of addition determines the sequence of how each Exception Intercept Handler gets called. 
            // Setting RethrowException = true, forces the original exception to be bubbled up to the next Middleware in the pipeline.
            app.UseExceptionCorePlugIn(new ExceptionlessCoreOptions() { RethrowException = true });
            app.AddExceptionlessCoreHandlerError(new ExceptionInitializer(new ExceptionCategorizer()));

            //app.AddExceptionlessCoreHandlerError(new ExceptionJIRALogger());
            //app.AddExceptionlessCoreHandlerError(new ExceptionDbLogger());
            
            // force the exception
            // The broken section of our application.
            app.Map("/throw", throwApp =>
            {
                throwApp.Run(context => { throw new Exception("Oh my goodness...what happened to you!"); });
            });

            app.AddExceptionlessCoreHandlerError(new ExceptionFinalizer());
            //app.UseMvc();



            // The home page.
            app.Run(async context =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>Sample to test Asp Net Cote 1 RC2 MVC Application.<br><br>\r\n");
                await context.Response.WriteAsync("Click here to throw an exception: <a href=\"/throw\">throw</a>\r\n");
                await context.Response.WriteAsync("</body></html>\r\n");
            });
            }
        }
    }
