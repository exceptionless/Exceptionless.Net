using System;
using System.Text.RegularExpressions;

namespace Exceptionless.Utility {
    internal static class DeploymentEnvironment {
        // ^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$ with \z so a trailing line feed can never match.
        private static readonly Regex _validName = new Regex(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}\z", RegexOptions.CultureInvariant);

        /// <summary>Process environment variables that name the hosting environment, in the order they are checked.</summary>
        internal static readonly string[] DetectionVariables = { "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT", "AZURE_FUNCTIONS_ENVIRONMENT" };

        /// <summary>
        /// Trims the name and returns it with its casing preserved, or null when it is not a valid
        /// deployment environment name (ASCII letters, digits, '.', '_' and '-', starting with a letter
        /// or digit, at most 64 characters).
        /// </summary>
        public static string Normalize(string value) {
            string name = value?.Trim();
            return name != null && _validName.IsMatch(name) ? name : null;
        }

        /// <summary>
        /// Returns the normalized value of the first non-empty hosting environment variable
        /// (ASPNETCORE_ENVIRONMENT, DOTNET_ENVIRONMENT, then AZURE_FUNCTIONS_ENVIRONMENT), or null.
        /// </summary>
        public static string Detect(Func<string, string> getVariable = null) {
            getVariable = getVariable ?? GetProcessVariable;
            foreach (string name in DetectionVariables) {
                string value = getVariable(name);
                if (!String.IsNullOrWhiteSpace(value))
                    return Normalize(value);
            }

            return null;
        }

        private static string GetProcessVariable(string name) {
            try {
                return System.Environment.GetEnvironmentVariable(name);
            } catch (Exception) {
                return null;
            }
        }
    }
}
