#if NETSTANDARD

using System;
using System.Net;
using System.Threading;
using EmbedIO;
using EmbedIO.Routing;
using Swan.Logging;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    // The .NET Standard implementation of HttpServer uses EmbedIO. This is a portable
    // implementation that should work on all .NET platforms except for .NET Framework 4.5.x,
    // which does not support .NET Standard.

    public sealed partial class HttpServer
    {
        private static int _nextPort = 10000;
        
        private struct PlatformDependent
        {
            internal WebServer Server;
        }

        private void DisposeInternal() => _impl.Server.Dispose();

        private static PlatformDependent StartWebServerOnAvailablePort(out Uri serverUri, Handler handler)
        {
            // EmbedIO uses Swan.Logging, which unfortunately has global configuration so we can't
            // simply turn off logging for just our server instance.
            Logger.NoLogging();

            while (true)
            {
                var port = Interlocked.Increment(ref _nextPort);

                // EmbedIO has two internal implementations: HttpListenerMode.EmbedIO, which uses Mono
                // APIs, and HttpListenerMode.Microsoft, which uses the System.Net.HttpListener API on
                // platforms that support it. There is a known issue that prevents chunked responses
                // from working in the former mode (https://github.com/unosquare/embedio/issues/510),
                // so we'll use the Microsoft mode.
                var options = new WebServerOptions()
                    .WithUrlPrefix($"http://*:{port}")
                    .WithMode(HttpListenerMode.Microsoft);
                var server = new WebServer(options);
                server.Listener.IgnoreWriteExceptions = true;
                
                server.OnAny("/", async internalContext =>
                {
                    var ctx = await EmbedIORequestContext.FromHttpContext(internalContext);
                    await handler(ctx).ConfigureAwait(false);
                });

                try
                {
                    _ = server.RunAsync();
                }
                catch (HttpListenerException)
                {
                    continue; // port is in use
                }

                serverUri = new Uri(string.Format("http://localhost:{0}", port));

                return new PlatformDependent { Server = server };
            }
        }
    }
}

#endif
