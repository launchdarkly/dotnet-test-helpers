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
    public sealed class HttpServer : IDisposable
    {
        /// <summary>
        /// The base URI of the server.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Returns the <see cref="RequestRecorder"/> that captures all requests to this server.
        /// </summary>
        public RequestRecorder Recorder => _recorder;

        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _canceller;
        private readonly RequestRecorder _recorder;

        private HttpServer(
            HttpListener listener,
            CancellationTokenSource canceller,
            RequestRecorder recorder,
            Uri uri
            )
        {
            _listener = listener;
            _canceller = canceller;
            _recorder = recorder;
            Uri = uri;
        }

        /// <summary>
        /// Shuts down the server.
        /// </summary>
        public void Dispose()
        {
            _canceller.Cancel();
            _listener.Stop();
            try
            {
                _listener.Close();
            }
            catch { } // .NET Core 2.0 has a bug that can cause a spurious exception here
        }

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
        /// <see cref="Handlers.Switchable(out HandlerSwitcher)"/>.</param>
        /// <returns>the started server instance</returns>
        public static HttpServer Start(Handler handler)
        {
            // HttpListener doesn't seem to have a per-request cancellation token, so we'll create
            // one for the entire server to ensure that handlers will exit if it's being stopped.
            var canceller = new CancellationTokenSource();

            var rootHandler = Handlers.Record(out var recorder).Then(handler);
            var listener = StartWebServerOnAvailablePort(canceller.Token, rootHandler, out var uri);

            EnsureServerIsListening(uri);
            
            return new HttpServer(listener, canceller, recorder, uri);
        }

        private static HttpListener StartWebServerOnAvailablePort(
            CancellationToken cancellationToken,
            Handler rootHandler,
            out Uri serverUriOut
            )
        {
            var port = FindNextPort();

            while (true)
            {
                var listener = new HttpListener();
                listener.Prefixes.Add(string.Format("http://*:{0}/", port));
                try
                {
                    listener.Start();
                }
                catch (HttpListenerException)
                {
                    // For unknown reasons, sometimes the logic used in FindNextPort will return a port that's
                    // not really available after all
                    port++;
                    continue;
                }
                Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested && listener.IsListening)
                    {
                        try
                        {
                            var listenerCtx = await listener.GetContextAsync().ConfigureAwait(false);
                            var ctx = RequestContextImpl.FromHttpListenerContext(listenerCtx, cancellationToken);
#pragma warning disable CS4014 // deliberately not awaiting this async task
                        Task.Run(async () =>
                            {
                                await Dispatch(ctx, rootHandler);
                                listenerCtx.Response.Close();
                            });
#pragma warning restore CS4014

                    }
                        catch
                        {
                        // an exception almost certainly means the listener has been shut down
                        break;
                        }
                    }
                });

                serverUriOut = new Uri(string.Format("http://localhost:{0}/", port));

                return listener;
            }
        }

        private static int FindNextPort()
        {
            // http://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();

                return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            }
            finally { tcpListener.Stop(); }
        }

        private static void EnsureServerIsListening(Uri uri)
        {
            // The server might take a moment to start asynchronously, so we'll check that it's
            // listening before we return.
            var deadline = DateTime.Now.AddSeconds(10);
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
