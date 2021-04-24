#if USE_OWIN

using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    internal sealed class OwinRequestContext : IRequestContext
    {
        private readonly IOwinContext _ctx;

        public RequestInfo RequestInfo { get; }
        public CancellationToken CancellationToken { get; }

        internal OwinRequestContext(IOwinContext ctx, RequestInfo requestInfo, CancellationToken cancellationToken)
        {
            _ctx = ctx;
            RequestInfo = requestInfo;
            CancellationToken = cancellationToken;
        }

        internal static OwinRequestContext FromOwinContext(
            IOwinContext owinCtx,
            CancellationToken cancellationToken
            )
        {
            var requestInfo = new RequestInfo
            {
                Method = owinCtx.Request.Method,
                Uri = owinCtx.Request.Uri,
                Path = owinCtx.Request.Path.ToString(),
                Query = owinCtx.Request.QueryString.HasValue ?
                    ("?" + owinCtx.Request.QueryString.Value) : "",
                Headers = new NameValueCollection(),
                Body = ""
            };
            foreach (var kv in owinCtx.Request.Headers)
            {
                foreach (var v in kv.Value)
                {
                    requestInfo.Headers.Add(kv.Key, v);
                }
            }
            if (owinCtx.Request.Body != null)
            {
                using (var sr = new StreamReader(owinCtx.Request.Body, Encoding.UTF8))
                {
                    var s = sr.ReadToEnd();
                    requestInfo.Body = s;

                }
            }
            return new OwinRequestContext(owinCtx, requestInfo, cancellationToken);
        }

        public void SetStatus(int statusCode) =>
            _ctx.Response.StatusCode = statusCode;

        public void SetHeader(string name, string value) =>
            _ctx.Response.Headers.Set(name, value);

        public void AddHeader(string name, string value) =>
            _ctx.Response.Headers.Append(name, value);

        public async Task WriteChunkedDataAsync(byte[] data)
        {
            if (data != null && data.Length != 0)
            {
                await _ctx.Response.WriteAsync(data, CancellationToken);
            }
        }

        public async Task WriteFullResponseAsync(string contentType, byte[] data)
        {
            _ctx.Response.ContentType = contentType;
            _ctx.Response.ContentLength = data.Length;
            await _ctx.Response.WriteAsync(data, CancellationToken);
        }
    }
}

#endif
