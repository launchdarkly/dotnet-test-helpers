using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// An abstraction used by <see cref="Handler"/> implementations to hide the details of
    /// the underlying HTTP server framework.
    /// </summary>
    public interface IRequestContext
    {
        /// <summary>
        /// The properties of the request.
        /// </summary>
        RequestInfo RequestInfo { get; }

        /// <summary>
        /// A <see cref="CancellationToken"/> that will be cancelled if the client closes the request
        /// or if the server is stopped.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Sets the response status.
        /// </summary>
        /// <remarks>
        /// This should be done before sending body content; otherwise the result is undefined.
        /// </remarks>
        /// <param name="statusCode">the HTTP status</param>
        void SetStatus(int statusCode);

        /// <summary>
        /// Sets a response header.
        /// </summary>
        /// <param name="name">the case-insensitive header name</param>
        /// <param name="value">the header value</param>
        void SetHeader(string name, string value);

        /// <summary>
        /// Adds a response header, allowing multiple values.
        /// </summary>
        /// <param name="name">the case-insensitive header name</param>
        /// <param name="value">the header value</param>
        void AddHeader(string name, string value);

        /// <summary>
        /// Writes a chunk of data in a chunked response.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Currently, this is only available in .NET Standard-compatible platforms. The
        /// <c>WireMock.Net</c> implementation that is used in .NET Framework 4.5.x does not
        /// support streaming responses.
        /// </para>
        /// <para>
        /// If non-chunked data has already been written with <see cref="WriteFullResponseAsync"/>,
        /// the result is undefined.
        /// </para>
        /// </remarks>
        /// <param name="data">the data to write; if null or zero-length, it will only turn on
        /// chunked mode and not write any data</param>
        /// <returns>an asynchronous task</returns>
        /// <exception cref="System.NotSupportedException">if called in .NET Framework 4.5.x</exception>
        Task WriteChunkedDataAsync(byte[] data);

        /// <summary>
        /// Writes a complete response body.
        /// </summary>
        /// <remarks>
        /// This can only be called once per response. If chunked data has already been written with
        /// <see cref="WriteChunkedDataAsync(byte[])"/>, the result is undefined.
        /// </remarks>
        /// <param name="contentType">the Content-Type header value</param>
        /// <param name="data">the data</param>
        /// <returns>an asynchronous task</returns>
        Task WriteFullResponseAsync(string contentType, byte[] data);

        /// <summary>
        /// Returns a path parameter, if any path parameters were captured.
        /// </summary>
        /// <remarks>
        /// By default, this will always return null. It is non-null only if you used
        /// <see cref="SimpleRouter"/> and matched a regex pattern that was added with
        /// <see cref="SimpleRouter.AddRegex(System.Net.Http.HttpMethod, string, Handler)"/>,
        /// and the pattern contained capture groups. For instance, if the pattern was
        /// <code>/a/([^/]*)/c/(.*)</code> and the request path was <code>/a/b/c/d/e</code>,
        /// <code>GetPathParam(0)</code> would return <code>"b"</code> and
        /// <code>GetPathParam(1)</code> would return <code>"d/e"</code>.
        /// </remarks>
        /// <param name="index">a zero-based index</param>
        /// <returns>the path parameter string; null if there were no path parameters, or if
        /// the index is out of range</returns>
        string GetPathParam(int index);

        /// <summary>
        /// Returns a copy of this context with path parameter information added.
        /// </summary>
        /// <param name="pathParams">an array of positional parameters</param>
        /// <returns>a transformed context</returns>
        IRequestContext WithPathParams(string[] pathParams);
    }
}
