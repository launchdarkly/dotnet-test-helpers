using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class StreamingTest
    {
        [Fact]
        public async Task BasicChunkedResponseWithNoCharsetInHeader()
        {
            string[] chunks = new string[] { "first.", "second.", "third" };
            await DoStreamingTest(
                Handlers.StartChunks("text/plain"),
                chunks.Select(c => Handlers.WriteChunkString(c)).ToArray(),
                Handlers.Hang(),
                "text/plain",
                chunks
                );
        }

        [Fact]
        public async Task BasicChunkedResponseWithCharsetInHeader()
        {
            string[] chunks = new string[] { "first.", "second.", "third" };
            await DoStreamingTest(
                Handlers.StartChunks("text/plain", Encoding.UTF8),
                chunks.Select(c => Handlers.WriteChunkString(c)).ToArray(),
                Handlers.Hang(),
                "text/plain; charset=utf-8",
                chunks
                );
        }

        [Fact]
        public async Task SSEStream()
        {
            string[] chunks = new string[] { "first.", "second.", "third" };
            await DoStreamingTest(
                Handlers.SSE.Start(),
                new Handler[]
                {
                    Handlers.SSE.Event("e1", "d1"),
                    Handlers.SSE.Comment("comment"),
                    Handlers.SSE.Event("e2", "d2"),
                    Handlers.SSE.Event("data: all done")
                },
                Handlers.SSE.LeaveOpen(),
                "text/event-stream; charset=utf-8",
                new string[] {
                    "event: e1\ndata: d1\n\n",
                    ":comment\n",
                    "event: e2\ndata: d2\n\n",
                    "data: all done\n\n"
                }
                );
        }

        private async Task DoStreamingTest(
            Handler startAction,
            Handler[] chunkActions,
            Handler endAction,
            string expectedContentType,
            string[] expectedChunks
            )
        {
            // Use these gates to ensure that it's really sending the data incrementally - we won't
            // write the next chunk till we read the previous one has been read.
            TaskCompletionSource<bool>[] didWriteChunk = Enumerable.Range(0, expectedChunks.Length)
                .Select(_ => new TaskCompletionSource<bool>()).ToArray();
            TaskCompletionSource<bool>[] didReadChunk = Enumerable.Range(0, expectedChunks.Length)
                .Select(_ => new TaskCompletionSource<bool>()).ToArray();

            Handler handler = startAction
                .Then(async ctx =>
                {
                    for (int i = 0; i < expectedChunks.Length; i++)
                    {
                        await chunkActions[i](ctx);
                        didWriteChunk[i].SetResult(true);
                        await didReadChunk[i].Task;
                    }
                })
                .Then(endAction);

            await WithServerAndClient(handler, async (server, client) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get, server.Uri);
                var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

                Assert.Equal(200, (int)resp.StatusCode);
                Assert.Equal(expectedContentType, resp.Content.Headers.ContentType.ToString());

                var stream = await resp.Content.ReadAsStreamAsync();

                for (int i = 0; i < expectedChunks.Length; i++)
                {
                    await didWriteChunk[i].Task;

                    var buf = new byte[100];
                    int n = await stream.ReadAsync(buf, 0, buf.Length);
                    string s = Encoding.UTF8.GetString(buf, 0, n);
                    Assert.Equal(expectedChunks[i], s);

                    didReadChunk[i].SetResult(true);
                }
            });
        }
    }
}
