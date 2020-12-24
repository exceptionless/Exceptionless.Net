using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Exceptionless.SampleAspNetCore {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services) {
            // Reads settings from IConfiguration
            services.AddExceptionless(c => c.DefaultData["Startup"] = "heyyy");
            // OR
            // services.AddExceptionless(c => c.ApiKey = "API_KEY_HERE");
            // OR
            // services.AddExceptionless("API_KEY_HERE");

            // This enables Exceptionless to gather more detailed information about unhandled exceptions and other events
            services.AddHttpContextAccessor();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            // Adds Exceptionless middleware to listen for unhandled exceptions
            app.UseExceptionless();

            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}