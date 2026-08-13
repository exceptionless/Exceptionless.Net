#if !NET8_0_OR_GREATER
using System;

namespace System.Diagnostics.CodeAnalysis {
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class UnconditionalSuppressMessageAttribute : Attribute {
        public UnconditionalSuppressMessageAttribute(string category, string checkId) {
            Category = category;
            CheckId = checkId;
        }

        public string Category { get; }
        public string CheckId { get; }
        public string Justification { get; set; }
        public string MessageId { get; set; }
        public string Scope { get; set; }
        public string Target { get; set; }
    }

    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.ReturnValue |
        AttributeTargets.GenericParameter |
        AttributeTargets.Parameter |
        AttributeTargets.Property |
        AttributeTargets.Method |
        AttributeTargets.Class |
        AttributeTargets.Interface |
        AttributeTargets.Struct,
        Inherited = false)]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute {
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) {
            MemberTypes = memberTypes;
        }

        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }

    [Flags]
    internal enum DynamicallyAccessedMemberTypes {
        None = 0,
        PublicParameterlessConstructor = 0x0001,
        PublicConstructors = 0x0003,
        PublicProperties = 0x0200,
        Interfaces = 0x2000
    }
}
#endif
