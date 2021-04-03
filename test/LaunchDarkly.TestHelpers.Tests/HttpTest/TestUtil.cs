using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public static class TestUtil
    {
        public static async Task WithServerAndClient(Handler handler, Func<HttpServer, HttpClient, Task> action)
        {
            using (var server = HttpServer.Start(handler))
            {
                using (var client = new HttpClient())
                {
                    await action(server, client);
                }
            }
        }
    }
}
