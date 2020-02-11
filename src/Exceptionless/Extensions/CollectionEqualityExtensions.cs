using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptionless {
    internal static class CollectionEqualityExtensions {
        public static bool CollectionEquals<T>(this IEnumerable<T> source, IEnumerable<T> other) {
            if (source == null && other == null)
                return true;
            
            if (source == null || other == null)
                return false;
            
            var sourceEnumerator = source.GetEnumerator();
            var otherEnumerator = other.GetEnumerator();

            while (sourceEnumerator.MoveNext()) {
                if (!otherEnumerator.MoveNext()) {
                    // counts differ
                    return false;
                }

                var sourceValue = sourceEnumerator.Current;
                var otherValue = otherEnumerator.Current;
                if (sourceValue == null && otherValue == null)
                    continue;
                
                if (source == null || other == null || !sourceValue.Equals(otherValue))
                    return false;
            }

            if (otherEnumerator.MoveNext()) {
                // counts differ
                return false;
            }
            
            return true;
        }

        public static bool CollectionEquals<TValue>(this IDictionary<string, TValue> source, IDictionary<string, TValue> other) {
            if (source == null && other == null)
                return true;
            
            if (source == null || other == null || source.Count != other.Count)
                return false;

            foreach (var key in source.Keys) {
                var sourceValue = source[key];

                TValue otherValue;
                if (!other.TryGetValue(key, out otherValue)) {
                    return false;
                }

                if (sourceValue == null && otherValue == null)
                    continue;
                
                if (source == null || other == null || !sourceValue.Equals(otherValue))
                    return false;
            }
            
            return true;
        }
        
        
        public static bool CollectionEquals<TValue>(this ISet<TValue> source, ISet<TValue> other) {
            if (source == null && other == null)
                return true;
            
            if (source == null || other == null || source.Count != other.Count)
                return false;

            return source.SetEquals(other);
        }

        /// <summary>
        /// The hashcode is calculated based on hash of each item regardless of order.
        /// </summary>
        public static int GetCollectionHashCode<T>(this IEnumerable<T> source) {
            string assemblyQualifiedName = typeof(T).AssemblyQualifiedName;
            int hashCode = assemblyQualifiedName == null ? 0 : assemblyQualifiedName.GetHashCode();
            
            var itemHashes = new List<int>();
            foreach (var item in source) {
                if (item == null)
                    continue;

                itemHashes.Add(item.GetHashCode());
            }

            // Sort the hashes
            itemHashes.Sort();
            foreach (int itemHash in itemHashes) {
                unchecked {
                    hashCode = (hashCode * 397) ^ itemHash;
                }
            }
            return hashCode;
        }

        /// <summary>
        /// The hashcode is calculated based on hash of each item regardless of order.
        /// </summary>
        public static int GetCollectionHashCode<TValue>(this IDictionary<string, TValue> source, ISet<string> exclusions = null) {
            string assemblyQualifiedName = typeof(TValue).AssemblyQualifiedName;
            int hashCode = assemblyQualifiedName == null ? 0 : assemblyQualifiedName.GetHashCode();

            var keyValuePairHashes = new List<int>(source.Keys.Count);

            foreach (string key in source.Keys) {
                if (exclusions != null && exclusions.Contains(key))
                    continue;

                var item = source[key];
                unchecked {
                    var kvpHash = key.GetHashCode();
                    kvpHash = (kvpHash * 397) ^ (item == null ? 0 : item.GetHashCode());
                    keyValuePairHashes.Add(kvpHash);
                }
            }

            keyValuePairHashes.Sort();
            foreach (int kvpHash in keyValuePairHashes) {
                unchecked {
                    hashCode = (hashCode * 397) ^ kvpHash;
                }
            }
            return hashCode;
        }
    }
}
