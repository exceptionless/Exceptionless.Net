using System;

namespace Exceptionless.Configuration {
    /// <summary>
    /// An attribute to configure the client
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ExceptionlessAttribute : Attribute {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionlessAttribute" /> class.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        public ExceptionlessAttribute(string apiKey) {
            ApiKey = apiKey;
            Enabled = true;
        }

        /// <summary>
        /// Gets or sets the server URL.
        /// </summary>
        /// <value>The server URL.</value>
        public string ServerUrl { get; set; }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        /// <value>The API key.</value>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets if SSL should be used.
        /// </summary>
        /// ///
        /// <value><c>true</c> to enable SSL; otherwise, <c>false</c>.</value>
        public bool EnableSSL { get; set; }

        /// <summary>
        /// Gets or sets if reporting is enabled.
        /// </summary>
        /// ///
        /// <value><c>true</c> to enable reporting; otherwise, <c>false</c>.</value>
        public bool Enabled { get; set; }
    }
}