using System;
using System.Collections.Generic;
using System.Linq;
using Exceptionless.Extensions;
using Exceptionless.Logging;
using Exceptionless.Models.Collections;

namespace Exceptionless.Models {
    public class SettingsDictionary : ObservableDictionary<string, string> {
        public SettingsDictionary() : base(StringComparer.OrdinalIgnoreCase) {}

        public SettingsDictionary(IEnumerable<KeyValuePair<string, string>> values) : base(StringComparer.OrdinalIgnoreCase) {
            foreach (var kvp in values)
                Add(kvp.Key, kvp.Value);
        }

        public string GetString(string name) {
            return GetString(name, String.Empty);
        }

        public string GetString(string name, string @default) {
            string value;

            if (name != null && TryGetValue(name, out value))
                return value;

            return @default;
        }

        public bool GetBoolean(string name) {
            return GetBoolean(name, false);
        }

        public bool GetBoolean(string name, bool @default) {
            string temp = null;
            if (String.IsNullOrWhiteSpace(name) || !TryGetValue(name, out temp))
                return @default;
            
            if (String.IsNullOrEmpty(temp))
                return @default;

            temp = temp.ToLowerInvariant().Trim();

            bool value;
            if (bool.TryParse(temp, out value))
                return value;

            if (String.Equals(temp, "yes") || String.Equals(temp, "1"))
                return true;
            
            if (String.Equals(temp, "no") || String.Equals(temp, "0"))
                return false;

            return @default;
        }

        public int GetInt32(string name) {
            return GetInt32(name, 0);
        }

        public int GetInt32(string name, int @default) {
            int value;
            string temp = null;

            bool result = name != null && TryGetValue(name, out temp);
            if (!result)
                return @default;

            result = int.TryParse(temp, out value);
            return result ? value : @default;
        }

        public long GetInt64(string name) {
            return GetInt64(name, 0L);
        }

        public long GetInt64(string name, long @default) {
            long value;
            string temp = null;

            bool result = name != null && TryGetValue(name, out temp);
            if (!result)
                return @default;

            result = long.TryParse(temp, out value);
            return result ? value : @default;
        }

        public double GetDouble(string name, double @default = 0d) {
            double value;
            string temp = null;

            bool result = name != null && TryGetValue(name, out temp);
            if (!result)
                return @default;

            result = double.TryParse(temp, out value);
            return result ? value : @default;
        }

        public DateTime GetDateTime(string name) {
            return GetDateTime(name, DateTime.MinValue);
        }

        public DateTime GetDateTime(string name, DateTime @default) {
            DateTime value;
            string temp = null;

            bool result = name != null && TryGetValue(name, out temp);
            if (!result)
                return @default;

            result = DateTime.TryParse(temp, out value);
            return result ? value : @default;
        }

        public DateTimeOffset GetDateTimeOffset(string name) {
            return GetDateTimeOffset(name, DateTimeOffset.MinValue);
        }

        public DateTimeOffset GetDateTimeOffset(string name, DateTimeOffset @default) {
            DateTimeOffset value;
            string temp = null;

            bool result = name != null && TryGetValue(name, out temp);
            if (!result)
                return @default;

            result = DateTimeOffset.TryParse(temp, out value);
            return result ? value : @default;
        }

        public Guid GetGuid(string name) {
            return GetGuid(name, Guid.Empty);
        }

        public Guid GetGuid(string name, Guid @default) {
            string temp = null;

            bool result = name != null && TryGetValue(name, out temp);
            return result ? new Guid(temp) : @default;
        }

        public IEnumerable<string> GetStringCollection(string name) {
            return GetStringCollection(name, null);
        }

