using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

// The entrypoint used when the function is deployed to AWS.
namespace Exceptionless.SampleLambdaAspNetCore {
    public class LambdaEntryPoint : Amazon.Lambda.AspNetCoreServer.APIGatewayProxyFunction {
        protected override void Init(IHostBuilder builder) {
            builder.ConfigureLogging(b => {
                // By default sends warning and error log messages to Exceptionless.
                // Log levels can be controlled remotely per log source from the Exceptionless app in near real-time.
                b.AddExceptionless();
            });
        }

        protected override void Init(IWebHostBuilder builder) {
            builder.UseStartup<Startup>();
        }
    }
}