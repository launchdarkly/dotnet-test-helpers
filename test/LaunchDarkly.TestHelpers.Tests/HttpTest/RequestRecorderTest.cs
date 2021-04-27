﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using static LaunchDarkly.TestHelpers.HttpTest.TestUtil;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public class RequestRecorderTest
    {
        [Fact]
        public async Task RequestWithoutBody()
        {
            await WithServerAndClient(Handlers.Status(200), async (server, client) =>
            {
                var requestedUri = new Uri(server.Uri, "/request/path");
                var req = new HttpRequestMessage(HttpMethod.Get, requestedUri);
                req.Headers.Add("header-name", "header-value");

                var resp = await client.SendAsync(req);
                Assert.Equal(200, (int)resp.StatusCode);

                var received = server.Recorder.RequireRequest();
                Assert.Equal("GET", received.Method);
                Assert.Equal(requestedUri, received.Uri);
                Assert.Equal("/request/path", received.Path);
                Assert.Equal("", received.Query);
                Assert.Equal("header-value", received.Headers["header-name"]);
                Assert.Equal("", received.Body);
            });
        }

        [Fact]
        public async Task RequestWithQueryString()
        {
            await WithServerAndClient(Handlers.Status(200), async (server, client) =>
            {
                var requestedUri = new Uri(server.Uri, "/request/path?a=b");
                var req = new HttpRequestMessage(HttpMethod.Get, requestedUri);
                req.Headers.Add("header-name", "header-value");

                var resp = await client.SendAsync(req);
                Assert.Equal(200, (int)resp.StatusCode);

                var received = server.Recorder.RequireRequest();
                Assert.Equal("GET", received.Method);
                Assert.Equal(requestedUri, received.Uri);
                Assert.Equal("/request/path", received.Path);
                Assert.Equal("?a=b", received.Query);
                Assert.Equal("header-value", received.Headers["header-name"]);
                Assert.Equal("", received.Body);
            });
        }

        [Fact]
        public async Task RequestWithBody()
        {
            await WithServerAndClient(Handlers.Status(200), async (server, client) =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, server.Uri.ToString() + "request/path");
                req.Content = new StringContent("hello", Encoding.UTF8, "text/plain");

                var resp = await client.SendAsync(req);
                Assert.Equal(200, (int)resp.StatusCode);

                var received = server.Recorder.RequireRequest();
                Assert.Equal("POST", received.Method);
                Assert.Equal("/request/path", received.Path);
                Assert.Equal("hello", received.Body);
            });
        }
    }
}