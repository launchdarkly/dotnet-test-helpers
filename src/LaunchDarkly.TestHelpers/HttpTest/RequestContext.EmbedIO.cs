#if USE_EMBEDIO

using System.Threading;
using System.Threading.Tasks;
using EmbedIO;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    internal sealed class EmbedIORequestContext : IRequestContext
    {
        private readonly IHttpContext _ctx;
        private bool _chunked;

        public RequestInfo RequestInfo { get; }

        public CancellationToken CancellationToken { get; }

        private EmbedIORequestContext(IHttpContext context, RequestInfo requestInfo)
        {
            _ctx = context;
            RequestInfo = requestInfo;
            CancellationToken = context.CancellationToken;
        }

        internal static async Task<EmbedIORequestContext> FromHttpContext(IHttpContext ctx)
        {
            var requestInfo = new RequestInfo
            {
                Method = ctx.Request.HttpMethod.ToUpper(),
                Path = ctx.RequestedPath,
                Query = ctx.Request.Url.Query ?? "",
                Headers = ctx.Request.Headers,
                Body = ""
            };

            if (ctx.Request.HasEntityBody)
            {
                requestInfo.Body = await ctx.GetRequestBodyAsStringAsync() ?? "";
            }
            return new EmbedIORequestContext(ctx, requestInfo);
        }

        public void AddHeader(string name, string value)
        {
            if (name.ToLower() == "content-type")
            {
                _ctx.Response.ContentType = value;
            }
            else
            {
                _ctx.Response.Headers.Add(name, value);
            }
        }

        public void SetHeader(string name, string value)
        {
            if (name.ToLower() == "content-type")
            {
                _ctx.Response.ContentType = value;
            }
            else
            {
                _ctx.Response.Headers.Set(name, value);
            }
        }

        public void SetStatus(int statusCode) => _ctx.Response.StatusCode = statusCode;

        public async Task WriteChunkedDataAsync(byte[] data)
        {
            if (!_chunked)
            {
                _ctx.Response.SendChunked = true;
                _chunked = true;
            }
            await _ctx.Response.OutputStream.WriteAsync(data, 0, data.Length);
        }

        public async Task WriteFullResponseAsync(string contentType, byte[] data)
        {
            _ctx.Response.ContentLength64 = data.Length;
            _ctx.Response.ContentType = contentType;
            await _ctx.Response.OutputStream.WriteAsync(data, 0, data.Length,
                _ctx.CancellationToken);
        }
    }
}

#endif
