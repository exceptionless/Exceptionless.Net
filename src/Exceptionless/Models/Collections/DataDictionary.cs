using System;
using System.Collections;
using System.Collections.Generic;

namespace Exceptionless.Models {
    public class DataDictionary : Dictionary<string, object>, IDictionary<string, object>, IDictionary {
        private readonly Dictionary<string, string> _rawJsonValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
            _rawJsonValues.Clear();
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

        internal bool IsRawJson(string key, object value) {
            return !String.IsNullOrEmpty(key)
                && value is string stringValue
                && _rawJsonValues.TryGetValue(key, out string rawJson)
                && ReferenceEquals(stringValue, rawJson);
        }

        internal void SetRawJson(string key, string value) {
            base[key] = value;

            if (!String.IsNullOrEmpty(key))
                _rawJsonValues[key] = value;
        }

        private void ClearRawJson(string key) {
            if (!String.IsNullOrEmpty(key))
                _rawJsonValues.Remove(key);
        }

        object IDictionary<string, object>.this[string key] {
            get => this[key];
            set => this[key] = value;
        }

        ICollection<string> IDictionary<string, object>.Keys => base.Keys;
        ICollection<object> IDictionary<string, object>.Values => base.Values;
        int ICollection<KeyValuePair<string, object>>.Count => base.Count;
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

        void IDictionary<string, object>.Add(string key, object value) => Add(key, value);
        bool IDictionary<string, object>.ContainsKey(string key) => base.ContainsKey(key);
        bool IDictionary<string, object>.Remove(string key) => Remove(key);
        bool IDictionary<string, object>.TryGetValue(string key, out object value) => base.TryGetValue(key, out value);

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);
        void ICollection<KeyValuePair<string, object>>.Clear() => Clear();
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) {
            return base.TryGetValue(item.Key, out object value)
                && EqualityComparer<object>.Default.Equals(value, item.Value);
        }
        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
            foreach (var item in this)
                array[arrayIndex++] = item;
        }
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) {
            if (!((ICollection<KeyValuePair<string, object>>)this).Contains(item))
                return false;

            return Remove(item.Key);
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => base.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => base.GetEnumerator();

        object IDictionary.this[object key] {
            get => key is string stringKey && base.TryGetValue(stringKey, out object value) ? value : null;
            set {
                if (!(key is string stringKey))
                    throw new ArgumentException("DataDictionary keys must be strings.", nameof(key));

                this[stringKey] = value;
            }
        }

        ICollection IDictionary.Keys => base.Keys;
        ICollection IDictionary.Values => base.Values;
        bool IDictionary.IsReadOnly => false;
        bool IDictionary.IsFixedSize => false;
        int ICollection.Count => base.Count;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        void IDictionary.Add(object key, object value) {
            if (!(key is string stringKey))
                throw new ArgumentException("DataDictionary keys must be strings.", nameof(key));

            Add(stringKey, value);
        }
        void IDictionary.Clear() => Clear();
        bool IDictionary.Contains(object key) => key is string stringKey && base.ContainsKey(stringKey);
        IDictionaryEnumerator IDictionary.GetEnumerator() => new DataDictionaryEnumerator(base.GetEnumerator());
        void IDictionary.Remove(object key) {
            if (key is string stringKey)
                Remove(stringKey);
        }
        void ICollection.CopyTo(Array array, int index) {
            foreach (var item in this)
                array.SetValue(new DictionaryEntry(item.Key, item.Value), index++);
        }

        private sealed class DataDictionaryEnumerator : IDictionaryEnumerator {
            private readonly IEnumerator<KeyValuePair<string, object>> _inner;

            public DataDictionaryEnumerator(IEnumerator<KeyValuePair<string, object>> inner) {
                _inner = inner;
            }

            public DictionaryEntry Entry => new DictionaryEntry(Key, Value);
            public object Key => _inner.Current.Key;
            public object Value => _inner.Current.Value;
            public object Current => Entry;
            public bool MoveNext() => _inner.MoveNext();
            public void Reset() => _inner.Reset();
        }
    }
}
