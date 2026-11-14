using System;

namespace Exceptionless.Json {
    /// <summary>
    /// Instructs the Exceptionless serializer not to serialize a public field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ExceptionlessIgnoreAttribute : Attribute { }
}
