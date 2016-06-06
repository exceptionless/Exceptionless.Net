using System;
using Exceptionless.Models;

namespace Exceptionless.Submission {
    public class SettingsResponse {
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