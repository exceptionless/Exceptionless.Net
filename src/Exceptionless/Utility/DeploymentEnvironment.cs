using System;
using System.Text.RegularExpressions;

namespace Exceptionless.Utility {
    internal static class DeploymentEnvironment {
        // ^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$ with \z so a trailing line feed can never match.
        private static readonly Regex _validName = new Regex(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}\z", RegexOptions.CultureInvariant);

        /// <summary>
        /// Trims the name and returns it with its casing preserved, or null when it is not a valid
        /// deployment environment name (ASCII letters, digits, '.', '_' and '-', starting with a letter
        /// or digit, at most 64 characters).
        /// </summary>
        public static string Normalize(string value) {
            string name = value?.Trim();
            return name != null && _validName.IsMatch(name) ? name : null;
        }
    }
}
