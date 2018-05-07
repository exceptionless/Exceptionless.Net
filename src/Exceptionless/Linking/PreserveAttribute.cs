using System;

namespace Exceptionless.Linking {
    /// <summary>
    /// This class is used to tell the Xamarin linker to not remove the specified member, type or assembly.
    /// </summary>
    internal sealed class PreserveAttribute : Attribute {
        /// <summary>
        /// Ensures that all members of this type are preserved.
        /// </summary>
        public bool AllMembers;
        /// <summary>
        /// Flags the method as a method to preserve during linking if the container class is pulled in.
        /// </summary>
        public bool Conditional;
    }
}
