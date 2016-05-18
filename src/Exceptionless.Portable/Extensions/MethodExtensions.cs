using System;
using System.Text;
using Exceptionless.Models.Data;

namespace Exceptionless {
    public static class MethodExtensions {
        public static string GetDeclaringTypeFullName(this Method method) {
            if (!String.IsNullOrEmpty(method.DeclaringNamespace) && !String.IsNullOrEmpty(method.DeclaringType))
                return String.Concat(method.DeclaringNamespace, ".", method.DeclaringType.Replace('+', '.'));

            if (!String.IsNullOrEmpty(method.DeclaringType))
                return method.DeclaringType.Replace('+', '.');

            return String.Empty;
        }

        public static string GetFullName(this Method method) {
            if (method == null || String.IsNullOrEmpty(method.Name))
                return "<null>";

            var sb = new StringBuilder(method.GetDeclaringTypeFullName());
            sb.AppendFormat(".{0}", method.Name);

            if (method.GenericArguments.Count <= 0)
                return sb.ToString();

            sb.Append("[");
            bool first = true;
            foreach (string arg in method.GenericArguments) {
                if (first)
                    first = false;
                else
                    sb.Append(",");

                sb.Append(arg);
            }

            sb.Append("]");

            return sb.ToString();
        }
    }
}