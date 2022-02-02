using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    internal sealed class RequestContextImpl : IRequestContext
    {
        private readonly HttpListenerContext _ctx;
        private readonly string[] _pathParams;
        private bool _chunked;

        public RequestInfo RequestInfo { get; }
        public CancellationToken CancellationToken { get; }

        internal RequestContextImpl(
            HttpListenerContext listenerCtx,
            RequestInfo requestInfo,
            CancellationToken cancellationToken,
            string[] pathParams
            )
        {
            _ctx = listenerCtx;
            RequestInfo = requestInfo;
            CancellationToken = cancellationToken;
            _pathParams = pathParams;
        }

        internal static RequestContextImpl FromHttpListenerContext(
            HttpListenerContext listenerCtx,
            CancellationToken cancellationToken
            )
        {
            // The computation of Uri below is meant to handle both the usual case where the request
            // does not provide an absolute URI, in which case ctx.Request.Url is the computed full URI,
            // and the special case where the server is a fake proxy server and the request URI is a
            // full absolute URI.
            var requestInfo = new RequestInfo
            {
                Method = listenerCtx.Request.HttpMethod.ToUpper(),
                Uri = listenerCtx.Request.RawUrl.StartsWith("http") ?
                    new Uri(listenerCtx.Request.RawUrl) : listenerCtx.Request.Url,
                Path = listenerCtx.Request.Url.LocalPath,
                Query = listenerCtx.Request.Url.Query,
                Headers = listenerCtx.Request.Headers,
                Body = ""
            };
            if (listenerCtx.Request.HasEntityBody)
            {
                using (var sr = new StreamReader(listenerCtx.Request.InputStream, Encoding.UTF8))
                {
                    var s = sr.ReadToEnd();
                    requestInfo.Body = s;

                }
            }
            return new RequestContextImpl(listenerCtx, requestInfo, cancellationToken, null);
        }

        public void SetStatus(int statusCode) =>
            _ctx.Response.StatusCode = statusCode;

        public void SetHeader(string name, string value) =>
            _ctx.Response.Headers.Set(name, value);

        public void AddHeader(string name, string value) =>
            _ctx.Response.Headers.Add(name, value);

        public async Task WriteChunkedDataAsync(byte[] data)
        {
            if (data != null && data.Length != 0)
            {
                if (!_chunked)
                {
                    _ctx.Response.SendChunked = true; // it's an error to set this more than once per request
                    _chunked = true;
                }
                await _ctx.Response.OutputStream.WriteAsync(data, 0, data.Length, CancellationToken);
                await _ctx.Response.OutputStream.FlushAsync();
            }
        }

        public async Task WriteFullResponseAsync(string contentType, byte[] data)
        {
            _ctx.Response.ContentType = contentType;
            _ctx.Response.ContentLength64 = data.Length;
            await _ctx.Response.OutputStream.WriteAsync(data, 0, data.Length, CancellationToken);
        }

        public string GetPathParam(int index) =>
            (_pathParams != null && index >= 0 && index < _pathParams.Length) ?
            _pathParams[index] : null;

        public IRequestContext WithPathParams(string[] pathParams) =>
            new RequestContextImpl(_ctx, RequestInfo, CancellationToken, pathParams);
    }
}