        public IEnumerable<string> GetStringCollection(string name, string @default) {
            string value = GetString(name, @default);

            if (String.IsNullOrEmpty(value))
                return new List<string>();

            string[] values = value.Split(new[] { ",", ";", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < values.Length; i++)
                values[i] = values[i].Trim();

            var list = new List<string>(values);
            return list;
        }

        protected override void OnChanged(ChangedEventArgs<KeyValuePair<string, string>> args) {
            if (args.Item.Key == null || !args.Item.Key.StartsWith("@@")) {
                base.OnChanged(args);
                return;
            }

            if (args.Item.Key.StartsWith(KnownKeys.LogLevelPrefix)) {
                var logLevelKeysToRemove = new List<string>();
                foreach (string key in _minLogLevels.Keys) {
                    if (key.IsPatternMatch(args.Item.Key.Substring(6)))
                        logLevelKeysToRemove.Add(key);
                }

                foreach (var logger in logLevelKeysToRemove)
                    _minLogLevels.Remove(logger);
            }

            foreach (var eventType in _eventTypes) {
                Dictionary<string, bool> sourceDictionary;
                if (eventType.Key == null || !_typeSourceEnabled.TryGetValue(eventType.Key, out sourceDictionary))
                    continue;

                if (!args.Item.Key.StartsWith(eventType.Value))
                    continue;

                var sourceKeysToRemove = new List<string>();
                foreach (string key in sourceDictionary.Keys) {
                    if (key.IsPatternMatch(args.Item.Key.Substring(eventType.Value.Length)))
                        sourceKeysToRemove.Add(key);
                }

                foreach (var logger in sourceKeysToRemove)
                    sourceDictionary.Remove(logger);
            }

            base.OnChanged(args);
        }

        private readonly Dictionary<string, LogLevel> _minLogLevels = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);
        public LogLevel GetMinLogLevel(string loggerName) {
            if (String.IsNullOrEmpty(loggerName))
                loggerName = "*";

            LogLevel minLogLevel;
            if (_minLogLevels.TryGetValue(loggerName, out minLogLevel))
                return minLogLevel;

            var setting = GetTypeAndSourceSetting("log", loggerName, "Trace");
            if (setting == null) {
                _minLogLevels[loggerName] = LogLevel.Trace;
                return LogLevel.Trace;
            }

            minLogLevel = LogLevel.FromString(setting);
            _minLogLevels[loggerName] = minLogLevel;
            return minLogLevel;
        }

        private readonly Dictionary<string, Dictionary<string, bool>> _typeSourceEnabled = new Dictionary<string, Dictionary<string, bool>>(StringComparer.OrdinalIgnoreCase);
        public bool GetTypeAndSourceEnabled(string type, string source) {
            if (type == null)
                return true;

            Dictionary<string, bool> sourceDictionary;
            bool sourceEnabled;
            if (source != null && _typeSourceEnabled.TryGetValue(type, out sourceDictionary)) {
                if (sourceDictionary.TryGetValue(source, out sourceEnabled))
                    return sourceEnabled;
            }

            var setting = GetTypeAndSourceSetting(type, source, "true");
            if (!Boolean.TryParse(setting, out sourceEnabled))
                sourceEnabled = true;

            return sourceEnabled;
        }

        private readonly Dictionary<string, string> _eventTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string GetTypeAndSourceSetting(string type, string source, string defaultValue) {
            if (type == null)
                return defaultValue;

            Dictionary<string, bool> sourceDictionary;
            string sourcePrefix;
            if (!_typeSourceEnabled.TryGetValue(type, out sourceDictionary)) {
                sourcePrefix = "@@" + type + ":";
                _eventTypes.Add(type, sourcePrefix);
                sourceDictionary = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                _typeSourceEnabled.Add(type, sourceDictionary);
            } else {
                sourcePrefix = _eventTypes[type];
            }

            // check for exact source match
            string settingValue;
            if (TryGetValue(sourcePrefix + source, out settingValue))
                return settingValue;
            
            // check for wildcard match
            var sourceSettings = this
                .Where(kvp => kvp.Key.StartsWith(sourcePrefix))
                .Select(kvp => new KeyValuePair<string, string>(kvp.Key.Substring(sourcePrefix.Length), kvp.Value))
                .ToList();

            foreach (var kvp in sourceSettings) {
                if (source.IsPatternMatch(kvp.Key))
                    return kvp.Value;
            }

            return defaultValue;
        }

        public void Apply(IEnumerable<KeyValuePair<string, string>> values) {
            if (values == null)
                return;

            foreach (var v in values) {
                if (!ContainsKey(v.Key) || v.Value != this[v.Key])
                    this[v.Key] = v.Value;
            }
        }

        public static class KnownKeys {
            public const string DataExclusions = "@@DataExclusions";
            public const string UserAgentBotPatterns = "@@UserAgentBotPatterns";
            public const string LogLevelPrefix = "@@log:";
        }
    }
}