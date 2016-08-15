using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exceptionless.Extensions {
    public static class StringExtensions {
        public static string ToLowerUnderscoredWords(this string value) {
            var builder = new StringBuilder(value.Length + 10);
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
            if (patternsToMatch == null || value == null)
                return false;

            if (ignoreCase)
                value = value.ToLower();

            return patternsToMatch.Any(pattern => IsPatternMatch(value, pattern, ignoreCase));
        }

        public static bool IsPatternMatch(this string value, string pattern, bool ignoreCase = true) {
            if (pattern == null || value == null)
                return false;

            if (pattern.Equals("*"))
                return true;

            bool startsWithWildcard = pattern.StartsWith("*");
            if (startsWithWildcard)
                pattern = pattern.Substring(1);

            bool endsWithWildcard = pattern.EndsWith("*");
            if (endsWithWildcard)
                pattern = pattern.Substring(0, pattern.Length - 1);

            if (ignoreCase) {
                value = value.ToLower();
                pattern = pattern.ToLower();
            }

            if (startsWithWildcard && endsWithWildcard)
                return value.Contains(pattern);

            if (startsWithWildcard)
                return value.EndsWith(pattern);

            if (endsWithWildcard)
                return value.StartsWith(pattern);

            return value.Equals(pattern);
        }

        public static string[] SplitAndTrim(this string input, params char[] separator) {
            if (String.IsNullOrEmpty(input))
                return new string[0];

            var result = input.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < result.Length; i++)
                result[i] = result[i].Trim();

            return result;
        }

        public static bool ToBoolean(this string input, bool @default = false) {
            if (String.IsNullOrEmpty(input))
                return @default;

            input = input.ToLowerInvariant().Trim();

            bool value;
            if (bool.TryParse(input, out value))
                return value;

            if (String.Equals(input, "yes") || String.Equals(input, "1"))
                return true;

            if (String.Equals(input, "no") || String.Equals(input, "0"))
                return false;

            return @default;
        }

        public static string ToHex(this IEnumerable<byte> bytes) {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

       public static bool IsValidIdentifier(this string value) {
            if (value == null)
                return false;

            for (int index = 0; index < value.Length; index++) {
                if (!Char.IsLetterOrDigit(value[index]) && value[index] != '-')
                    return false;
            }

            return true;
        }
    }
}
