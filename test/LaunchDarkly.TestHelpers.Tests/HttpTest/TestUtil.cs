using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

        public static async Task<string> ReadAllAvailableStringAsync(Stream stream, int max)
        {
            var buf = new byte[max];
            int n = await stream.ReadAsync(buf, 0, buf.Length);
            return n == 0 ? null : Encoding.UTF8.GetString(buf, 0, n);
        }

        public static async Task AssertNoContent(HttpResponseMessage resp)
        {
            if (resp.Content is null)
            {
                return;
            }
            // In .NET 5.0, there may be a zero-length content object instead of null.
            var data = await resp.Content.ReadAsByteArrayAsync();
            if (data != null)
            {
                Assert.Empty(data);
            }
        }

        public static void AssertNoHeader(HttpResponseMessage resp, string headerName)
        {
            var headers = HeadersFor(resp, headerName);
            if (headers != null)
            {
                Assert.False(headers.TryGetValues(headerName, out _));
            }
        }
            

        public static void AssertHeader(HttpResponseMessage resp, string headerName, params string[] expectedValues)
        {
            // HTTP implementations are inconsistent in terms of how they parse multiple header
            // values - could be returned as multiple values or as a comma-delimited string
            var headers = HeadersFor(resp, headerName);
            Assert.NotNull(headers);
            Assert.True(headers.TryGetValues(headerName, out var values));
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
                    return resp.Content is null ? null : resp.Content.Headers;
                default:
                    return resp.Headers;
            }
        }
    }
}
