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

            string version = GetVersion(context.Log);
            if (String.IsNullOrEmpty(version))
                version = GetVersion(context.Log);

            if (String.IsNullOrEmpty(version))
                return;

            context.Event.Data[Event.KnownDataKeys.Version] = context.Client.Configuration.DefaultData[Event.KnownDataKeys.Version] = version;
        }

        private bool _appVersionLoaded = false;
        private string _appVersion = null;

        private string GetVersion(IExceptionlessLog log) {
            if (_appVersionLoaded)
                return _appVersion;

            var entryAssembly = GetEntryAssembly(log);

            try {
                string version = GetVersionFromAssembly(entryAssembly);
                if (!String.IsNullOrEmpty(version)) {
                    _appVersion = version;
                    _appVersionLoaded = true;

                    return _appVersion;
                }
            } catch (Exception ex) {
                log.FormattedError(typeof(VersionPlugin), ex, "Unable to get version from loaded assemblies. Error: {0}", ex.Message);
            }

#if NETSTANDARD2_0
            try {
                var platformService = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default;

                _appVersion = platformService.Application.ApplicationVersion;
                _appVersionLoaded = true;

                return _appVersion;
            } catch (Exception ex) {
                log.FormattedError(typeof(VersionPlugin), ex, "Unable to get Platform Services instance. Error: {0}", ex.Message);
            }
#endif

            _appVersion = null;
            _appVersionLoaded = true;

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

        private Assembly GetEntryAssembly(IExceptionlessLog log) {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (IsUserAssembly(entryAssembly))
                return entryAssembly;

            try {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => 
                    !a.IsDynamic
                    && a != typeof(ExceptionlessClient).GetTypeInfo().Assembly
                    && a != GetType().GetTypeInfo().Assembly
                    && a != typeof(object).GetTypeInfo().Assembly);

                return assemblies.FirstOrDefault(a => IsUserAssembly(a));
            } catch (Exception ex) {
                log.FormattedError(typeof(VersionPlugin), ex, "Unable to get entry assembly. Error: {0}", ex.Message);
            }

            return null;
        }

        private bool IsUserAssembly(Assembly assembly) {
            if (assembly == null)
                return false;

            if (!String.IsNullOrEmpty(assembly.FullName) && (assembly.FullName.StartsWith("System.") || assembly.FullName.StartsWith("Microsoft.")))
                return false;

            string company = assembly.GetCompany() ?? String.Empty;
            string[] nonUserCompanies = new[] { "Exceptionless", "Microsoft" };
            if (nonUserCompanies.Any(c => company.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0))
                return false;

            if (assembly.FullName == typeof(ExceptionlessClient).GetTypeInfo().Assembly.FullName)
                return false;

            return true;
        }
    }
}
