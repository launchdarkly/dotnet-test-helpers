using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class HandlersTest
    {
        [Fact]
        public async Task CustomAsyncHandler()
        {
            Handler handler = async ctx =>
            {
                await ctx.WriteFullResponseAsync("text/plain", Encoding.UTF8.GetBytes("hello"));
            };
            await WithServerAndClient(handler, async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal("hello", await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task CustomSyncHandler()
        {
            Action<IRequestContext> action = ctx =>
            {
                ctx.SetStatus(419);
            };
            await WithServerAndClient(Handlers.Sync(action), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(419, (int)resp.StatusCode);
            });
        }

        [Fact]
        public async Task ResponseHandlerWithoutBody()
        {
            var headers = new NameValueCollection();
            headers.Add("header-name", "header-value");
            await WithServerAndClient(Handlers.Response(419, headers), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(419, (int)resp.StatusCode);
                Assert.Equal("header-value", resp.Headers.GetValues("header-name").First());
                Assert.Equal("", await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task ResponseHandlerWithBody()
        {
            var headers = new NameValueCollection();
            headers.Add("header-name", "header-value");
            byte[] data = new byte[] { 1, 2, 3 };
            await WithServerAndClient(Handlers.Response(200, headers, "application/weird", data), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                // resp.Content.Headers.ContentType will be null if .NET doesn't recognize this content type
                Assert.Equal("application/weird", resp.Content.Headers.GetValues("content-type").First());
                Assert.Equal("header-value", resp.Headers.GetValues("header-name").First());
                Assert.Equal(data, await resp.Content.ReadAsByteArrayAsync());
            });
        }

        [Fact]
        public async Task StringResponseHandlerWithBody()
        {
            var headers = new NameValueCollection();
            headers.Add("header-name", "header-value");
            string body = "hello";
            await WithServerAndClient(Handlers.StringResponse(200, headers, "text/weird", body), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("text/weird; charset=utf-8", resp.Content.Headers.GetValues("content-type").First());
                Assert.Equal("header-value", resp.Headers.GetValues("header-name").First());
                Assert.Equal(body, await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task JsonResponseHandler()
        {
            await WithServerAndClient(Handlers.JsonResponse("true"), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("application/json; charset=utf-8", resp.Content.Headers.ContentType.ToString());
                Assert.Equal("true", await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task JsonResponseHandlerWithHeaders()
        {
            var headers = new NameValueCollection();
            headers.Add("header-name", "header-value");
            await WithServerAndClient(Handlers.JsonResponse("true", headers), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal("application/json; charset=utf-8", resp.Content.Headers.ContentType.ToString());
                Assert.Equal("header-value", resp.Headers.GetValues("header-name").First());
                Assert.Equal("true", await resp.Content.ReadAsStringAsync());
            });
        }
    }
}
