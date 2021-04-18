using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Exceptionless.Extensions {
    public static class StringExtensions {
        [ThreadStatic] private static StringBuilder? s_builder;
        private static ConcurrentDictionary<string, string> LookUp => s_lookUp ?? CreateLookup();
        private static ConcurrentDictionary<string, string> s_lookUp;

        private static ConcurrentDictionary<string, string> CreateLookup() {
            var lookup = Interlocked.CompareExchange(ref s_lookUp, new ConcurrentDictionary<string, string>(), null);
            if (lookup is null) {
                // We won the set race; setup Gen2 cleanup
                lookup = s_lookUp;

                Gen2GcCallback.Register(() => {
                    // Setup callback to drop the cache at Gen2 so it doesn't keep growing.
                    s_lookUp = null;
                    // We don't auto re-register here as that will cause an infinite GC loop on process exit
                    // for Framework as it will try to guarantee the CriticalFinalizerObject always runs.
                    return false;
                });
            }

            return lookup;
        }

        public static string ToLowerUnderscoredWords(this string name) {
            if (string.IsNullOrEmpty(name)) return name;

            var lookUp = LookUp;
            if (lookUp.TryGetValue(name, out string value)) return value;

            ref var builder = ref s_builder;
            var sb = builder ?? new StringBuilder(name.Length + 5);
            // Null out the StringBuilder so if an exception is thrown it doesn't start partially filled
            builder = null;

            value = ToLowerUnderscoredWordsCreate(name, sb);
            lookUp[name] = value;

            if (value.Length <= 100) {
                // String is small, let's reuse the StringBuilder
                sb.Clear();
                builder = sb;
            }

            return value;
        }

        private static string ToLowerUnderscoredWordsCreate(string value, StringBuilder builder) {
            for (int index = 0; index < value.Length; index++) {
                char c = value[index];
                if (Char.IsUpper(c)) {
                    if (index > 0 && value[index - 1] != '_')
                        builder.Append('_');

                    builder.Append(Char.ToLower(c));
                } else {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public static bool AnyWildcardMatches(this string value, IEnumerable<string> patternsToMatch, bool ignoreCase = true) {
            if (patternsToMatch is null || value is null)
                return false;

            return patternsToMatch.Any(pattern => IsPatternMatch(value, pattern, ignoreCase));
        }

        public static bool IsPatternMatch(this string value, string pattern, bool ignoreCase = true) {
            if (pattern == null)
                return value == null;

            if (pattern.Equals("*"))
                return true;

            if (value == null)
                return false;

            bool startsWithWildcard = pattern.StartsWith("*");
            if (startsWithWildcard)
                pattern = pattern.Substring(1);

            bool endsWithWildcard = pattern.EndsWith("*");
            if (endsWithWildcard)
                pattern = pattern.Substring(0, pattern.Length - 1);
            
            var comparison = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            if (startsWithWildcard && endsWithWildcard)
                return value.IndexOf(pattern ?? "", comparison) >= 0;

            if (startsWithWildcard)
                return value.EndsWith(pattern, comparison);

            if (endsWithWildcard)
                return value.StartsWith(pattern, comparison);

            return String.Equals(value, pattern, comparison);
        }

        public static string[] SplitAndTrim(this string input, params char[] separator) {
            if (String.IsNullOrEmpty(input))
                return Array.Empty<string>();

            var result = input.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < result.Length; i++)
                result[i] = result[i].Trim();

            return result;
        }

        public static bool ToBoolean(this string input, bool @default = false) {
            if (String.IsNullOrEmpty(input))
                return @default;

            input = input.ToLowerInvariant().Trim();

            if (bool.TryParse(input, out bool value))
                return value;

            if (String.Equals(input, "yes") || String.Equals(input, "1"))
                return true;

            if (String.Equals(input, "no") || String.Equals(input, "0"))
                return false;

            return @default;
        }

        public static string ToHex(this byte[] bytes) {
            var hex = new char[bytes.Length * 2];

            int i = 0;
            foreach (byte b in bytes) {
                hex[i] = ToLowerHexChar(b >> 4);
                hex[i + 1] = ToLowerHexChar(b);
                i += 2;
            }

            return new string(hex);
        }

        private static char ToLowerHexChar(int value) {
            value &= 0xF;
            value += '0';

            if (value > '9') {
                value += ('a' - ('9' + 1));
            }

            return (char)value;
        }

        public static bool IsValidIdentifier(this string value) {
            if (value is null)
                return false;

            foreach (var ch in value)
            {
                if (!Char.IsLetterOrDigit(ch) && ch != '-')
                    return false;
            }

            return true;
        }
    }
}
