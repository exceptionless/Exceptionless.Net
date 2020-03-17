using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Exceptionless {
    public class CertificateData {
#if NET45
        public CertificateData(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            :this(chain, sslPolicyErrors) {
            Sender = sender;
            Certificate = new X509Certificate2(certificate.Handle);
        }
#else
        public CertificateData(HttpRequestMessage request, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            :this(chain, sslPolicyErrors) {
            Request = request;
            Certificate = certificate;
        }
#endif
        private CertificateData(X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            Chain = chain;
            SslPolicyErrors = sslPolicyErrors;
        }

        /// <summary>
        /// The certificate used to authenticate the remote party.
        /// </summary>
        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// The chain of certificate authorities associated with the remote certificate.
        /// </summary>
        public X509Chain Chain { get; }

        /// <summary>
        /// One or more errors associated with the remote certificate.
        /// </summary>
        public SslPolicyErrors SslPolicyErrors { get; }

#if NET45
        /// <summary>
        /// An object that contains state information for this validation.
        /// </summary>
        public object Sender { get; }
#endif

#if !NET45
        /// <summary>
        /// The request which was sent to the remore party
        /// </summary>
        public HttpRequestMessage Request { get; }
#endif
    }
}