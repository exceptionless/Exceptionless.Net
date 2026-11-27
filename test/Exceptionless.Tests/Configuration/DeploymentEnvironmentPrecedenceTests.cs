using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Exceptionless.Configuration;
using Exceptionless.Models;
using Exceptionless.Plugins;
using Exceptionless.Utility;
using Xunit;

namespace Exceptionless.Tests.Configuration {
    public class DeploymentEnvironmentPrecedenceTests {
        [Theory]
        [InlineData("Staging", "Development", "Production", "Staging")]
        [InlineData(null, "Development", "Production", "Development")]
        [InlineData(" ", " Development ", "Production", "Development")]
        [InlineData(null, null, "Production", "Production")]
        [InlineData(null, null, null, null)]
        [InlineData("Staging West", "Development", null, null)]
        public void Detect_UsesFirstSetVariableInOrder(string? aspNetCore, string? dotnet, string? functions, string? expected) {
            var variables = new Dictionary<string, string?> {
                ["ASPNETCORE_ENVIRONMENT"] = aspNetCore,
                ["DOTNET_ENVIRONMENT"] = dotnet,
                ["AZURE_FUNCTIONS_ENVIRONMENT"] = functions
            };

            Assert.Equal(expected, DeploymentEnvironment.Detect(name => variables[name]));
        }

        [Fact]
        public void Constructor_DetectsProcessEnvironmentWithoutMarkingItConfigured() {
            using (new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development")) {
                using var client = new ExceptionlessClient();

                Assert.Equal("Development", client.Configuration.Environment);
                Assert.False(client.Configuration.IsEnvironmentConfigured);
            }
        }

        [Fact]
        public void SetDefaultEnvironment_Null_KeepsDetectedEnvironment() {
            using (new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "Development")) {
                using var client = new ExceptionlessClient();

                client.Configuration.SetDefaultEnvironment(null);
                Assert.Equal("Development", client.Configuration.Environment);

                client.Configuration.SetDefaultEnvironment("Staging");
                Assert.Equal("Staging", client.Configuration.Environment);
            }
        }

        [Fact]
        public void Precedence_EventOverCodeOverConfigurationOverHostOverDetected() {
            using (new EnvironmentVariableScope("DOTNET_ENVIRONMENT", "detected")) {
                using var client = new ExceptionlessClient();
                Assert.Equal("detected", GetEffectiveEnvironment(client));

                client.Configuration.SetDefaultEnvironment("host");
                Assert.Equal("host", GetEffectiveEnvironment(client));

                client.Configuration.SetConfiguredEnvironment("configured");
                client.Configuration.SetDefaultEnvironment("host");
                Assert.Equal("configured", GetEffectiveEnvironment(client));

                client.Configuration.SetEnvironment("code");
                client.Configuration.SetConfiguredEnvironment("configured");
                client.Configuration.SetDefaultEnvironment("host");
                Assert.Equal("code", GetEffectiveEnvironment(client));

                Assert.Equal("event", GetEffectiveEnvironment(client, "event"));
            }
        }

        [Fact]
        public void ReadFromEnvironmentalVariables_ExceptionlessEnvironment_IsConfigured() {
            using (new EnvironmentVariableScope("Exceptionless__Environment", " qa-west ")) {
                ResetEnvironmentVariableCache();
                try {
                    using var client = new ExceptionlessClient();
                    client.Configuration.SetDefaultEnvironment("host");
                    client.Configuration.ReadFromEnvironmentalVariables();
                    client.Configuration.SetDefaultEnvironment("host");

                    Assert.Equal("qa-west", client.Configuration.Environment);
                    Assert.True(client.Configuration.IsEnvironmentConfigured);
                } finally {
                    ResetEnvironmentVariableCache();
                }
            }
        }

        [Theory]
        [InlineData(" Staging ", "Staging", true)]
        [InlineData("Staging West", null, true)]
        [InlineData(null, "host", false)]
        public void ReadFromAttributes_Environment_IsConfigured(string? environment, string? expected, bool configured) {
            using var client = new ExceptionlessClient();
            client.Configuration.SetDefaultEnvironment("host");

            client.Configuration.ReadFromAttributes(new List<Assembly> { CreateAssemblyWithAttribute(environment) });
            client.Configuration.SetDefaultEnvironment("host");

            Assert.Equal(expected, client.Configuration.Environment);
            Assert.Equal(configured, client.Configuration.IsEnvironmentConfigured);
        }

        private static string? GetEffectiveEnvironment(ExceptionlessClient client, string? eventEnvironment = null) {
            var ev = new Event { Type = Event.KnownTypes.Log };
            if (eventEnvironment != null)
                ev.Environment = eventEnvironment;

            var context = new EventPluginContext(client, ev);
            EventPluginManager.Run(context);
            return context.Event.Environment;
        }

        private static Assembly CreateAssemblyWithAttribute(string? environment) {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("EnvironmentAttribute" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            var constructor = typeof(ExceptionlessAttribute).GetConstructor(new[] { typeof(string) })!;
            var properties = environment == null ? Array.Empty<PropertyInfo>() : new[] { typeof(ExceptionlessAttribute).GetProperty(nameof(ExceptionlessAttribute.Environment))! };
            var values = environment == null ? Array.Empty<object>() : new object[] { environment };
            assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { "API_KEY_HERE" }, properties, values));
            return assembly;
        }

        private static void ResetEnvironmentVariableCache() {
            typeof(ExceptionlessConfigurationExtensions)
                .GetField("_environmentVariables", BindingFlags.NonPublic | BindingFlags.Static)!
                .SetValue(null, null);
        }

        private sealed class EnvironmentVariableScope : IDisposable {
            private readonly Dictionary<string, string?> _original = new Dictionary<string, string?>();

            public EnvironmentVariableScope(string name, string value) {
                foreach (string variable in DeploymentEnvironment.DetectionVariables)
                    Set(variable, null);

                Set(name, value);
            }

            private void Set(string name, string? value) {
                if (!_original.ContainsKey(name))
                    _original[name] = Environment.GetEnvironmentVariable(name);

                Environment.SetEnvironmentVariable(name, value);
            }

            public void Dispose() {
                foreach (var variable in _original)
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
        }
    }
}
