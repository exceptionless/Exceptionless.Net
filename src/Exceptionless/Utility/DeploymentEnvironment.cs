using System;
using System.Linq;

namespace Exceptionless.Utility {
    internal static class DeploymentEnvironment {
        public static string Normalize(string value) {
            string name = value?.Trim();
            return String.IsNullOrEmpty(name) || name.Length > 64 || name.Any(Char.IsControl) ? null : name.ToLowerInvariant();
        }
    }
}
