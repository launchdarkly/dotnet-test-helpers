using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

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

        public static void AssertNoHeader(HttpResponseMessage resp, string headerName) =>
            Assert.False(HeadersFor(resp, headerName).TryGetValues(headerName, out _));

        public static void AssertHeader(HttpResponseMessage resp, string headerName, params string[] expectedValues)
        {
            Assert.True(HeadersFor(resp, headerName).TryGetValues(headerName, out var values));
            Assert.Equal(string.Join(", ", expectedValues), string.Join(", ", values));
        }

        private static HttpHeaders HeadersFor(HttpResponseMessage resp, string headerName)
        {
            switch (headerName.ToLower())
            {
                case "content-length":
                case "content-type":
                    return resp.Content.Headers;
                default:
                    return resp.Headers;
            }
        }
    }
}
