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
            return assembly.GetSingleVersionAttribute<AssemblyFileVersionAttribute>()?.Version;
        }

        public static string GetInformationalVersion(this Assembly assembly) {
            return assembly.GetSingleVersionAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }

        private static T GetSingleVersionAttribute<T>(this Assembly assembly) where T : Attribute {
            // Version attributes are single-use. If an emitted assembly repeats one, fall back to the next version source.
            var matches = assembly.GetCustomAttributes(typeof(T), inherit: false);
            if (matches.Length is not 1)
                return null;

            return matches[0] as T;
        }

        public static string GetCompany(this Assembly assembly) {
            var attr = assembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
            return attr != null ? attr.Company : null;
        }
    }
}
