using System;
using System.Linq;
using System.Reflection;
using Exceptionless.Logging;
using Exceptionless.Models;

namespace Exceptionless.Plugins.Default {
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

            string version = GetVersionFromRuntimeInfo(context.Log);
            if (String.IsNullOrEmpty(version))
                version = GetVersionFromLoadedAssemblies(context.Log);

            if (String.IsNullOrEmpty(version))
                return;

            context.Event.Data[Event.KnownDataKeys.Version] = context.Client.Configuration.DefaultData[Event.KnownDataKeys.Version] = version;
        }

        private string GetVersionFromRuntimeInfo(IExceptionlessLog log) {
#if NETSTANDARD2_0
            try {
                var platformService = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default;
                return platformService.Application.ApplicationVersion;
            } catch (Exception ex) {
                log.FormattedError(typeof(VersionPlugin), ex, "Unable to get Platform Services instance. Error: {0}", ex.Message);
            }
#endif
            return null;
        }

        private string GetVersionFromLoadedAssemblies(IExceptionlessLog log) {
            try {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a != typeof(ExceptionlessClient).GetTypeInfo().Assembly && a != GetType().GetTypeInfo().Assembly && a != typeof(object).GetTypeInfo().Assembly)) {
                    if (String.IsNullOrEmpty(assembly.FullName) || assembly.FullName.StartsWith("System.") || assembly.FullName.StartsWith("Microsoft."))
                        continue;

                    string company = assembly.GetCompany();
                    if (!String.IsNullOrEmpty(company) && (String.Equals(company, "Exceptionless", StringComparison.OrdinalIgnoreCase) || String.Equals(company, "Microsoft Corporation", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    if (!assembly.GetReferencedAssemblies().Any(an => String.Equals(an.FullName, typeof(ExceptionlessClient).GetTypeInfo().Assembly.FullName)))
                        continue;

                    string version = GetVersionFromAssembly(assembly);
                    if (!String.IsNullOrEmpty(version))
                        return version;
                }
            } catch (Exception ex) {
                log.FormattedError(typeof(VersionPlugin), ex, "Unable to get version from loaded assemblies. Error: {0}", ex.Message);
            }

            return null;
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
    }
}
