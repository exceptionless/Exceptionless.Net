using System;
using System.IO;
using System.Linq;
using Exceptionless.Dependency;
using Exceptionless.Logging;
using Exceptionless.Models;
using Exceptionless.Storage;

namespace Exceptionless.Configuration {
    public static class SettingsManager {
        private static bool _isUpdatingSettings = false;

        public static void ApplySavedServerSettings(ExceptionlessConfiguration config) {
            if (config == null)
                return;

            if (String.IsNullOrEmpty(config.ApiKey) || String.Equals(config.ApiKey, "API_KEY_HERE", StringComparison.OrdinalIgnoreCase)) {
                config.Resolver.GetLog().Error(typeof(SettingsManager), "Unable to apply saved server settings: ApiKey is not set.");
                return;
            }

            var savedServerSettings = GetSavedServerSettings(config);
            config.Settings.Apply(savedServerSettings);
        }

        private static SettingsDictionary GetSavedServerSettings(ExceptionlessConfiguration config) {
            if (config == null)
                return new SettingsDictionary();

            string configPath = GetConfigPath(config);
            if (String.IsNullOrEmpty(configPath))
                return new SettingsDictionary();

            try {
                var fileStorage = config.Resolver.GetFileStorage();
                if (!fileStorage.Exists(configPath))
                    return new SettingsDictionary();

                return fileStorage.GetObject<SettingsDictionary>(configPath);
            } catch (Exception ex) {
                config.Resolver.GetLog().FormattedError(typeof(SettingsManager), ex, "Unable to read and apply saved server settings: {0}", ex.Message);
            }

            return new SettingsDictionary();
        }

        public static int GetVersion(ExceptionlessConfiguration config) {
            if (config == null)
                return 0;

            if (String.IsNullOrEmpty(config.ApiKey) || String.Equals(config.ApiKey, "API_KEY_HERE", StringComparison.OrdinalIgnoreCase)) {
                config.Resolver.GetLog().Error(typeof(SettingsManager), "Unable to get version: ApiKey is not set.");
                return 0;
            }

            try {
                var persistedClientData = config.Resolver.Resolve<PersistedDictionary>();
                return persistedClientData.GetInt32(String.Concat(config.GetQueueName(), "-ServerConfigVersion"), 0);
            } catch (Exception ex) {
                config.Resolver.GetLog().Error(typeof(SettingsManager), ex, "Error occurred getting settings version.");
                return 0;
            }
        }
        
        public static void CheckVersion(int version, ExceptionlessConfiguration config) {
            int currentVersion = GetVersion(config);
            if (version <= currentVersion)
                return;

            UpdateSettings(config, currentVersion);
        }

        public static void UpdateSettings(ExceptionlessConfiguration config, int? version = null) {
            if (config == null || !config.IsValid || !config.Enabled || _isUpdatingSettings)
                return;

            try {
                _isUpdatingSettings = true;
                if (!version.HasValue || version < 0)
                    version = GetVersion(config);

                var serializer = config.Resolver.GetJsonSerializer();
                var client = config.Resolver.GetSubmissionClient();

                var response = client.GetSettings(config, version.Value, serializer);
                if (!response.Success || response.Settings == null)
                    return;

                var savedServerSettings = GetSavedServerSettings(config);
                config.Settings.Apply(response.Settings);

                // TODO: Store snapshot of settings after reading from config and attributes and use that to revert to defaults.
                // Remove any existing server settings that are not in the new server settings.
                foreach (string key in savedServerSettings.Keys.Except(response.Settings.Keys))
                    config.Settings.Remove(key);

                var persistedClientData = config.Resolver.Resolve<PersistedDictionary>();
                persistedClientData[String.Concat(config.GetQueueName(), "-ServerConfigVersion")] = response.SettingsVersion.ToString();

                var fileStorage = config.Resolver.GetFileStorage();
                fileStorage.SaveObject(GetConfigPath(config), response.Settings);
            } catch (Exception ex) {
                config.Resolver.GetLog().Error(typeof(SettingsManager), ex, "Error occurred updating settings.");
            } finally {
                _isUpdatingSettings = false;
            }
        }

        private static string GetConfigPath(ExceptionlessConfiguration config) {
            string queueName = config != null ? config.GetQueueName() : String.Empty;
            return Path.Combine(queueName ?? String.Empty, "server-settings.json");
        }
    }
}
