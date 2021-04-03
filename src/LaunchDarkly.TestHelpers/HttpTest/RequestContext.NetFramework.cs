#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using WireMock;
using WireMock.ResponseBuilders;
using WireMock.Types;
using WireMock.Util;

namespace LaunchDarkly.TestHelpers.HttpTest
{
	internal sealed class WireMockRequestContext : IRequestContext
	{
		public RequestInfo RequestInfo { get; }
		public CancellationToken CancellationToken { get; }

		private readonly IResponseBuilder _builder;
		private int _statusCode = 200;
		private readonly Dictionary<string, WireMockList<string>> _headers =
			new Dictionary<string, WireMockList<string>>();
		private byte[] _body = null;

		internal WireMockRequestContext(RequestMessage request, CancellationToken cancellationToken)
        {
			var headers = new NameValueCollection();
			foreach (var kv in request.Headers)
            {
				foreach (var v in kv.Value)
                {
					headers.Add(kv.Key, v);
                }
            }
			RequestInfo = new RequestInfo
			{
				Method = request.Method.ToUpper(),
				Path = request.Path,
				Query = request.RawQuery,
				Headers = headers,
				Body = request.Body
			};
			CancellationToken = cancellationToken;
			_builder = Response.Create();
        }

		internal ResponseMessage ToResponse()
        {
			return new ResponseMessage
			{
				StatusCode = _statusCode,
				Headers = _headers,
				BodyData = _body is null ? null :
					new BodyData() { BodyAsBytes = _body }
			};
        }

		public void SetStatus(int statusCode) => _statusCode = statusCode;

		public void SetHeader(string name, string value) => _headers[name] =
			new WireMockList<string>(value);

        public void AddHeader(string name, string value)
		{
			if (_headers.TryGetValue(name, out var list))
			{
				list.Add(value);
			}
			else
            {
				SetHeader(name, value);
            }
        }

		public Task WriteChunkedDataAsync(byte[] data) =>
			throw new NotSupportedException("Can't use WriteChunkedDataAsync in .NET Framework 4.5.x.");

#pragma warning disable CS1998
        public async Task WriteFullResponseAsync(string contentType, byte[] data)
        {
			SetHeader("Content-Type", contentType);
			_body = data;
        }
#pragma warning restore CS1998
    }
}

#endif
