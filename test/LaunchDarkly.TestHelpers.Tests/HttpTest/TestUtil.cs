using System;
using System.Collections.Specialized;
using System.Linq;
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
            // HTTP implementations are inconsistent in terms of how they parse multiple header
            // values - could be returned as multiple values or as a comma-delimited string
            Assert.True(HeadersFor(resp, headerName).TryGetValues(headerName, out var values));
            var normalizedValues = values.SelectMany(s =>
                s.Trim().Split(',').Select(s1 => s1.Trim())
            ).ToArray();
            Assert.Equal(expectedValues, normalizedValues);
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
