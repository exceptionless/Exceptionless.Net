using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Exceptionless.SampleAspNetCore {
    public class Startup {
        public Startup(IWebHostEnvironment env) {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services) {
            services.AddLogging(b => b
                .AddConfiguration(Configuration.GetSection("Logging"))
                .AddDebug()
                .AddConsole()
                .AddExceptionless());
            services.AddHttpContextAccessor();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory) {
            app.UseExceptionless(Configuration);
            //OR
            //app.UseExceptionless(new ExceptionlessClient(c => c.ReadFromConfiguration(Configuration)));
            //OR
            //app.UseExceptionless("API_KEY_HERE");
            //OR
            //loggerFactory.AddExceptionless("API_KEY_HERE");
            //OR
            //loggerFactory.AddExceptionless((c) => c.ReadFromConfiguration(Configuration));

            //loggerFactory.AddExceptionless();
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}