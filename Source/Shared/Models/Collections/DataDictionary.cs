using System;
using System.Collections.Generic;

namespace Exceptionless.Models {
    public class DataDictionary : Dictionary<string, object> {
        public DataDictionary() : base(StringComparer.OrdinalIgnoreCase) {}

        public DataDictionary(IEnumerable<KeyValuePair<string, object>> values) : base(StringComparer.OrdinalIgnoreCase) {
            foreach (var kvp in values)
                Add(kvp.Key, kvp.Value);
        }

        public object GetValueOrDefault(string key) {
            object value;
            return TryGetValue(key, out value) ? value : null;
        }

        public object GetValueOrDefault(string key, object defaultValue) {
            object value;
            return TryGetValue(key, out value) ? value : defaultValue;
        }

        public object GetValueOrDefault(string key, Func<object> defaultValueProvider) {
            object value;
            return TryGetValue(key, out value) ? value : defaultValueProvider();
        }

        public string GetString(string name) {
            return GetString(name, String.Empty);
        }

        public string GetString(string name, string @default) {
            object value;

            if (!TryGetValue(name, out value))
                return @default;

            if (value is string)
                return (string)value;
                
            return String.Empty;
        }

        protected bool ShouldBeExcludedFromHash(string key) {
            switch (key) {
                case Event.KnownDataKeys.TraceLog:
                    return true;
            }

            return false;
        }

        protected bool Equals(DataDictionary other) {
            foreach (var key in Keys) {
                if (ShouldBeExcludedFromHash(key))
                    continue;

                object value = this[key];

                object otherValue;
                if (!other.TryGetValue(key, out otherValue))
                    return false;

                if (ReferenceEquals(null, value))
                    return false;

                if (!value.Equals(otherValue))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((DataDictionary)obj);
        }

        public override int GetHashCode() {
            int hashCode = 0;
            foreach (var key in Keys)
            {
                if (ShouldBeExcludedFromHash(key))
                    continue;

                object value = this[key];

                unchecked {
                    if (value != null) {
                        hashCode = (hashCode * 397) ^ value.GetHashCode();
                    }
                }
            }

            return hashCode;
        }
    }
}