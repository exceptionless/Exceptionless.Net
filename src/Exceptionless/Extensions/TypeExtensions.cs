using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Exceptionless.Extensions {
    internal static class TypeExtensions {
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

            return Activator.CreateInstance(type);
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
    }
}