using System;
using System.Net.Security;
using Exceptionless.Dependency;
using Exceptionless.Submission;
using Exceptionless.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Exceptionless.Tests.Configuration {
    public class CertificateValidationTest {
        private readonly TestOutputWriter _writer;
        public CertificateValidationTest(ITestOutputHelper output) {
            _writer = new TestOutputWriter(output);
        }

        [Fact(Skip = "depends on availability of a remote service")]
        public void CanOverrideCertValidation() {
            bool failed = true;
            var client = GetClient(
                                   x => {
                                       failed = false;
                                       Assert.NotNull(x.Certificate);
                                       Assert.NotNull(x.Chain);
                                       Assert.NotNull(x.Sender);
                                       Assert.Equal(SslPolicyErrors.None, x.SslPolicyErrors);
                                       return true;
                                   },
                                   "https://expired.badssl.com/");
            var submissionClient = client.Configuration.Resolver.Resolve<ISubmissionClient>();
            submissionClient.SendHeartbeat("null", false, client.Configuration);
            Assert.False(failed, "Validation Callback was not invoked");
        }

        private ExceptionlessClient GetClient(Func<CertificateData, bool> validator, string serverUrl) {
            return new ExceptionlessClient("LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw") {
                Configuration = {
                    ServerUrl = serverUrl,
                    ServerCertificateValidationCallback = validator
                }
            };
        }
    }
}
