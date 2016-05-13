using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Exceptionless.Extensions {
    public static class StringExtensions {
        public static string ToLowerUnderscoredWords(this string value) {
           string[] tokens = String.Join(" ", SplitPascalCaseWords(value)).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
           if (tokens.Length == 0)
              return String.Empty;

           var sb = new StringBuilder(tokens[0]);
           for (int i = 1; i < tokens.Length; i++) {
              if (tokens[i - 1][tokens[i - 1].Length - 1] != '_' && tokens[i][0] != '_')
                 sb.Append('_');
              sb.Append(tokens[i]);
           }

           return sb.ToString().ToLower();
        }

        private static string[] SplitPascalCaseWords(string value)
        {
           if (value == null)
              throw new ArgumentNullException("value");

           if (value.Length == 0)
              return new string[0];

           char[] chars = value.ToCharArray();
           var words = new List<string>();
           int tokenStart = 0;
           UnicodeCategory currentChar = CharUnicodeInfo.GetUnicodeCategory(chars[tokenStart]);

           for (int i = tokenStart + 1; i < chars.Length; i++) {
              UnicodeCategory nextChar = CharUnicodeInfo.GetUnicodeCategory(chars[i]);
              if (nextChar == currentChar)
                 continue;

              if (currentChar == UnicodeCategory.UppercaseLetter && nextChar == UnicodeCategory.LowercaseLetter) {
                 int newTokenStart = i - 1;
                 if (newTokenStart != tokenStart) {
                    words.Add(new String(chars, tokenStart, newTokenStart - tokenStart));
                    tokenStart = newTokenStart;
                 }
              }
              else if (currentChar == UnicodeCategory.LowercaseLetter && nextChar == UnicodeCategory.UppercaseLetter) {
                 words.Add(new String(chars, tokenStart, i - tokenStart));
                 tokenStart = i;
              }

              currentChar = nextChar;
           }

           words.Add(new String(chars, tokenStart, chars.Length - tokenStart));
           return words.ToArray();
        }

        public static bool AnyWildcardMatches(this string value, IEnumerable<string> patternsToMatch, bool ignoreCase = false) {
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
