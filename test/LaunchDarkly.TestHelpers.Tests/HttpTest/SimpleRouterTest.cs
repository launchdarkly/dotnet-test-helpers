using System;
using System.Net.Http;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class SimpleRouterTest
    {
        [Fact]
        public async void NoPathsMatchByDefault() =>
            await WithServerAndClient(Handlers.Router(out _), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(404, (int)resp.StatusCode);
            });

        [Fact]
        public async void SimplePathMatch() =>
            await WithServerAndClient(Handlers.Router(out var router), async (server, client) =>
            {
                router.AddPath("/path1", Handlers.Status(201));
                router.AddPath("/path2", Handlers.Status(419));

                var resp1 = await client.GetAsync(new Uri(server.Uri, "/path1"));
                Assert.Equal(201, (int)resp1.StatusCode);

                var resp2 = await client.GetAsync(new Uri(server.Uri, "/path2"));
                Assert.Equal(419, (int)resp2.StatusCode);

                var resp3 = await client.GetAsync(new Uri(server.Uri, "/path3"));
                Assert.Equal(404, (int)resp3.StatusCode);
            });

        [Fact]
        public async void SimplePathMatchWithMethod() =>
            await WithServerAndClient(Handlers.Router(out var router), async (server, client) =>
            {
                router.AddPath(HttpMethod.Get, "/path1", Handlers.Status(201));
                router.AddPath(HttpMethod.Delete, "/path1", Handlers.Status(204));

                var resp1 = await client.GetAsync(new Uri(server.Uri, "/path1"));
                Assert.Equal(201, (int)resp1.StatusCode);

                var resp2 = await client.DeleteAsync(new Uri(server.Uri, "/path1"));
                Assert.Equal(204, (int)resp2.StatusCode);

                var resp3 = await client.PostAsync(new Uri(server.Uri, "/path1"),
                    new StringContent("hi"));
                Assert.Equal(405, (int)resp3.StatusCode);

                var resp4 = await client.GetAsync(new Uri(server.Uri, "/path2"));
                Assert.Equal(404, (int)resp4.StatusCode);
            });

        [Fact]
        public async void PathRegexMatch() =>
            await WithServerAndClient(Handlers.Router(out var router), async (server, client) =>
            {
                router.AddRegex("/path[12]", Handlers.Status(201));
                router.AddRegex("/path[34]", Handlers.Status(419));

                var resp1 = await client.GetAsync(new Uri(server.Uri, "/path1"));
                Assert.Equal(201, (int)resp1.StatusCode);

                var resp2 = await client.GetAsync(new Uri(server.Uri, "/path3"));
                Assert.Equal(419, (int)resp2.StatusCode);

                var resp3 = await client.GetAsync(new Uri(server.Uri, "/path5"));
                Assert.Equal(404, (int)resp3.StatusCode);
            });

        [Fact]
        public async void PathRegexMatchWithMethod() =>
            await WithServerAndClient(Handlers.Router(out var router), async (server, client) =>
            {
                router.AddRegex(HttpMethod.Get, "/path[12]", Handlers.Status(201));
                router.AddRegex(HttpMethod.Delete, "/path[12]", Handlers.Status(204));

                var resp1 = await client.GetAsync(new Uri(server.Uri, "/path1"));
                Assert.Equal(201, (int)resp1.StatusCode);

                var resp2 = await client.DeleteAsync(new Uri(server.Uri, "/path1"));
                Assert.Equal(204, (int)resp2.StatusCode);

                var resp3 = await client.PostAsync(new Uri(server.Uri, "/path1"),
                    new StringContent("hi"));
                Assert.Equal(405, (int)resp3.StatusCode);

                var resp4 = await client.DeleteAsync(new Uri(server.Uri, "/path3"));
                Assert.Equal(404, (int)resp4.StatusCode);
            });
    }
}
