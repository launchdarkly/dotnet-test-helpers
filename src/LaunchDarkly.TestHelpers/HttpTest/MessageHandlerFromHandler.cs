using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    internal sealed class MessageHandlerFromHandler : HttpClientHandler
    {
        private readonly Handler _handler;

        internal MessageHandlerFromHandler(Handler handler) { _handler = handler; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var headers = new NameValueCollection();
            foreach (var kv in request.Headers)
            {
                foreach (var v in kv.Value)
                {
                    headers.Add(kv.Key, v);
                }
            }
            string body = null;
            if (request.Content != null)
            {
                body = await request.Content.ReadAsStringAsync();
            }
            var requestInfo = new RequestInfo
            {
                Method = request.Method.ToString(),
                Uri = request.RequestUri,
                Path = request.RequestUri.PathAndQuery,
                Headers = headers,
                Body = body
            };
            var ctx = new FakeRequestContext { RequestInfo = requestInfo, CancellationToken = cancellationToken };
            _ = Task.Run(async () =>
            {
                try
                {
                    await _handler(ctx);
                }
                catch (Exception e)
                {
                    ctx.Ready.TrySetException(e);
                }
                ctx.MakeResponseAvailable();
            });
            return await ctx.Ready.Task;
        }

        private class FakeRequestContext : IRequestContext
        {
            public RequestInfo RequestInfo { get; set; }
            public CancellationToken CancellationToken { get; set; }
            public TaskCompletionSource<HttpResponseMessage> Ready = new TaskCompletionSource<HttpResponseMessage>();

            private readonly HttpResponseMessage _response = new HttpResponseMessage();
            private System.IO.Pipes.AnonymousPipeServerStream _pipe;

            private string _deferredContentType;

            internal void MakeResponseAvailable()
            {
                Ready.TrySetResult(_response);
            }

            public void AddHeader(string name, string value)
            {
                if (name.ToLower() == "content-type")
                {
                    _deferredContentType = value;
                }
                else
                {
                    _response.Headers.Add(name, value);
                }
            }

            public void SetHeader(string name, string value)
            {
                if (name.ToLower() == "content-type")
                {
                    _deferredContentType = value;
                }
                else
                {
                    _response.Headers.Remove(name);
                    _response.Headers.Add(name, value);
                }
            }

            public void SetStatus(int statusCode) =>
                _response.StatusCode = (HttpStatusCode)statusCode;

            public async Task WriteChunkedDataAsync(byte[] data)
            {
                if (_pipe is null)
                {
                    _pipe = new System.IO.Pipes.AnonymousPipeServerStream();
                    _response.Content = new StreamContent(
                        new System.IO.Pipes.AnonymousPipeClientStream(_pipe.GetClientHandleAsString())
                    );
                    _response.Content.Headers.ContentEncoding.Add("chunked");
                    if (_deferredContentType != null)
                    {
                        _response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(_deferredContentType);
                    }
                }
                if (data is null)
                {
                    MakeResponseAvailable();
                }
                else
                {
                    await _pipe.WriteAsync(data, 0, data.Length, CancellationToken);
                }
            }

            public async Task WriteFullResponseAsync(string contentType, byte[] data)
            {
                _response.Content = new ByteArrayContent(data);
                if (contentType != null || _deferredContentType != null)
                {
                    _response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType ?? _deferredContentType);
                }
                MakeResponseAvailable();
                await Task.Yield();
            }
        }
    }
}
