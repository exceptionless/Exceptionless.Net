using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Exceptionless.SampleLambda {
    public class Function
    {
        public async Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            var client = new ExceptionlessClient(c => {
                c.ApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271nTest";
                c.ServerUrl = "http://localhost:5200";

                // read configuration values from environment variables
                c.ReadFromEnvironmentalVariables();
            });

            // will automatically trigger a client.ProcessQueue call when this method completes even if there is an unhandled exception
            await using var _ = new ProcessQueueScope(client);

            client.SubmitFeatureUsage("Serverless Function");

            try {
                throw new Exception("Lambda error");
            } catch (Exception ex) {
                ex.ToExceptionless(client).Submit();
            }

            return input.ToLower();
        }
    }

    internal class ProcessQueueScope : IAsyncDisposable {
        private readonly ExceptionlessClient _exceptionlessClient;

        public ProcessQueueScope(ExceptionlessClient exceptionlessClient) {
            _exceptionlessClient = exceptionlessClient;
        }

        public async ValueTask DisposeAsync() {
            await _exceptionlessClient.ProcessQueueAsync();
        }
    }
}
