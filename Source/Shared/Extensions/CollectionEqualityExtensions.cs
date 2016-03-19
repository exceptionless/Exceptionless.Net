using System;
using System.Collections.Generic;
using System.Linq;

namespace Exceptionless
{
    internal static class CollectionEqualityExtensions
    {
        public static bool CollectionEquals<T>(this IEnumerable<T> source, IEnumerable<T> other) {
            var sourceEnumerator = source.GetEnumerator();
            var otherEnumerator = other.GetEnumerator();

            while (sourceEnumerator.MoveNext()) {
                if (!otherEnumerator.MoveNext()) {
                    // counts differ
                    return false;
                }

                if (sourceEnumerator.Current.Equals(otherEnumerator.Current)) {
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

        public static bool CollectionEquals<TValue>(this IDictionary<string, TValue> source, IDictionary<string, TValue> other)
        {
            if (source.Count != other.Count) {
                return false;
            }

            foreach (var key in source.Keys) {
                var sourceValue = source[key];

                TValue otherValue;
                if (!other.TryGetValue(key, out otherValue)) {
                    return false;
                }

                if (sourceValue.Equals(otherValue)) {
                    return false;
                }
            }
            return true;
        }

        public static int GetCollectionHashCode<T>(this IEnumerable<T> source) {
            var assemblyQualifiedName = typeof(T).AssemblyQualifiedName;
            int hashCode = assemblyQualifiedName == null ? 0 : assemblyQualifiedName.GetHashCode();

            foreach (var item in source) {
                if(item == null) continue;

                unchecked {
                    hashCode = (hashCode * 397) ^ item.GetHashCode();
                }
            }
            return hashCode;
        }

        public static int GetCollectionHashCode<TValue>(this IDictionary<string, TValue> source)
        {
            var assemblyQualifiedName = typeof(TValue).AssemblyQualifiedName;
            int hashCode = assemblyQualifiedName == null ? 0 : assemblyQualifiedName.GetHashCode();

            var keyValuePairHashes = new List<int>(source.Keys.Count);

            foreach (var key in source.Keys.OrderBy(x => x)) {
                var item = source[key];
                unchecked {
                    var kvpHash = key.GetHashCode();
                    kvpHash = (kvpHash * 397) ^ item.GetHashCode();
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
