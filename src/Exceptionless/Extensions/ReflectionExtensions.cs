using System;
using System.IO;
using System.Reflection;

namespace Exceptionless {
    internal static class ReflectionExtensions {
        public static AssemblyName GetAssemblyName(this Assembly assembly) {
            try {
                return assembly.GetName();
            } catch { }

            return new AssemblyName(assembly.FullName);
        }

        public static DateTime? GetCreationTime(this Assembly assembly) {
            try {
                return File.GetCreationTimeUtc(assembly.Location);
            } catch {}

            return null;
        }

        public static DateTime? GetLastWriteTime(this Assembly assembly) {
            try {
                return File.GetLastWriteTimeUtc(assembly.Location);
            } catch {}

            return null;
        }

        public static string GetVersion(this Assembly assembly) {
            var attr = assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute)) as AssemblyVersionAttribute;
            if (attr != null)
                return attr.Version;

            return assembly.GetAssemblyName().Version.ToString();
        }

        public static string GetFileVersion(this Assembly assembly) {
            return assembly.GetVersionAttributeValue<AssemblyFileVersionAttribute>(static attribute => attribute.Version);
        }

        public static string GetInformationalVersion(this Assembly assembly) {
            return assembly.GetVersionAttributeValue<AssemblyInformationalVersionAttribute>(static attribute => attribute.InformationalVersion);
        }

        private static string GetVersionAttributeValue<T>(this Assembly assembly, Func<T, string> getValue) where T : Attribute {
            var matches = assembly.GetCustomAttributes(typeof(T), inherit: false);
            if (matches.Length is 0)
                return null;

            string version = getValue((T)matches[0]);
            // Emitted assemblies can repeat a single-use attribute. Keep the value when the copies agree.
            // A conflict skips only this version source, so the module can still use its file or assembly version.
            for (int i = 1; i < matches.Length; i++) {
                if (!String.Equals(version, getValue((T)matches[i]), StringComparison.Ordinal))
                    return null;
            }

            return version;
        }

        public static string GetCompany(this Assembly assembly) {
            var attr = assembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
            return attr != null ? attr.Company : null;
        }
    }
}
