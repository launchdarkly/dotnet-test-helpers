using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class HttpServerTest
    {
        [Fact]
        public async Task ServerWithSimpleStatusHandler()
        {
            await WithServerAndClient(Handlers.Status(419), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(419, (int)resp.StatusCode);
            });
        }

        [Fact]
        public async Task MultipleServers()
        {
            using (var server1 = HttpServer.Start(Handlers.Status(200)))
            {
                using (var server2 = HttpServer.Start(Handlers.Status(419)))
                {
                    Assert.NotEqual(server1.Uri, server2.Uri);

                    using (var client = new HttpClient())
                    {
                        var resp1 = await client.GetAsync(server1.Uri);
                        Assert.Equal(200, (int)resp1.StatusCode);

                        var resp2 = await client.GetAsync(server2.Uri);
                        Assert.Equal(419, (int)resp2.StatusCode);
                    }
                }
            }
        }

        [Fact]
        public async Task ServerCanBeUsedAsFakeProxy()
        {
            var proxyResp = Handlers.Status(200).Then(Handlers.BodyString("text/plain", "hello"));
            using (var fakeProxy = HttpServer.Start(proxyResp))
            {
                var proxyParam = new WebProxy(fakeProxy.Uri);
                using (var client = new HttpClient(new HttpClientHandler { Proxy = proxyParam }, true))
                {
                    var fakeTargetUri = new Uri("http://example/not/real");
                    var resp = await client.GetAsync(fakeTargetUri);
                    Assert.Equal(200, (int)resp.StatusCode);
                    Assert.Equal("hello", await resp.Content.ReadAsStringAsync());

                    var request = fakeProxy.Recorder.RequireRequest();
                    Assert.Equal("GET", request.Method);
                    Assert.Equal(fakeTargetUri, request.Uri);
                    Assert.Equal("/not/real", request.Path);
                }
            }
        }

        [Fact]
        public async Task ServerOnSpecificPort()
        {
            int port = 18173;
            using (var server = HttpServer.Start(port, Handlers.Status(419)))
            {
                Assert.Equal(port, server.Uri.Port);

                using (var client = new HttpClient())
                {
                    var resp = await client.GetAsync(server.Uri);
                    Assert.Equal(419, (int)resp.StatusCode);
                }
            }
        }

        [Fact]
        public async Task UncaughtExceptionReturns500Error()
        {
            Handler handler = Handlers.Sync(ctx =>
                throw new Exception("deliberately broken"));
            await WithServerAndClient(handler, async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(500, (int)resp.StatusCode);
                var body = await resp.Content.ReadAsStringAsync();
                Assert.Contains("Internal error", body);
                Assert.Contains("deliberately broken", body);
            });
        }

        [Fact]
        public async Task ServerCanAcceptManyRequests()
        {
            await WithServerAndClient(Handlers.Status(200), async (server, client) =>
            {
                server.Recorder.Enabled = false;
                for (var i = 0; i < 2000; i++)
                {
                    var resp = await client.GetAsync(server.Uri);
                    Assert.Equal(200, (int)resp.StatusCode);
                }
            });
        }
    }
}
