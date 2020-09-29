using System;

namespace Exceptionless.Models.Data {
    public class SimpleError : SimpleInnerError {
        public SimpleError() {
            Modules = new ModuleCollection();
        }

        /// <summary>
        /// Any modules that were loaded / referenced when the error occurred.
        /// </summary>
        public ModuleCollection Modules { get; set; }

        protected bool Equals(SimpleError other) {
            return base.Equals(other) && Modules.CollectionEquals(other.Modules);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((SimpleError)obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (base.GetHashCode() * 397) ^ (Modules != null ? Modules.GetCollectionHashCode() : 0);
            }
        }

        public static class KnownDataKeys {
            public const string ExtraProperties = "@ext";
        }
    }
}