using System.Collections.Generic;

namespace Exceptionless.Json.Serialization {
    public delegate IEnumerable<KeyValuePair<object, object>> ExtensionDataGetter(object o);

    public delegate object ObjectConstructor<T>(params object[] args);
}
