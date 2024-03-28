using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Exceptionless.Logging;

namespace Exceptionless.Utility {
    public class AssemblyHelper {
        public static Assembly GetRootAssembly() {
            return Assembly.GetEntryAssembly();
        }

        public static Assembly GetEntryAssembly(IExceptionlessLog log) {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (IsUserAssembly(entryAssembly))
                return entryAssembly;

            try {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a =>
                    !a.IsDynamic
                    && a != typeof(ExceptionlessClient).GetTypeInfo().Assembly
                    && a != typeof(object).GetTypeInfo().Assembly);

                return assemblies.FirstOrDefault(a => IsUserAssembly(a));
            }
            catch (Exception ex) {
                log.FormattedError(typeof(AssemblyHelper), ex, "Unable to get entry assembly. Error: {0}", ex.Message);
            }

            return null;
        }

        private static bool IsUserAssembly(Assembly assembly) {
            if (assembly == null)
                return false;

            if (!String.IsNullOrEmpty(assembly.FullName) && (assembly.FullName.StartsWith("System.") || assembly.FullName.StartsWith("Microsoft.")))
                return false;

            string company = assembly.GetCompany() ?? String.Empty;
            string[] nonUserCompanies = new[] { "Exceptionless", "Microsoft" };
            if (nonUserCompanies.Any(c => company.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0))
                return false;

            if (assembly.FullName == typeof(ExceptionlessClient).GetTypeInfo().Assembly.FullName)
                return false;

            return true;
        }

        public static string GetVersionFromAssembly(Assembly assembly) {
            if (assembly == null)
                return null;

            string version = assembly.GetInformationalVersion();
            if (String.IsNullOrEmpty(version) || String.Equals(version, "0.0.0.0"))
                version = assembly.GetFileVersion();

            if (String.IsNullOrEmpty(version) || String.Equals(version, "0.0.0.0"))
                version = assembly.GetVersion();

            if (String.IsNullOrEmpty(version) || String.Equals(version, "0.0.0.0")) {
                var assemblyName = assembly.GetAssemblyName();
                version = assemblyName != null ? assemblyName.Version.ToString() : null;
            }

            return !String.IsNullOrEmpty(version) && !String.Equals(version, "0.0.0.0") ? version : null;
        }

        public static string GetAssemblyTitle() {
            // Get all attributes on this assembly
            var assembly = GetRootAssembly();
            if (assembly == null)
                return String.Empty;

            var attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute)).ToArray();
            // If there aren't any attributes, return an empty string
            if (attributes.Length == 0)
                return String.Empty;

            // If there is an attribute, return its value
            return ((AssemblyTitleAttribute)attributes[0]).Title;
        }

        public static List<Type> GetTypes(IExceptionlessLog log) {
            var types = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                try {
                    if (assembly.IsDynamic)
                        continue;

                    types.AddRange(assembly.GetExportedTypes());
                } catch (Exception ex) {
                    log.Error(typeof(AssemblyHelper), ex, $"An error occurred while getting types for assembly \"{assembly}\".");
                }
            }

            return types;
        }
    }
}