using System.Collections.Generic;
using Exceptionless.Models;

namespace Exceptionless.Tests.Serializer {
    public class EventEqualityComparer : IEqualityComparer<Event> {
        public bool Equals(Event x, Event y) {
            if (ReferenceEquals(x, null) && ReferenceEquals(y, null)) {
                return true;
            }

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) {
                return false;
            }

            if (ReferenceEquals(x, y)) {
                return true;
            }

            return string.Equals(x.Type, y.Type) && string.Equals(x.Source, y.Source) && x.Tags.CollectionEquals(y.Tags) && string.Equals(x.Message, y.Message) && string.Equals(x.Geo, y.Geo) && x.Value == y.Value && x.Date == y.Date && x.Count == y.Count && x.ReferenceId == y.ReferenceId && x.Data.CollectionEquals(y.Data);
        }

        public int GetHashCode(Event obj) {
            unchecked {
                var hashCode = obj.Type == null ? 0 : obj.Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (obj.Source == null ? 0 : obj.Source.GetHashCode());
                hashCode = (hashCode * 397) ^ (obj.Tags == null ? 0 : obj.Tags.GetCollectionHashCode());
                hashCode = (hashCode * 397) ^ (obj.Message == null ? 0 : obj.Message.GetHashCode());
                hashCode = (hashCode * 397) ^ (obj.Geo == null ? 0 : obj.Geo.GetHashCode());
                hashCode = (hashCode * 397) ^ (obj.ReferenceId == null ? 0 : obj.ReferenceId.GetHashCode());
                hashCode = (hashCode * 397) ^ obj.Date.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Count.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Value.GetHashCode();
                hashCode = (hashCode * 397) ^ (obj.Data == null ? 0 : obj.Data.GetCollectionHashCode());
                return hashCode;
            }
        }
    }
}
