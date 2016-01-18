using System;
using Exceptionless.Web;

namespace Exceptionless.SampleWcf {
    [ExceptionlessWcfHandleError]
    public class Service1 : IService1 {
        public string GetData(int value) {
            throw new Exception(Guid.NewGuid().ToString());
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite) {
            if (composite == null)
                throw new ArgumentNullException(nameof(composite));

            if (composite.BoolValue)
                composite.StringValue += "Suffix";

            return composite;
        }
    }
}