using Exceptionless;
using Exceptionless.SampleBlazorWebAssemblyApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

ExceptionlessClient.Default.Configuration.ServerUrl = "http://localhost:5200";
ExceptionlessClient.Default.Startup("LhhP1C9gijpSKCslHHCvwdSIz298twx271nTest");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
