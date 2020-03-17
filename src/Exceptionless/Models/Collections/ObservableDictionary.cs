using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Exceptionless.Models.Collections {
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

        public ObservableDictionary() {
            _dictionary = new ConcurrentDictionary<TKey, TValue>();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary) {
            _dictionary = new ConcurrentDictionary<TKey, TValue>(dictionary);
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer) {
            _dictionary = new ConcurrentDictionary<TKey, TValue>(comparer);
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) {
            _dictionary = new ConcurrentDictionary<TKey, TValue>(dictionary, comparer);
        }

        public void Add(TKey key, TValue value) {
            if (_dictionary.TryAdd(key, value))
                OnChanged(new ChangedEventArgs<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value), ChangedAction.Add));
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            if (_dictionary.TryAdd(item.Key, item.Value))
                OnChanged(new ChangedEventArgs<KeyValuePair<TKey, TValue>>(item, ChangedAction.Add));
        }

        public bool Remove(TKey key) {
            TValue value = default(TValue);
            bool success = _dictionary.TryRemove(key, out value);
            if (success)
                OnChanged(new ChangedEventArgs<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value), ChangedAction.Remove));

            return success;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            TValue value;
            bool success = _dictionary.TryRemove(item.Key, out value);
            if (success)
                OnChanged(new ChangedEventArgs<KeyValuePair<TKey, TValue>>(item, ChangedAction.Remove));

            return success;
        }

        public void Clear() {
            _dictionary.Clear();
            OnChanged(new ChangedEventArgs<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(), ChangedAction.Clear));
        }

        public bool ContainsKey(TKey key) {
            return _dictionary.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return _dictionary.ContainsKey(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value) {
            return _dictionary.TryGetValue(key, out value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            ((IDictionary)_dictionary).CopyTo(array, arrayIndex);
        }

        public ICollection<TKey> Keys {
            get { return _dictionary.Keys; }
        }

        public ICollection<TValue> Values {
            get { return _dictionary.Values; }
        }

        public int Count {
            get { return _dictionary.Count; }
        }

        public bool IsReadOnly {
            get { return ((IDictionary)_dictionary).IsReadOnly; }
        }

        public TValue this[TKey key] {
            get { return _dictionary[key]; }
            set {
                ChangedAction action = ContainsKey(key) ? ChangedAction.Update : ChangedAction.Add;

                _dictionary[key] = value;
                OnChanged(new ChangedEventArgs<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value), action));
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public event EventHandler<ChangedEventArgs<KeyValuePair<TKey, TValue>>> Changed;

        protected virtual void OnChanged(ChangedEventArgs<KeyValuePair<TKey, TValue>> args) {
            if (Changed != null)
                Changed(this, args);
        }
    }

    public class ChangedEventArgs<T> : EventArgs {
        public T Item { get; private set; }
        public ChangedAction Action { get; private set; }

        public ChangedEventArgs(T item, ChangedAction action) {
            Item = item;
            Action = action;
        }
    }

    public enum ChangedAction {
        Add,
        Remove,
        Clear,
        Update
    }
}