using System.Collections.Specialized;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// Properties of a request received by a <see cref="HttpServer"/>.
    /// </summary>
    public struct RequestInfo
    {
        /// <summary>
        /// The request method, always in uppercase.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// The URL path, not including query string.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The query string, if any (including the "?" prefix), or an empty string; never null.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// The request headers.
        /// </summary>
        public NameValueCollection Headers { get; set; }

        /// <summary>
        /// The request body, or an empty string if none.
        /// </summary>
        /// <remarks>
        /// <see cref="HttpServer"/> always reads the entire request body before calling
        /// a <see cref="Handler"/>; you can't read the request as a stream.
        /// </remarks>
        public string Body { get; set; }
    }
}
