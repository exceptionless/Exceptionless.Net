using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Exceptionless.Submission {
    public sealed class GzipContent : HttpContent {
        private readonly HttpContent _httpContent;

        public GzipContent(HttpContent httpContent) {
            if (httpContent == null)
                throw new ArgumentNullException(nameof(httpContent));
            
            _httpContent = httpContent;
            
            Headers.ContentEncoding.Add("gzip");
            foreach (var header in _httpContent.Headers)
                Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        protected override bool TryComputeLength(out long length) {
            length = -1;
            return false;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context) {
            using (Stream compressedStream = new GZipStream(stream, CompressionMode.Compress, true)) {
                await _httpContent.CopyToAsync(compressedStream).ConfigureAwait(false);
            }
        }
    }
}