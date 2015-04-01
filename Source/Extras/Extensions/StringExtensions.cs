using System;

namespace Exceptionless.Extras {
    internal static class StringExtensions {
        public static string[] SplitAndTrim(this string input, params char[] separator) {
            if (String.IsNullOrEmpty(input))
                return new string[0];

            var result = input.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < result.Length; i++)
                result[i] = result[i].Trim();

            return result;
        }
    }
}