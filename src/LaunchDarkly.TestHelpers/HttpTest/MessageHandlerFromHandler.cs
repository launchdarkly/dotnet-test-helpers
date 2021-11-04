using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
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

        private sealed class FakeRequestContext : IRequestContext
        {
            public RequestInfo RequestInfo { get; set; }
            public CancellationToken CancellationToken { get; set; }
            public TaskCompletionSource<HttpResponseMessage> Ready = new TaskCompletionSource<HttpResponseMessage>();

            private readonly HttpResponseMessage _response = new HttpResponseMessage();
            private SimplePipe _pipe;

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
                    _pipe = new SimplePipe();
                    CancellationToken.Register(_pipe.Close);
                    _response.Content = new StreamContent(_pipe);
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
                    _pipe.Write(data, 0, data.Length);
                }
                await Task.Yield();
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

        // HttpMessageHandler has to provide the response body as a stream, which we might be
        // writing to in chunks. MemoryStream doesn't have blocking read behavior, so we simulate
        // something more pipe-like by using a queue.
        private sealed class SimplePipe : Stream
        {
            public Stream ReadStream { get; }

            private readonly BlockingCollection<byte[]> _chunks = new BlockingCollection<byte[]>();
            private readonly MemoryStream _availableData = new MemoryStream();

            private long _readPosition = 0;
            private bool _eof = false;

            public override bool CanRead { get { return true; } }

            public override bool CanSeek { get { return false; } }

            public override bool CanWrite { get { return true; } }

            public override void Flush() { }

            public override long Length => _availableData.Position - _readPosition;

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_eof)
                {
                    return 0;
                }
                var endPos = _availableData.Position;
                if (endPos == _readPosition)
                {
                    var chunk = _chunks.Take();
                    if (chunk == null)
                    {
                        return 0;
                    }
                    _availableData.Write(chunk, 0, chunk.Length);
                    endPos = _availableData.Position;
                }
                _availableData.Position = _readPosition;
                int numRead = _availableData.Read(buffer, offset, count);
                _readPosition += numRead;
                return numRead;
            }

            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();

            public override void SetLength(long value) =>
                throw new NotImplementedException();

            public override void Close() =>
                _chunks.Add(null);

            public override void Write(byte[] buffer, int offset, int count)
            {
                var chunk = new byte[count];
                Buffer.BlockCopy(buffer, offset, chunk, 0, count);
                _chunks.Add(chunk);
            }
        }
    }
}
