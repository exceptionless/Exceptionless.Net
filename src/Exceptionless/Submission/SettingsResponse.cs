using System;
using Exceptionless.Models;

namespace Exceptionless.Submission {
    public class SettingsResponse {
        internal static SettingsResponse NotModified { get; } = new(success: false, message: "Settings have not been modified.");
        internal static SettingsResponse InvalidConfig { get; } = new(success: false, message: "Invalid configuration settings.");
        internal static SettingsResponse InvalidClientConfig { get; } = new(success: false, message: "Invalid client configuration settings.");

        public SettingsResponse(bool success, SettingsDictionary settings = null, int settingsVersion = -1, Exception exception = null, string message = null) {
            Success = success;
            Settings = settings;
            SettingsVersion = settingsVersion;
            Exception = exception;
            Message = message;
        }

        public bool Success { get; private set; }
        public SettingsDictionary Settings { get; private set; }
        public int SettingsVersion { get; private set; }
        public string Message { get; private set; }
        public Exception Exception { get; private set; }
    }
}