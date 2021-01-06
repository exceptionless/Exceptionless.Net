using System;
using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Exceptionless.SampleLambdaAspNetCore {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            // Reads settings from IConfiguration then adds additional configuration from this lambda.
            // This also configures ExceptionlessClient.Default
            services.AddExceptionless(c => c.DefaultData["Startup"] = "heyyy");
            // OR
            // services.AddExceptionless();
            // OR
            // services.AddExceptionless("API_KEY_HERE");

            // This enables Exceptionless to gather more detailed information about unhandled exceptions and other events
            services.AddHttpContextAccessor();

            // This is normal ASP.NET code
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            // Adds Exceptionless middleware to listen for unhandled exceptions
            app.UseExceptionless();

            // This is normal ASP.NET code
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}