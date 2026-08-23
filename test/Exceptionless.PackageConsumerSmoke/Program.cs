using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using Exceptionless;
using Exceptionless.Dependency;
using Exceptionless.Models;
using Exceptionless.Serializer;
using Microsoft.Extensions.DependencyInjection;

internal static class Program {
    private static int Main() {
#if NET472
        const string expectedExceptionlessTarget = ".NETFramework,Version=v4.6.2";
#elif NET7_0
        const string expectedExceptionlessTarget = ".NETStandard,Version=v2.0";
#else
#error Update the package asset expectation when adding a smoke-test target framework.
#endif

        var targetFramework = typeof(ExceptionlessClient).Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        Assert(targetFramework == expectedExceptionlessTarget,
            $"Expected Exceptionless asset '{expectedExceptionlessTarget}', but loaded '{targetFramework ?? "<unknown>"}'.");

        var marker = new PackageSmokeMarker();
        var services = new ServiceCollection();
        services.AddSingleton<IPackageSmokeMarker>(marker);

        using (var client = new ExceptionlessClient(services, configuration => {
            configuration.ApiKey = "00000000000000000000000000000000";
            configuration.IncludeModules = false;
            configuration.IncludePrivateInformation = false;
            configuration.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
            configuration.UseInMemoryStorage();
        })) {
            var resolvedMarker = client.Configuration.Resolver.Resolve<IPackageSmokeMarker>();
            Assert(ReferenceEquals(marker, resolvedMarker), "Microsoft DI did not resolve the package consumer registration.");

            var serializer = client.Configuration.Resolver.GetJsonSerializer();
            var storageSerializer = client.Configuration.Resolver.GetStorageSerializer();
            var original = new Event {
                Type = Event.KnownTypes.Log,
                Source = "package-consumer-smoke",
                Message = "Packed package DI and serializer round-trip",
                Data = { ["answer"] = 42 }
            };

            string json = serializer.Serialize(original);
            Assert(json.Contains("\"source\":\"package-consumer-smoke\""), "JSON serialization lost the event source.");

            var jsonRoundTrip = (Event)serializer.Deserialize(json, typeof(Event));
            Assert(jsonRoundTrip.Source == original.Source, "JSON deserialization lost the event source.");
            Assert(Convert.ToInt64(jsonRoundTrip.Data["answer"]) == 42L, "JSON deserialization lost extended data.");

            using (var stream = new MemoryStream()) {
                storageSerializer.Serialize(original, stream);
                stream.Position = 0;
                var storageRoundTrip = storageSerializer.Deserialize<Event>(stream);
                Assert(storageRoundTrip.Source == original.Source, "Storage serialization lost the event source.");
                Assert(Convert.ToInt64(storageRoundTrip.Data["answer"]) == 42L, "Storage serialization lost extended data.");
            }
        }

        Console.WriteLine($"PACKAGE_CONSUMER_SMOKE_OK {targetFramework}");
        return 0;
    }

    private static void Assert(bool condition, string message) {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private interface IPackageSmokeMarker { }

    private sealed class PackageSmokeMarker : IPackageSmokeMarker { }
}
