﻿using System;
using System.Collections.Generic;

namespace Exceptionless.Models.Data {
    [Json.JsonObject(NamingStrategyType = typeof(Json.Serialization.SnakeCaseNamingStrategy))]
    public class Method : IData {
        public Method() {
            Data = new DataDictionary();
            GenericArguments = new GenericArguments();
            Parameters = new ParameterCollection();
        }

        public bool IsSignatureTarget { get; set; }
        public string DeclaringNamespace { get; set; }
        public string DeclaringType { get; set; }

        public string Name { get; set; }

        public int ModuleId { get; set; }
        public DataDictionary Data { get; set; }
        public GenericArguments GenericArguments { get; set; }
        public ParameterCollection Parameters { get; set; }

        protected bool Equals(Method other) {
            return IsSignatureTarget == other.IsSignatureTarget && string.Equals(DeclaringNamespace, other.DeclaringNamespace) && string.Equals(DeclaringType, other.DeclaringType) && string.Equals(Name, other.Name) && Equals(Data, other.Data) && GenericArguments.CollectionEquals(other.GenericArguments) && Parameters.CollectionEquals(other.Parameters);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((Method)obj);
        }

        private static readonly ISet<string> _exclusions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "ILOffset", "NativeOffset" };

        public override int GetHashCode() {
            unchecked {
                var hashCode = IsSignatureTarget.GetHashCode();
                hashCode = (hashCode * 397) ^ (DeclaringNamespace == null ? 0 : DeclaringNamespace.GetHashCode());
                hashCode = (hashCode * 397) ^ (DeclaringType == null ? 0 : DeclaringType.GetHashCode());
                hashCode = (hashCode * 397) ^ (Name == null ? 0 : Name.GetHashCode());
                hashCode = (hashCode * 397) ^ (Data == null ? 0 : Data.GetCollectionHashCode(_exclusions));
                hashCode = (hashCode * 397) ^ (GenericArguments == null ? 0 : GenericArguments.GetCollectionHashCode());
                hashCode = (hashCode * 397) ^ (Parameters == null ? 0 : Parameters.GetCollectionHashCode());
                return hashCode;
            }
        }
    }
}