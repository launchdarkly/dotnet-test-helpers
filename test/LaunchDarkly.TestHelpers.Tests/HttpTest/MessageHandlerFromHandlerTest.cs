using System.IO;
using System.Net.Http;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class MessageHandlerFromHandlerTest
    {
        private const string FakeUri = "http://not-real";

        [Fact]
        public async void SimpleResponseNoBody()
        {
            var handler = Handlers.Status(419);
            using (var client = new HttpClient(handler.AsMessageHandler()))
            {
                var resp = await client.GetAsync(FakeUri);
                Assert.Equal(419, (int)resp.StatusCode);
                AssertNoHeader(resp, "content-type");
                Assert.Null(resp.Content);
            }
        }

        [Fact]
        public async void SimpleResponseWithBody()
        {
            var handler = Handlers.BodyJson("[]");
            using (var client = new HttpClient(handler.AsMessageHandler()))
            {
                var resp = await client.GetAsync(FakeUri);
                Assert.Equal(200, (int)resp.StatusCode);
                AssertHeader(resp, "content-type", "application/json");
                Assert.Equal("[]", await resp.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void Recorder()
        {
            var handler = Handlers.Record(out var recorder)
                .Then(Handlers.Status(503));
            using (var client = new HttpClient(handler.AsMessageHandler()))
            {
                var resp = await client.GetAsync(FakeUri + "/subpath");
                Assert.Equal(503, (int)resp.StatusCode);

                var request = recorder.RequireRequest();
                Assert.Equal("/subpath", request.Path);
            }
        }

        [Fact]
        public async void Sequential()
        {
            var handler = Handlers.Sequential(Handlers.Status(419), Handlers.Status(503));
            using (var client = new HttpClient(handler.AsMessageHandler()))
            {
                var resp1 = await client.GetAsync(FakeUri);
                Assert.Equal(419, (int)resp1.StatusCode);

                var resp2 = await client.GetAsync(FakeUri);
                Assert.Equal(503, (int)resp2.StatusCode);
            }
        }

        [Fact]
        public async void SimulateIOException()
        {
            var ex = new IOException("sorry");
            var handler = Handlers.Error(ex);
            using (var client = new HttpClient(handler.AsMessageHandler()))
            {
                var gotException = await Assert.ThrowsAnyAsync<IOException>(() => client.GetAsync(FakeUri));
                Assert.Equal(ex, gotException);
            }
        }
    }
}
