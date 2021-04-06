using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// An asynchronous function that handles HTTP requests for a <see cref="HttpServer"/>.
    /// </summary>
    /// <remarks>
    /// Use the factory methods in <see cref="Handlers"/> to create standard implementations.
    /// </remarks>
    /// <param name="context">the request context</param>
    /// <returns>the asynchronous task</returns>
    public delegate Task Handler(IRequestContext context);

    /// <summary>
    /// Factory methods for standard <see cref="Handler"/> implementations.
    /// </summary>
    public static class Handlers
    {
        //#pragma warning disable CS1998 // allow async methods with no await

        /// <summary>
        /// A <c>Handler</c> that does nothing but set the status to 200. Useful as a start when
        /// chaining with <see cref="Then(Handler, Handler)"/>.
        /// </summary>
        public static Handler Default => Sync(ctx => { });

        /// <summary>
        /// Chains another handler to be executed immediately after this one.
        /// </summary>
        /// <remarks>
        /// Changing the status code or headers after a previous handler has already written a
        /// response body will not work.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var handler = Handlers.Delay(TimeSpan.FromSeconds(2)).Then(Handlers.Status(200));
        /// </code>
        /// </example>
        /// <param name="first">the first handler to execute</param>
        /// <param name="second">the next handler to execute</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Then(this Handler first, Handler second) =>
            async ctx =>
            {
                await first(ctx);
                if (!ctx.CancellationToken.IsCancellationRequested)
                {
                    await second(ctx);
                }
            };

        /// <summary>
        /// Creates a <see cref="Handler"/> that sets the HTTP response status.
        /// </summary>
        /// <param name="statusCode">the status code</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Status(int statusCode) =>
            Sync(ctx => ctx.SetStatus(statusCode));

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
        /// <param name="encoding">response encoding (defaults to UTF8)</param>
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
        /// Creates a <see cref="Handler"/> that starts writing a chunked response.
        /// </summary>
        /// <example>
        /// <code>
        ///     var handler = Handlers.StartChunks("text/my-stream-data")
        ///         .Then(Handlers.WriteChunkString("data1"))
        ///         .Then(Handlers.WriteChunkString("data2"));
        /// </code>
        /// </example>
        /// <param name="contentType">the content type</param>
        /// <param name="encoding">response encoding (defaults to UTF8)</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="WriteChunk(byte[])"/>
        /// <seealso cref="WriteChunkString(string, Encoding)"/>
        public static Handler StartChunks(string contentType, Encoding encoding = null) =>
            async ctx =>
            {
                ctx.SetHeader("Content-Type", ContentTypeWithEncoding(contentType, encoding));
                await ctx.WriteChunkedDataAsync(null);
            };

        /// <summary>
        /// Creates a <see cref="Handler"/> that writes a chunk of response data.
        /// </summary>
        /// <param name="data">the chunk data</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="StartChunks(string, Encoding)"/>
        /// <seealso cref="WriteChunkString(string, Encoding)"/>
        public static Handler WriteChunk(byte[] data) =>
            async ctx => await ctx.WriteChunkedDataAsync(data);

        /// <summary>
        /// Creates a <see cref="Handler"/> that writes a chunk of response data.
        /// </summary>
        /// <param name="data">the chunk data as a string</param>
        /// <param name="encoding">response encoding (defaults to UTF8)</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="StartChunks(string, Encoding)"/>
        /// <seealso cref="WriteChunk(byte[])"/>
        public static Handler WriteChunkString(string data, Encoding encoding = null) =>
            async ctx => await ctx.WriteChunkedDataAsync(
                data == null ? new byte[0] :
                    (encoding ?? Encoding.UTF8).GetBytes(data)
                );

        /// <summary>
        /// Creates a <see cref="Handler"/> that sleeps for the specified amount of time.
        /// </summary>
        /// <remarks>
        /// If the delay is <see cref="Timeout.InfiniteTimeSpan"/>, it will hold the connection open
        /// indefinitely until the server is closed. This may be useful in testing timeout logic.
        /// </remarks>
        /// <param name="delay">how long to delay</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Delay(TimeSpan delay) =>
            async ctx => await Task.Delay(delay, ctx.CancellationToken);

        /// <summary>
        /// Creates a <see cref="RequestRecorder"/> that captures requests.
        /// </summary>
        /// <remarks>
        /// You won't normally need to use this directly, since the <see cref="HttpServer"/> has a
        /// <see cref="RequestRecorder"/> built in, but you could use it to capture a subset of
        /// requests.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var handler = Handlers.Record(out var recorder).Then(Handlers.Status(200));
        /// </code>
        /// </example>
        /// <param name="recorder">receives the new <see cref="RequestRecorder"/> instance</param>
        /// <returns>a <see cref="Handler"/> that will pass requests to the recorder</returns>
        public static Handler Record(out RequestRecorder recorder)
        {
            recorder = new RequestRecorder();
            return recorder.Handler;
        }

        /// <summary>
        /// Creates a <see cref="SimpleRouter"/> for delegating to other handlers based on the
        /// request path.
        /// </summary>
        /// <example>
        /// <code>
        ///     var server = HttpServer.Start(Handlers.Router(out var router));
        ///     router.AddPath("/goodpath", Handlers.Status(200));
        ///     router.AddPath("/badpath", Handlers.Status(400));
        /// </code>
        /// </example>
        /// <param name="router">receives the new <see cref="SimpleRouter"/> instance</param>
        /// <returns></returns>
        public static Handler Router(out SimpleRouter router)
        {
            router = new SimpleRouter();
            return router.Handler;
        }

        /// <summary>
        /// Creates a <see cref="HandlerSwitcher"/> for changing handler behavior dynamically.
        /// It is initially set to delegate to <see cref="Handlers.Default"/>.
        /// </summary>
        /// <param name="switchable">receives the new <see cref="HandlerSwitcher"/> instance</param>
        /// <returns>a <see cref="HandlerSwitcher"/></returns>
        public static Handler Switchable(out HandlerSwitcher switchable)
        {
            switchable = new HandlerSwitcher(Handlers.Default);
            return switchable.Handler;
        }

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

        private static string ContentTypeWithEncoding(string contentType, Encoding encoding) =>
            contentType is null || contentType.Contains("charset=") ? contentType :
                contentType + "; charset=" + (encoding ?? Encoding.UTF8).WebName;
    }
}
