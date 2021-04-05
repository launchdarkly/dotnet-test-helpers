using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class ChunkedResponseTest
    {
        [Fact]
        public async Task ChunkedResponse()
        {
            Handler handler = async ctx =>
            {
                ctx.SetHeader("Content-Type", "text/plain; charset=utf-8");
                await ctx.WriteChunkedDataAsync(Encoding.UTF8.GetBytes("chunk1,"));
                await ctx.WriteChunkedDataAsync(Encoding.UTF8.GetBytes("chunk2"));
                await Task.Delay(Timeout.Infinite, ctx.CancellationToken).ConfigureAwait(false);
            };
            var expected = "chunk1,chunk2";
            await WithServerAndClient(handler, async (server, client) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get, server.Uri);
                var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("text/plain; charset=utf-8", resp.Content.Headers.ContentType.ToString());
                var stream = await resp.Content.ReadAsStreamAsync();
                var received = new StringBuilder();
                while (true)
                {
                    var buf = new byte[100];
                    int n = await stream.ReadAsync(buf, 0, buf.Length);
                    Assert.True(n > 0, "should not have reached end of response");
                    received.Append(Encoding.UTF8.GetString(buf, 0, n));
                    if (received.Length >= expected.Length)
                    {
                        Assert.Equal(expected, received.ToString());
                        break;
                    }
                }
            });
        }
    }
}
