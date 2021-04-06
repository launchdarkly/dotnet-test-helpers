using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// A simplified system for setting up embedded test HTTP servers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstraction is designed to allow writing test code that does not need to know anything
    /// about the underlying implementation details of the HTTP framework, so that if a different
    /// library needs to be used for compatibility reasons, it can be substituted without disrupting
    /// the tests.
    /// </para>
    /// <example>
    /// <code>
    ///     // Start a server that returns a 200 status for all requests
    ///     using (var server = HttpServer.Start(Handlers.Status(200)))
    ///     {
    ///         DoSomethingThatMakesARequestTo(server.Uri);
    ///
    ///         var req = server.Recorder.RequireRequest();
    ///         // Check for expected properties of the request
    ///     }
    /// </code>
    /// </example>
    /// </remarks>
    public sealed partial class HttpServer : IDisposable
    {
        /// <summary>
        /// The base URI of the server.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Returns the <see cref="RequestRecorder"/> that captures all requests to this server.
        /// </summary>
        public RequestRecorder Recorder => _recorder;

        private readonly PlatformDependent _impl;
        private readonly RequestRecorder _recorder;

        private HttpServer(PlatformDependent impl, RequestRecorder recorder, Uri uri)
        {
            _impl = impl;
            _recorder = recorder;
            Uri = uri;
        }

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        public void Dispose() => DisposeInternal();

        /// <summary>
        /// Starts a new test server.
        /// </summary>
        /// <remarks>
        /// Make sure to close this when done, by calling <c>Dispose</c> or with a <c>using</c>
        /// statement.
        /// </remarks>
        /// <param name="handler">A function that will handle all requests to this server. Use
        /// the factory methods in <see cref="Handlers"/> for standard handlers. If you will need
        /// to change the behavior of the handler during the lifetime of the server, use
        /// <see cref="Handlers.Changeable(Handler)"/>.</param>
        /// <returns></returns>
        public static HttpServer Start(Handler handler)
        {
            var rootHandler = Handlers.Record(out var recorder).Then(handler);
            var impl = StartWebServerOnAvailablePort(out var uri, rootHandler);

            EnsureServerIsListening(uri);
            
            return new HttpServer(impl, recorder, uri);
        }

        private static void EnsureServerIsListening(Uri uri)
        {
            // The server might take a moment to start asynchronously, so we'll check that it's
            // listening before we return.
            var deadline = DateTime.Now.AddSeconds(1);
            while (DateTime.Now < deadline)
            {
                using (var tcpClient = new TcpClient())
                {
                    try
                    {
                        tcpClient.Connect(IPAddress.Loopback, uri.Port);
                        return;
                    }
                    catch { }
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }
            throw new InvalidOperationException("Timed out waiting for test server to start listening");
        }

        private static async Task Dispatch(IRequestContext ctx, Handler handler)
        {
            try
            {
                await handler(ctx).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ctx.SetStatus(500);
                await Handlers.BodyString("text/plain", "Internal error from test server: " + e.ToString())(ctx);
            };
        }
    }
}
