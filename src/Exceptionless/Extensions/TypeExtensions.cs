using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Exceptionless.Extensions {
    internal static class TypeExtensions {
#if PORTABLE || NETSTANDARD1_0 || NETSTANDARD1_1 || NETSTANDARD1_2
        [Flags]
        internal enum BindingFlags {
            Default = 0,
            IgnoreCase = 1,
            DeclaredOnly = 2,
            Instance = 4,
            Static = 8,
            Public = 16,
            NonPublic = 32,
            FlattenHierarchy = 64,
            InvokeMethod = 256,
            CreateInstance = 512,
            GetField = 1024,
            SetField = 2048,
            GetProperty = 4096,
            SetProperty = 8192,
            PutDispProperty = 16384,
            ExactBinding = 65536,
            PutRefDispProperty = 32768,
            SuppressChangeType = 131072,
            OptionalParamBinding = 262144,
            IgnoreReturn = 16777216
        }

        public static Type[] GetGenericArguments(this Type type) {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static IEnumerable<Type> GetInterfaces(this Type type) {
            return type.GetTypeInfo().ImplementedInterfaces;
        }

        public static PropertyInfo GetProperty(this Type type, string name) {
            return type.GetTypeInfo().GetDeclaredProperty(name);
        }

        public static IEnumerable<PropertyInfo> GetProperties(this Type type, BindingFlags bindingFlags) {
            IList<PropertyInfo> properties = (bindingFlags.HasFlag(BindingFlags.DeclaredOnly))
                ? type.GetTypeInfo().DeclaredProperties.ToList()
                : type.GetTypeInfo().GetPropertiesRecursive();

            return properties.Where(p => TestAccessibility(p, bindingFlags));
        }
        
        private static IList<PropertyInfo> GetPropertiesRecursive(this TypeInfo type) {
            TypeInfo t = type;
            IList<PropertyInfo> properties = new List<PropertyInfo>();
            while (t != null) {
                foreach (PropertyInfo member in t.DeclaredProperties) {
                    if (!properties.Any(p => p.Name == member.Name)) {
                        properties.Add(member);
                    }
                }
                t = (t.BaseType != null) ? t.BaseType.GetTypeInfo() : null;
            }

            return properties;
        }

        private static bool TestAccessibility(PropertyInfo member, BindingFlags bindingFlags) {
            if (member.GetMethod != null && TestAccessibility(member.GetMethod, bindingFlags)) {
                return true;
            }

            if (member.SetMethod != null && TestAccessibility(member.SetMethod, bindingFlags)) {
                return true;
            }

            return false;
        }

        private static bool TestAccessibility(MethodBase member, BindingFlags bindingFlags) {
            bool visibility = (member.IsPublic && bindingFlags.HasFlag(BindingFlags.Public)) ||
                              (!member.IsPublic && bindingFlags.HasFlag(BindingFlags.NonPublic));

            bool instance = (member.IsStatic && bindingFlags.HasFlag(BindingFlags.Static)) ||
                            (!member.IsStatic && bindingFlags.HasFlag(BindingFlags.Instance));

            return visibility && instance;
        }
#endif

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