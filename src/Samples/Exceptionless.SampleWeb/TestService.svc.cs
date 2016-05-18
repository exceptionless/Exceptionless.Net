using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using Exceptionless.Web;

namespace Exceptionless.SampleWeb {
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "TestService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select TestService.svc or TestService.svc.cs at the Solution Explorer and start debugging.

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    [ServiceContract(Namespace = "TestName")]
    [ExceptionlessWcfHandleError]
    public class TestService {
        [WebInvoke]
        public void DoWork() {
            throw new Exception("Exception in DoWork");
        }
    }
}