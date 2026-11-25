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
            var attr = assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
            return attr != null ? attr.Version : null;
        }

        public static string GetInformationalVersion(this Assembly assembly) {
            var attr = assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
            return attr != null ? attr.InformationalVersion : null;
        }

        public static string GetCompany(this Assembly assembly) {
            var attr = assembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
            return attr != null ? attr.Company : null;
        }
    }
}