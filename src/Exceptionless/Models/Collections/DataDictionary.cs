using System;
using System.Collections.Generic;

namespace Exceptionless.Models {
    public class DataDictionary : Dictionary<string, object> {
        private readonly HashSet<string> _rawJsonKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public DataDictionary() : base(StringComparer.OrdinalIgnoreCase) {}

        public DataDictionary(IEnumerable<KeyValuePair<string, object>> values) : base(StringComparer.OrdinalIgnoreCase) {
            foreach (var kvp in values)
                Add(kvp.Key, kvp.Value);
        }

        public new object this[string key] {
            get => base[key];
            set {
                ClearRawJson(key);
                base[key] = value;
            }
        }

        public new void Add(string key, object value) {
            ClearRawJson(key);
            base.Add(key, value);
        }

        public new bool Remove(string key) {
            ClearRawJson(key);
            return base.Remove(key);
        }

        public new void Clear() {
            _rawJsonKeys.Clear();
            base.Clear();
        }

        public object GetValueOrDefault(string key) {
            return TryGetValue(key, out object value) ? value : null;
        }

        public object GetValueOrDefault(string key, object defaultValue) {
            return TryGetValue(key, out object value) ? value : defaultValue;
        }

        public object GetValueOrDefault(string key, Func<object> defaultValueProvider) {
            return TryGetValue(key, out object value) ? value : defaultValueProvider();
        }

        public string GetString(string name) {
            return GetString(name, String.Empty);
        }

        public string GetString(string name, string @default) {
            if (!TryGetValue(name, out object value))
                return @default;

            if (value is string)
                return (string)value;
                
            return String.Empty;
        }

        internal bool IsRawJson(string key) {
            return !String.IsNullOrEmpty(key) && _rawJsonKeys.Contains(key);
        }

        internal void SetRawJson(string key, string value) {
            base[key] = value;

            if (!String.IsNullOrEmpty(key))
                _rawJsonKeys.Add(key);
        }

        private void ClearRawJson(string key) {
            if (!String.IsNullOrEmpty(key))
                _rawJsonKeys.Remove(key);
        }
    }
}