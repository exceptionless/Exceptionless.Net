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
                    log.Error(typeof(AssemblyHelper), ex, String.Format("An error occurred while getting types for assembly \"{0}\".", assembly));
                }
            }

            return types;
        }
    }
}