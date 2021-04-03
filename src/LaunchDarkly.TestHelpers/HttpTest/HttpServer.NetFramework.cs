#if NETFRAMEWORK

using System;
using System.Threading;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LaunchDarkly.TestHelpers.HttpTest
{
	// The .NET Framework 4.5 implementation of HttpServer uses WireMock.Net. This is not
	// used on other platforms because, while WireMock.Net does support .NET Standard, it
	// can have somewhat inconsistent cross-platform behavior due to the way it uses the
	// lower-level system framework (Owin), and it can also have problems with incompatible
	// dependencies in Xamarin.

	public sealed partial class HttpServer
	{
		private struct PlatformDependent
        {
            internal WireMockServer Server;
            internal CancellationTokenSource Canceller;
        }

        private void DisposeInternal()
        {
            _impl.Canceller.Cancel();
            _impl.Server.Stop();
        }

        private static PlatformDependent StartWebServerOnAvailablePort(out Uri serverUri, Handler handler)
        {
            var canceller = new CancellationTokenSource();

            var server = WireMockServer.Start();
            serverUri = new Uri(server.Urls[0]);

            server.Given(Request.Create())
                .RespondWith(Response.Create().WithCallback(async req =>
                {
                    var ctx = new WireMockRequestContext(req, canceller.Token);
                    await handler(ctx);
                    return ctx.ToResponse();
                }));

            return new PlatformDependent
            {
                Server = server,
                Canceller = canceller
            };
        }
    }
}

#endif
