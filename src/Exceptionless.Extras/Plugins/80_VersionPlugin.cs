using System;
using System.Linq;
using System.Reflection;
using Exceptionless.Extras;
using Exceptionless.Models;

namespace Exceptionless.Plugins {
    [Priority(80)]
    public class VersionPlugin : IEventPlugin {
        private static bool _checkedForVersion;

        public void Run(EventPluginContext context) {
            if (context.Event.Data.ContainsKey(Event.KnownDataKeys.Version))
                return;

            object value;
            if (context.Client.Configuration.DefaultData.TryGetValue(Event.KnownDataKeys.Version, out value) && value is string) {
                context.Event.Data[Event.KnownDataKeys.Version] = value;
                return;
            }

            if (_checkedForVersion)
                return;
            
            _checkedForVersion = true;
            string version = null;
            try {
                version = GetVersionFromLoadedAssemblies();
            } catch (Exception) {}

            if (String.IsNullOrEmpty(version))
                return;

            context.Event.Data[Event.KnownDataKeys.Version] = context.Client.Configuration.DefaultData[Event.KnownDataKeys.Version] = version;
        }

        private string GetVersionFromAssembly(Assembly assembly) {
            if (assembly == null)
                return null;

            string version = assembly.GetInformationalVersion();
            if (String.IsNullOrEmpty(version) || String.Equals(version, "0.0.0.0"))
                version = assembly.GetFileVersion();

            if (String.IsNullOrEmpty(version) || String.Equals(version, "0.0.0.0"))
                version = assembly.GetVersion();
            
            if (String.IsNullOrEmpty(version) || String.Equals(version, "0.0.0.0")) {
                var assemblyName = assembly.GetAssemblyName();
                version = assemblyName != null ? assemblyName.Version.ToString() : null;
            }

            return !String.IsNullOrEmpty(version) && !String.Equals(version, "0.0.0.0") ? version : null;
        }

        private string GetVersionFromLoadedAssemblies() {
#if !NETSTANDARD1_5
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a != typeof(ExceptionlessClient).Assembly && a != GetType().Assembly && a != typeof(object).Assembly)) {
                if (String.IsNullOrEmpty(assembly.FullName) || assembly.FullName.StartsWith("System.") || assembly.FullName.StartsWith("Microsoft."))
                    continue;

                string company = assembly.GetCompany();
                if (!String.IsNullOrEmpty(company) && (String.Equals(company, "Exceptionless", StringComparison.OrdinalIgnoreCase) || String.Equals(company, "Microsoft Corporation", StringComparison.OrdinalIgnoreCase)))
                    continue;
            
                if (!assembly.GetReferencedAssemblies().Any(an => String.Equals(an.FullName, typeof(ExceptionlessClient).Assembly.FullName)))
                    continue;

                string version = GetVersionFromAssembly(assembly);
                if (!String.IsNullOrEmpty(version))
                    return version;
            }
#endif

            return null;
        }
    }
}