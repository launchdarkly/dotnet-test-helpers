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
    }
}
