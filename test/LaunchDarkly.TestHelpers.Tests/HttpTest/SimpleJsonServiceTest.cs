#if !NET452
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class SimpleJsonServiceTest
    {
        [Fact]
        public async void NoPathsMatchByDefault() =>
            await WithServerAndClient(new SimpleJsonService(), async (server, client) =>
            {
                var resp = await client.GetAsync(server.Uri);
                Assert.Equal(404, (int)resp.StatusCode);
            });

        [Fact]
        public async void SimpleEndpoint()
        {
            var received = new EventSink<bool>();
            var service = new SimpleJsonService();
            service.Route(HttpMethod.Post, "/path", context =>
            {
                received.Enqueue(true);
                return SimpleResponse.Of(202);
            });
            await WithServerAndClient(service, async (server, client) =>
            {
                var resp = await client.PostAsync(new Uri(server.Uri, "/path"), null);
                Assert.Equal(202, (int)resp.StatusCode);
                received.ExpectValue();
            });
        }

        [Fact]
        public async void EndpointWithJsonInput()
        {
            var received = new EventSink<JsonParams>();
            var service = new SimpleJsonService();
            service.Route(HttpMethod.Post, "/path", (IRequestContext context, JsonParams p) =>
            {
                received.Enqueue(p);
                return SimpleResponse.Of(202);
            });
            await WithServerAndClient(service, async (server, client) =>
            {
                var resp = await client.PostAsync(new Uri(server.Uri, "/path"),
                    new StringContent(@"{""number"":1,""name"":""a""}", Encoding.UTF8, "application/json"));
                Assert.Equal(202, (int)resp.StatusCode);
                var p = received.ExpectValue();
                Assert.Equal(1, p.Number);
                Assert.Equal("a", p.Name);
            });
        }

        [Fact]
        public async void EndpointWithJsonInputBadRequest()
        {
            var received = new EventSink<JsonParams>();
            var service = new SimpleJsonService();
            service.Route(HttpMethod.Post, "/path", (IRequestContext context, JsonParams p) =>
            {
                received.Enqueue(p);
                return SimpleResponse.Of(202);
            });
            await WithServerAndClient(service, async (server, client) =>
            {
                var resp = await client.PostAsync(new Uri(server.Uri, "/path"),
                    new StringContent(@"{""no", Encoding.UTF8, "application/json"));
                Assert.Equal(400, (int)resp.StatusCode);
                received.ExpectNoValue();
            });
        }

        [Fact]
        public async void EndpointWithJsonOutput()
        {
            var service = new SimpleJsonService();
            service.Route<JsonParams>(HttpMethod.Get, "/path", context =>
            {
                return SimpleResponse.Of(200, new JsonParams { Number = 1, Name = "a" });
            });
            await WithServerAndClient(service, async (server, client) =>
            {
                var resp = await client.GetAsync(new Uri(server.Uri, "/path"));
                Assert.Equal(200, (int)resp.StatusCode);
                var p = JsonSerializer.Deserialize<JsonParams>(await resp.Content.ReadAsStringAsync());
                Assert.Equal(1, p.Number);
                Assert.Equal("a", p.Name);
            });
        }

        [Fact]
        public async void EndpointWithJsonInputAndOutput()
        {
            var service = new SimpleJsonService();
            service.Route<JsonParams, JsonParams>(HttpMethod.Post, "/path", (IRequestContext context, JsonParams p) =>
            {
                return SimpleResponse.Of(200, new JsonParams { Number = p.Number + 1, Name = p.Name + "b" });
            });
            await WithServerAndClient(service, async (server, client) =>
            {
                var resp = await client.PostAsync(new Uri(server.Uri, "/path"),
                    new StringContent(@"{""number"":1,""name"":""a""}", Encoding.UTF8, "application/json"));
                Assert.Equal(200, (int)resp.StatusCode);
                var p = JsonSerializer.Deserialize<JsonParams>(await resp.Content.ReadAsStringAsync());
                Assert.Equal(2, p.Number);
                Assert.Equal("ab", p.Name);
            });
        }

        [Fact]
        public async void EndpointWithJsonInputAndOutputBadRequest()
        {
            var received = new EventSink<JsonParams>();
            var service = new SimpleJsonService();
            service.Route<JsonParams, JsonParams>(HttpMethod.Post, "/path", (IRequestContext context, JsonParams p) =>
            {
                received.Enqueue(p);
                return SimpleResponse.Of(200, new JsonParams { Number = p.Number + 1, Name = p.Name + "b" });
            });
            await WithServerAndClient(service, async (server, client) =>
            {
                var resp = await client.PostAsync(new Uri(server.Uri, "/path"),
                    new StringContent(@"{""no", Encoding.UTF8, "application/json"));
                Assert.Equal(400, (int)resp.StatusCode);
                received.ExpectNoValue();
            });
        }

        [Fact]
        public async void EndpointCanReturnHeaders()
        {
            var service = new SimpleJsonService();
            service.Route(HttpMethod.Post, "/path", context =>
            {
                return SimpleResponse.Of(201).WithHeader("Location", "http://somewhere/");
            });
            await WithServerAndClient(service, async (server, client) =>
            {
                var resp = await client.PostAsync(new Uri(server.Uri, "/path"), null);
                Assert.Equal(201, (int)resp.StatusCode);
                Assert.Equal("http://somewhere/", resp.Headers.Location.ToString());
            });
        }

        [Fact]
        public async void EndpointGetsPathParam()
        {
            var received = new EventSink<string>();
            var service = new SimpleJsonService();
            service.Route(HttpMethod.Post, "/path/([^/]*)/please", context =>
            {
                received.Enqueue(context.GetPathParam(0));
                return SimpleResponse.Of(202);
            });
            await WithServerAndClient(service, async (server, client) =>
            {
                var resp = await client.PostAsync(new Uri(server.Uri, "/path/thing/please"), null);
                Assert.Equal(202, (int)resp.StatusCode);
                Assert.Equal("thing", received.ExpectValue());
            });
        }

        sealed class JsonParams
        {
            [JsonPropertyName("number")] public int Number { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; }
        }
    }
}
#endif
