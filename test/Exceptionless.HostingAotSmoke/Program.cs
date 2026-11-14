using System;
using System.Threading;
using Exceptionless;
using Exceptionless.Dependency;
using Exceptionless.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings {
    DisableDefaults = true
});
builder.AddExceptionless(configuration => {
    configuration.ApiKey = "00000000000000000000000000000000";
    configuration.IncludeModules = false;
    configuration.IncludePrivateInformation = false;
    configuration.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
    configuration.UseInMemoryStorage();
});
builder.UseExceptionless();

using var host = builder.Build();
using var startCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await host.StartAsync(startCancellation.Token);

var client = host.Services.GetRequiredService<ExceptionlessClient>();
var serializer = client.Configuration.Resolver.GetJsonSerializer();
string json = serializer.Serialize(new Event {
    Type = Event.KnownTypes.Log,
    Source = "aot-hosting-smoke",
    Message = "Generic Host NativeAOT"
});

if (!json.Contains("\"source\":\"aot-hosting-smoke\""))
    throw new InvalidOperationException("Hosted client serialization failed.");

using var stopCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await host.StopAsync(stopCancellation.Token);
Console.WriteLine("AOT_HOSTING_SMOKE_OK");
