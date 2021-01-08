using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Exceptionless.SampleHosting {
    public class Program {
        public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder => {
                    // By default sends warning and error log messages to Exceptionless.
                    // Log levels can be controlled remotely per log source from the Exceptionless app in near real-time.
                    builder.AddExceptionless();
                })
                .UseExceptionless() // listens for host shutdown and 
                .ConfigureServices(services => {
                    // Reads settings from IConfiguration then adds additional configuration from this lambda.
                    // This also configures ExceptionlessClient.Default
                    services.AddExceptionless(c => c.DefaultData["Startup"] = "heyyy");
                    // OR
                    // services.AddExceptionless();
                    // OR
                    // services.AddExceptionless("API_KEY_HERE");

                    // adds a hosted service that will send sample events to Exceptionless.
                    services.AddHostedService<SampleService>();
                })
                .UseConsoleLifetime()
                .ConfigureWebHostDefaults(builder => {
                    builder.Configure(app => {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => {
                            endpoints.MapGet("/ping", context => {
                                var client = context.RequestServices.GetRequiredService<ExceptionlessClient>();
                                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                                // Submit a feature usage event directly using the client instance.
                                client.SubmitFeatureUsage("MapGet_Ping");

                                // This log message will get sent to Exceptionless since Exceptionless has be added to the logging system in Program.cs.
                                logger.LogWarning("Test warning message from ping");

                                try {
                                    throw new Exception($"Handled Exception: {Guid.NewGuid()}");
                                }
                                catch (Exception handledException) {
                                    // Use the ToExceptionless extension method to submit this handled exception to Exceptionless using the client instance from DI.
                                    handledException.ToExceptionless(client).Submit();
                                }

                                try {
                                    throw new Exception($"Handled Exception (Default Client): {Guid.NewGuid()}");
                                }
                                catch (Exception handledException) {
                                    // Use the ToExceptionless extension method to submit this handled exception to Exceptionless using the default client instance (ExceptionlessClient.Default).
                                    // This works and is convenient, but its generally not recommended to use static singleton instances because it makes testing and
                                    // other things harder.
                                    handledException.ToExceptionless().Submit();
                                }

                                // Unhandled exceptions will get reported since called UseExceptionless in the Startup.cs which registers a listener for unhandled exceptions.
                                throw new Exception($"Unhandled Exception: {Guid.NewGuid()}");
                            });
                        });
                    });
                });
    }
}