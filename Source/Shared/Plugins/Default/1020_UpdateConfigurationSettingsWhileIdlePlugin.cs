using System;
using System.Threading;
using Exceptionless.Configuration;
using Exceptionless.Dependency;
using Exceptionless.Logging;

namespace Exceptionless.Plugins.Default {
    [Priority(1020)]
    public class UpdateConfigurationSettingsWhileIdlePlugin : IEventPlugin, IDisposable {
        private readonly Timer _timer;
        private readonly TimeSpan _interval;
        private readonly ExceptionlessConfiguration _configuration;

        /// <summary>
        /// Automatically sync the configuration settings when the client hasn't sent any events for a period of time.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="interval">The interval at which the client will automatically ensure the configuration settings are up-to-date when idle.</param>
        public UpdateConfigurationSettingsWhileIdlePlugin(ExceptionlessConfiguration configuration, TimeSpan? interval = null) {
            _configuration = configuration;

            var startupInterval = interval ?? TimeSpan.FromSeconds(5);
            _interval = interval.HasValue && interval.Value.Ticks > 0 ? interval.Value : TimeSpan.FromMinutes(5);
            _timer = new Timer(UpdateConfiguration, null, startupInterval, _interval);
        }

        public void Run(EventPluginContext context) {
            _timer.Change(_interval, _interval);
        }

        private void UpdateConfiguration(object state) {
            try {
                SettingsManager.UpdateSettings(_configuration);
            } catch (Exception ex) {
                var log = _configuration.Resolver.GetLog();
                log.Error(ex, "Error while updating configuration settings.");
            }
        }

        public void Dispose() {
            _timer.Dispose();
        }
    }
}