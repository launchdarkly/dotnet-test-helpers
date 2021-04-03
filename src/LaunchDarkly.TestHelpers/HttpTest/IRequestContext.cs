using System;
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
        /// <param name="data">the data to write</param>
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
    }
}
