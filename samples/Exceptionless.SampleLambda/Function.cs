using System;
using Amazon.Lambda.Core;
using Exceptionless;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Exceptionless.SampleLambda {
    public class Function
    {
        public string FunctionHandler(string input, ILambdaContext context)
        {
            var client = new ExceptionlessClient(c => {
                c.ApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw";
                c.ServerUrl = "http://localhost:5000";

                // read configuration values from environment variables
                c.ReadFromEnvironmentalVariables();
            });

            // will automatically trigger a client.ProcessQueue call when this method completes even if there is an unhandled exception
            using var _ = client.ProcessQueueDeferred();

            client.SubmitFeatureUsage("Serverless Function");

            try {
                throw new Exception("Lambda error");
            } catch (Exception ex) {
                ex.ToExceptionless(client).Submit();
            }

            return input.ToLower();
        }
    }
}
