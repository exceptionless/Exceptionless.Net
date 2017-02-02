using System;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Owin;

namespace Exceptionless.SampleWebApi {
    public class Startup {
        public void Configuration(IAppBuilder app) {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute(name: "DefaultApi", routeTemplate: "api/{controller}/{id}", defaults: new {
                id = RouteParameter.Optional
            });
            app.UseWebApi(config);

            ExceptionlessClient.Default.Configuration.UseTraceLogger();
            ExceptionlessClient.Default.RegisterWebApi(config);
        }
    }

    public class Program {
        public static void Main(string[] args) {
            string baseAddress = "http://localhost:9000/";

            using (WebApp.Start<Startup>(url: baseAddress)) {
                Console.WriteLine("Press any key to send a request...");
                ConsoleKeyInfo key = Console.ReadKey();
                while (key.KeyChar != 27) {
                    // Create HttpCient and make a request to api/values
                    var client = new HttpClient();
                    HttpResponseMessage response = client.GetAsync(baseAddress + "api/values").GetAwaiter().GetResult();

                    Console.WriteLine(response);
                    Console.WriteLine(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                    key = Console.ReadKey();
                }
            }
        }
    }
}