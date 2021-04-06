using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
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
        public async Task DefaultHandler() =>
            await WithServerAndClient(Handlers.Default, async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                AssertNoHeader(resp, "content-type");
                Assert.Equal("", await resp.Content.ReadAsStringAsync());
            });

        [Fact]
        public async Task Status() =>
            await WithServerAndClient(Handlers.Status(419), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(419, (int)resp.StatusCode);
                AssertNoHeader(resp, "content-type");
                Assert.Equal("", await resp.Content.ReadAsStringAsync());
            });

        [Fact]
        public async Task StatusAsEnum() =>
            await WithServerAndClient(Handlers.Status(HttpStatusCode.BadRequest), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal((int)HttpStatusCode.BadRequest, (int)resp.StatusCode);
                AssertNoHeader(resp, "content-type");
                Assert.Equal("", await resp.Content.ReadAsStringAsync());
            });

        [Fact]
        public async Task SetHeader()
        {
            var handler = Handlers.Default.Then(Handlers.Header("header-name", "old-value"))
                .Then(Handlers.Header("header-name", "new-value"));
            await WithServerAndClient(handler, async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                AssertHeader(resp, "header-name", "new-value");
            });
        }

        [Fact]
        public async Task AddHeader()
        {
            var handler = Handlers.Default.Then(Handlers.Header("header-name", "old-value"))
                .Then(Handlers.AddHeader("header-name", "new-value"));
            await WithServerAndClient(handler, async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                AssertHeader(resp, "header-name", "old-value", "new-value");
            });
        }

        [Fact]
        public async Task Body()
        {
            byte[] data = new byte[] { 1, 2, 3 };
            await WithServerAndClient(Handlers.Body("application/weird", data), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                AssertHeader(resp, "content-type", "application/weird");
                Assert.Equal(data, await resp.Content.ReadAsByteArrayAsync());
            });
        }

        [Fact]
        public async Task BodyString()
        {
            string body = "hello";
            await WithServerAndClient(Handlers.BodyString("text/weird", body), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                AssertHeader(resp, "content-type", "text/weird; charset=utf-8");
                Assert.Equal(body, await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task BodyJson()
        {
            await WithServerAndClient(Handlers.BodyJson("true"), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp.StatusCode);
                AssertHeader(resp, "content-type", "application/json; charset=utf-8");
                Assert.Equal("true", await resp.Content.ReadAsStringAsync());
            });
        }

        [Fact]
        public async Task ChainStatusAndHeadersAndBody()
        {
            var handler = Handlers.Status(201)
                .Then(Handlers.Header("name1", "value1"))
                .Then(Handlers.Header("name2", "value2"))
                .Then(Handlers.BodyString("text/plain", "hello"));
            await WithServerAndClient(handler, async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(201, (int)resp.StatusCode);
                AssertHeader(resp, "name1", "value1");
                AssertHeader(resp, "name2", "value2");
                AssertHeader(resp, "content-type", "text/plain; charset=utf-8");
                Assert.Equal("hello", await resp.Content.ReadAsStringAsync());
            });
        }
    }
}
