using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Exceptionless.Extensions {
    internal static class TypeExtensions {
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Exception property capture is best-effort; trimmed properties are safely omitted and failures are caught by the caller.")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Exception property capture is best-effort; trimmed interface properties are safely omitted and failures are caught by the caller.")]
        public static PropertyInfo[] GetPublicProperties(this Type type) {
            if (type.GetTypeInfo().IsInterface) {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0) {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces()) {
                        if (considered.Contains(subInterface))
                            continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties.Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance).ToArray();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "The type is a value type from an already-instantiated generic IDictionary. Its layout is present, and GetUninitializedObject does not invoke a constructor.")]
        public static object GetDefaultValue(this Type type) {
            if (type == null || type.IsNullable())
                return null;

            if (type == typeof(string))
                return default(string);
            if (type == typeof(bool))
                return default(bool);
            if (type == typeof(byte))
                return default(byte);
            if (type == typeof(char))
                return default(char);
            if (type == typeof(decimal))
                return default(decimal);
            if (type == typeof(double))
                return default(double);
            if (type == typeof(float))
                return default(float);
            if (type == typeof(int))
                return default(int);
            if (type == typeof(long))
                return default(long);
            if (type == typeof(sbyte))
                return default(sbyte);
            if (type == typeof(uint))
                return default(uint);
            if (type == typeof(ulong))
                return default(ulong);
            if (type == typeof(ushort))
                return default(ushort);

            var ti = type.GetTypeInfo();
            if (ti.IsClass || ti.IsInterface)
                return null;

#if NET8_0_OR_GREATER
            return RuntimeHelpers.GetUninitializedObject(type);
#else
            return Activator.CreateInstance(type);
#endif
        }

        public static bool IsNullable(this Type type) {
            var ti = type.GetTypeInfo();
            if (ti.IsValueType)
                return false;

            return ti.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static bool IsPrimitiveType(this Type type) {
            if (type.GetTypeInfo().IsPrimitive)
                return true;

            if (type == typeof(Decimal)
                || type == typeof(String)
                || type == typeof(Guid)
                || type == typeof(TimeSpan)
                || type == typeof(Uri))
                return true;

            if (type.GetTypeInfo().IsEnum)
                return true;

            if (type.IsNullable())
                return IsPrimitiveType(Nullable.GetUnderlyingType(type));

            return false;
        }

        public static bool IsNumeric(this Type type) {
            if (type.IsArray)
                return false;

            return type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(byte)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal);
        }

        /// <summary>
        /// Gets the types pretty print full name.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTypeDisplayName(this Type type) => System.Diagnostics.TypeNameHelper.GetTypeDisplayName(type, true, true);
    }
}
