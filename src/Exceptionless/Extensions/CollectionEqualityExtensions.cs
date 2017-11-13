using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptionless {
    internal static class CollectionEqualityExtensions {
        public static bool CollectionEquals<T>(this IEnumerable<T> source, IEnumerable<T> other) {
            if (ReferenceEquals(null, source) && ReferenceEquals(null, other)) {
                return true;
            }

            if (ReferenceEquals(null, source) || ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(source, other)) {
                return true;
            }

            var sourceEnumerator = source.GetEnumerator();
            var otherEnumerator = other.GetEnumerator();

            while (sourceEnumerator.MoveNext()) {
                if (!otherEnumerator.MoveNext()) {
                    // counts differ
                    return false;
                }

                if (!Equals(sourceEnumerator.Current, otherEnumerator.Current)) {
                    // values aren't equal
                    return false;
                }
            }

            if (otherEnumerator.MoveNext()) {
                // counts differ
                return false;
            }
            return true;
        }

        public static bool CollectionEquals<TKey,TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> other) {
            if (source.Count != other.Count) {
                return false;
            }

            foreach (var key in source.Keys) {
                var sourceValue = source[key];

                TValue otherValue;
                if (!other.TryGetValue(key, out otherValue)) {
                    return false;
                }

                if (!Equals(sourceValue, otherValue)) {
                    return false;
                }
            }
            return true;
        }

        public static int GetCollectionHashCode<T>(this IEnumerable<T> source) {
            var assemblyQualifiedName = typeof(T).AssemblyQualifiedName;
            int hashCode = assemblyQualifiedName == null ? 0 : assemblyQualifiedName.GetHashCode();

            foreach (var item in source) {
                if (item == null)
                    continue;

                unchecked {
                    hashCode = (hashCode * 397) ^ item.GetHashCode();
                }
            }
            return hashCode;
        }

        public static int GetCollectionHashCode<TValue>(this IDictionary<string, TValue> source, IList<string> exclusions = null) {
            var assemblyQualifiedName = typeof(TValue).AssemblyQualifiedName;
            int hashCode = assemblyQualifiedName == null ? 0 : assemblyQualifiedName.GetHashCode();

            var keyValuePairHashes = new List<int>(source.Keys.Count);

            foreach (var key in source.Keys.OrderBy(x => x)) {
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
            foreach (var kvpHash in keyValuePairHashes) {
                unchecked {
                    hashCode = (hashCode * 397) ^ kvpHash;
                }
            }
            return hashCode;
        }
    }
}
