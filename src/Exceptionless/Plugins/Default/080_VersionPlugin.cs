using System;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Utility;

namespace Exceptionless.Plugins.Default {
    [Priority(80)]
    public class VersionPlugin : IEventPlugin {
        private static bool _checkedForVersion;

        public void Run(EventPluginContext context) {
            if (context.Event.Data.ContainsKey(Event.KnownDataKeys.Version))
                return;

            if (context.Client.Configuration.DefaultData.TryGetValue(Event.KnownDataKeys.Version, out object value) && value is string) {
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

            var entryAssembly = AssemblyHelper.GetEntryAssembly(log);

            try {
                string version = AssemblyHelper.GetVersionFromAssembly(entryAssembly);
                if (!String.IsNullOrEmpty(version)) {
                    _appVersion = version;
                    _appVersionLoaded = true;

                    return _appVersion;
                }
            } catch (Exception ex) {
                log.FormattedError(typeof(VersionPlugin), ex, "Unable to get version from loaded assemblies. Error: {0}", ex.Message);
            }

            _appVersion = null;
            _appVersionLoaded = true;

            return null;
        }

    }
}
