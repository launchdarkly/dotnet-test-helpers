using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// Factory methods for standard <see cref="Handler"/> implementations.
    /// </summary>
    public static partial class Handlers
    {
        /// <summary>
        /// A <c>Handler</c> that does nothing but set the status to 200. Useful as a start when
        /// chaining with <see cref="Then(Handler, Handler)"/>.
        /// </summary>
        public static Handler Default => Sync(ctx => { });

        /// <summary>
        /// Creates a <see cref="Handler"/> that sets the HTTP response status.
        /// </summary>
        /// <param name="statusCode">the status code</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Status(int statusCode) =>
            Sync(ctx => ctx.SetStatus(statusCode));

        /// <summary>
        /// Creates a <see cref="Handler"/> that sets the HTTP response status.
        /// </summary>
        /// <param name="statusCode">the status code</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Status(HttpStatusCode statusCode) => Status((int)statusCode);

        /// <summary>
        /// Creates a <see cref="Handler"/> that sets a response header.
        /// </summary>
        /// <example>
        /// <code>
        ///     var handler = Handlers.Default.Then(Handlers.Header("Etag", "123")).
        ///         Then(Handlers.JsonBody("{}"));
        /// </code>
        /// </example>
        /// <param name="name">the header name</param>
        /// <param name="value">the header value</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="AddHeader(string, string)"/>
        public static Handler Header(string name, string value) =>
            Sync(ctx => ctx.SetHeader(name, value));

        /// <summary>
        /// Creates a <see cref="Handler"/> that adds a response header, without overwriting
        /// any previous values.
        /// </summary>
        /// <param name="name">the header name</param>
        /// <param name="value">the header value</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="Header(string, string)"/>
        public static Handler AddHeader(string name, string value) =>
            Sync(ctx => ctx.AddHeader(name, value));

        /// <summary>
        /// Creates a <see cref="Handler"/> that sends the specified response body.
        /// </summary>
        /// <param name="contentType">response content type</param>
        /// <param name="body">response body (null is equivalent to an empty array)</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="BodyString(string, string, Encoding)"/>
        /// <seealso cref="BodyJson(string, Encoding)"/>
        public static Handler Body(string contentType, byte[] body) =>
            async ctx => await ctx.WriteFullResponseAsync(contentType, body ?? new byte[0]);

        /// <summary>
        /// Creates a <see cref="Handler"/> that sends the specified response body.
        /// </summary>
        /// <param name="contentType">response content type (used only if body is not null)</param>
        /// <param name="body">response body (may be null)</param>
        /// <param name="encoding">character encoding; if not specified, no charset will be included
        /// in the Content-Type header, but UTF8 will be used to encode the string</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="Body(string, byte[])"/>
        /// <seealso cref="BodyJson(string, Encoding)"/>
        public static Handler BodyString(string contentType, string body, Encoding encoding = null) =>
            Body(
                ContentTypeWithEncoding(contentType, encoding),
                body == null ? null : (encoding ?? Encoding.UTF8).GetBytes(body)
                );

        /// <summary>
        /// Creates a <see cref="Handler"/> that sends a response body with JSON content type.
        /// </summary>
        /// <param name="jsonBody">the JSON data</param>
        /// <param name="encoding">response encoding (defaults to UTF8)</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="Body(string, byte[])"/>
        /// <seealso cref="BodyString(string, string, Encoding)"/>
        public static Handler BodyJson(string jsonBody, Encoding encoding = null) =>
            BodyString("application/json", jsonBody, encoding);

        /// <summary>
        /// Wraps a synchronous action in an asynchronous <see cref="Handler"/>.
        /// </summary>
        /// <param name="action">the action to run</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Sync(Action<IRequestContext> action) =>
#pragma warning disable CS1998 // async method without await
        async ctx =>
        {
            action(ctx);
        };
#pragma warning restore CS1998

        /// <summary>
        /// Converts a <see cref="Handler"/> to a standard .NET <see cref="HttpMessageHandler"/>.
        /// </summary>
        /// <remarks>
        /// This allows the various handlers in this library to be used as a convenient,
        /// composable way to simulate responses from an <c>HttpClient</c> using a custom
        /// <c>HttpMessageHandler</c> that does not make any network requests. This can be
        /// useful in testing code that makes requests to a real external URI, redirecting it
        /// to your internal fixture. It also allows simulating network errors; see
        /// <see cref="Error(Exception)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        ///     // Here we will intercept any request from the configured HttpClient so it
        ///     // receives a 503 response, while also recording requests.
        ///     var messageHandler = Handlers.Record(out var recorder)
        ///         .Then(Handlers.Status(503)).AsMessageHandler();
        ///     var httpClient = new HttpClient(messageHandler);
        ///
        ///     // ...Do something that will cause a request to be made...
        ///
        ///     var request = recorder.RequireRequest();
        ///     // Verify the properties of the request that was made
        /// </code>
        /// </example>
        /// <param name="handler">the handler to execute</param>
        /// <returns>an <c>HttpMessageHandler</c></returns>
        public static HttpMessageHandler AsMessageHandler(this Handler handler) =>
            new MessageHandlerFromHandler(handler);

        private static string ContentTypeWithEncoding(string contentType, Encoding encoding) =>
            contentType is null || encoding is null || contentType.Contains("charset=") ? contentType :
                contentType + "; charset=" + encoding.WebName;
    }
}
