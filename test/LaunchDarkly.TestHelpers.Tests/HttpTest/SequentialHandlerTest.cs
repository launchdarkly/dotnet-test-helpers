using System.Threading.Tasks;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class SequentialHandlerTest
    {
        [Fact]
        public async Task HandlersAreCalledInSequence()
        {
            var handler = Handlers.Sequential(Handlers.Status(200), Handlers.Status(201));

            await WithServerAndClient(handler, async (server, client) =>
            {
                var resp1 = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp1.StatusCode);

                var resp2 = await client.GetAsync(server.Uri);
                Assert.Equal(201, (int)resp2.StatusCode);

                var resp3 = await client.GetAsync(server.Uri);
                Assert.Equal(500, (int)resp3.StatusCode);
            });
        }
    }
}
