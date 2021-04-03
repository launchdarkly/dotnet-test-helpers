using System.Threading.Tasks;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class HandlerSwitcherTest
    {
        [Fact]
        public async Task SwitchHandlers()
        {
            var switchable = Handlers.Switchable(Handlers.Status(200));
            await WithServerAndClient(switchable, async (server, client) =>
            {
                var resp1 = await client.GetAsync(server.Uri);
                Assert.Equal(200, (int)resp1.StatusCode);

                switchable.Target = Handlers.Status(400);

                var resp2 = await client.GetAsync(server.Uri);
                Assert.Equal(400, (int)resp2.StatusCode);
            });
        }
    }
}
