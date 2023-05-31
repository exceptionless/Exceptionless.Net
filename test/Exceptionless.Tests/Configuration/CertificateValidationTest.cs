using System;
using System.Net.Security;
using System.Threading.Tasks;
using Exceptionless.Dependency;
using Exceptionless.Submission;
using Xunit;

namespace Exceptionless.Tests.Configuration {
    public class CertificateValidationTest {
        [Fact(Skip = "Depends on availability of a remote service")]
        public Task CanOverrideCertValidation() {
            return RunAsync(GetClient(x => {
                Assert.NotNull(x.Certificate);
                Assert.NotNull(x.Chain);
#if NET45
                Assert.NotNull(x.Sender);
#endif
                Assert.Equal(SslPolicyErrors.RemoteCertificateChainErrors, x.SslPolicyErrors);
                return true;
            }, "https://expired.badssl.com/"));
        }

        [Fact(Skip = "Depends on availability of a remote service")]
        public async Task CanTrustByThumbprint() {
            await RunAsync(GetClient(x=>x.Certificate.Thumbprint == "3E8AB453B8CF62F0BD0240739AAB815A170B08F0", "https://revoked.badssl.com/"));
            
            var client = GetClient(null, "https://revoked.badssl.com/");
            client.Configuration.TrustCertificateThumbprint("3e8Ab453b8cf62f0bd0240739aab815a170b08f0");
            
            await RunAsync(client);
        }

        [Fact(Skip = "Depends on availability of a remote service")]
        public Task CanTrustByCAThumbprint() {
            var client = GetClient(null, "https://revoked.badssl.com/");
            client.Configuration.TrustCAThumbprint("a8985d3A65e5e5c4b2d7d66d40c6dd2fb19c5436");
            
            return RunAsync(client);
        }

        [Fact(Skip = "Depends on availability of a remote service")]
        public Task CanTrustAllCertificates() {
            var client = GetClient(null, "https://self-signed.badssl.com/");
#pragma warning disable CS0618 // 'member' is obsolete
            client.Configuration.SkipCertificateValidation();
#pragma warning restore CS0618 // 'member' is obsolete

            return RunAsync(client);
        }

        private async Task RunAsync(ExceptionlessClient client) {
            bool failed = true;
            var callback = client.Configuration.ServerCertificateValidationCallback;
            client.Configuration.ServerCertificateValidationCallback = x => {
                failed = false;
                return callback(x);
            };
            
            var submissionClient = client.Configuration.Resolver.Resolve<ISubmissionClient>();
            var response = await submissionClient.GetSettingsAsync(client.Configuration, 1, null);
            Assert.Contains(" 404 ", response.Message);
            Assert.False(failed, "Validation Callback was not invoked");
        }

        private ExceptionlessClient GetClient(Func<CertificateData, bool> validator, string serverUrl) {
            return new ExceptionlessClient("LhhP1C9gijpSKCslHHCvwdSIz298twx271nTest") {
                Configuration = {
                    ServerUrl = serverUrl,
                    ServerCertificateValidationCallback = validator
                }
            };
        }
    }
}
