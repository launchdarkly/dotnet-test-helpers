using System;
using System.Collections.Specialized;
using System.Text;
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
    public class Handlers
    {
        /// <summary>
        /// Creates a <see cref="Handler"/> that sleeps for the specified amount of time
        /// before passing the request to the target handler.
        /// </summary>
        /// <param name="delay">how long to delay</param>
        /// <param name="target">the handler to call after the delay</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler DelayBefore(TimeSpan delay, Handler target) =>
            async ctx =>
            {
                await Task.Delay(delay, ctx.CancellationToken);
                if (!ctx.CancellationToken.IsCancellationRequested)
                {
                    await target(ctx);
                }
            };

        /// <summary>
        /// Creates a <see cref="Handler"/> that sends a 200 response with a JSON content type.
        /// </summary>
        /// <param name="jsonBody">the JSON data</param>
        /// <param name="headers">additional headers (may be null)</param>
        /// <returns></returns>
        public static Handler JsonResponse(
            string jsonBody,
            NameValueCollection headers = null
            ) =>
            StringResponse(200, headers, "application/json", jsonBody);

        /// <summary>
        /// Creates a <see cref="RequestRecorder"/> that captures requests while delegating to
        /// another handler
        /// </summary>
        /// <param name="target">the handler to delegate to</param>
        /// <returns>a <see cref="RequestRecorder"/></returns>
        public static RequestRecorder RecordAndDelegateTo(Handler target) =>
            new RequestRecorder(target);

        /// <summary>
        /// Creates a <see cref="Handler"/> that always sends the same response,
        /// specifying the response body (if any) as a byte array.
        /// </summary>
        /// <param name="statusCode">the HTTP status code</param>
        /// <param name="headers">response headers (may be null)</param>
        /// <param name="contentType">response content type (used only if body is not null)</param>
        /// <param name="body">response body (may be null)</param>
        /// <returns></returns>
        public static Handler Response(
            int statusCode,
            NameValueCollection headers,
            string contentType = null,
            byte[] body = null
            ) =>
            async ctx =>
            {
                ctx.SetStatus(statusCode);
                if (headers != null)
                {
                    foreach (var k in headers.AllKeys)
                    {
                        foreach (var v in headers.GetValues(k))
                        {
                            ctx.AddHeader(k, v);
                        }
                    }
                }
                if (body != null)
                {
                    await ctx.WriteFullResponseAsync(contentType, body);
                }
            };

        /// <summary>
        /// Creates a <see cref="Handler"/> that always sends the same response,
        /// specifying the response body (if any) as a string.
        /// </summary>
        /// <param name="statusCode">the HTTP status code</param>
        /// <param name="headers">response headers (may be null)</param>
        /// <param name="contentType">response content type (used only if body is not null)</param>
        /// <param name="body">response body (may be null)</param>
        /// <param name="encoding">response encoding (defaults to UTF8)</param>
        /// <returns></returns>
        public static Handler StringResponse(
            int statusCode,
            NameValueCollection headers,
            string contentType = null,
            string body = null,
            Encoding encoding = null
            ) =>
            Response(
                statusCode,
                headers,
                contentType is null || contentType.Contains("charset=") ? contentType :
                    contentType + "; charset=" + (encoding ?? Encoding.UTF8).WebName,
                body == null ? null : (encoding ?? Encoding.UTF8).GetBytes(body)
                );

        /// <summary>
        /// Creates a <see cref="Handler"/> that always returns the specified HTTP
        /// response status, with no custom headers and no body.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Status(int statusCode) =>
            Sync(ctx => ctx.SetStatus(statusCode));

        /// <summary>
        /// Creates a <see cref="HandlerSwitcher"/> for changing handler behavior
        /// dynamically.
        /// </summary>
        /// <param name="target">the initial <see cref="Handler"/> to delegate to</param>
        /// <returns>a <see cref="HandlerSwitcher"/></returns>
        public static HandlerSwitcher Switchable(Handler target) =>
            new HandlerSwitcher(target);

        /// <summary>
        /// Wraps a synchronous action in an asynchronous <see cref="Handler"/>.
        /// </summary>
        /// <param name="action">the action to run</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Sync(Action<IRequestContext> action) =>
#pragma warning disable CS1998
            async ctx =>
            {
                action(ctx);
            };
#pragma warning restore CS1998
    }
}
