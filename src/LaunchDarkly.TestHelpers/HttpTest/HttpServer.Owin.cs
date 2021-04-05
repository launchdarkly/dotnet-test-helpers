#if USE_OWIN

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;

namespace LaunchDarkly.TestHelpers.HttpTest
{
	// The .NET Framework 4.5 implementation of HttpServer uses Microsoft.AspNet.WebApi.OwinSelfHost,
    // an ASP.NET framework that is Windows-specific.

	public sealed partial class HttpServer
	{
		private struct PlatformDependent
        {
            internal IDisposable Server;
            internal CancellationTokenSource Canceller;
        }

        private void DisposeInternal()
        {
            _impl.Canceller.Cancel();
            _impl.Server.Dispose();
        }

        private static PlatformDependent StartWebServerOnAvailablePort(out Uri serverUri, Handler handler)
        {
            var port = FindNextPort();

            // Owin doesn't seem to have a per-request cancellation token, so we'll create one
            // for the entire server to ensure that handlers will exit if it's being stopped.
            var canceller = new CancellationTokenSource();

            var server = WebApp.Start(
                new StartOptions { Port = port },
                app =>
                {
                    app.Use(typeof(TestServerMiddleware), handler, canceller.Token);
                }
                );
            
            serverUri = new Uri(string.Format("http://localhost:{0}", port));

            return new PlatformDependent
            {
                Server = server,
                Canceller = canceller
            };
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

        private sealed class TestServerMiddleware : OwinMiddleware
        {
            private readonly Handler _handler;
            private readonly CancellationToken _cancellationToken;

            public TestServerMiddleware(OwinMiddleware next, Handler handler, CancellationToken cancellationToken) :
                base(next)
            {
                _handler = handler;
                _cancellationToken = cancellationToken;
            }

            public override async Task Invoke(IOwinContext owinCtx)
            {
                var context = OwinRequestContext.FromOwinContext(owinCtx, _cancellationToken);
                await _handler(context);
            }
        }
    }
}

#endif
