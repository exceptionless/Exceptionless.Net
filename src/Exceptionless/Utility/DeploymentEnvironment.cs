using System;

namespace Exceptionless.Utility {
    internal static class DeploymentEnvironment {
        public static string Normalize(string value) {
            string name = value?.Trim();
            if (String.IsNullOrEmpty(name) || name.Length > 64)
                return null;

            foreach (char character in name) {
                if (Char.IsControl(character))
                    return null;
            }

            return name;
        }
    }
}
