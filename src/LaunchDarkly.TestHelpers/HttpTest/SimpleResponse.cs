#if !NET452
using System.Collections.Generic;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// Return type for endpoint handlers with <see cref="SimpleJsonService"/>.
    /// </summary>
    public struct SimpleResponse
    {
        private static KeyValuePair<string, IEnumerable<string>>[] _empty =
            new KeyValuePair<string, IEnumerable<string>>[0];

        /// <summary>
        /// The HTTP status code.
        /// </summary>
        public int Status { get; private set; }
        private readonly Dictionary<string, List<string>> _headers;

        /// <summary>
        /// An enumeration of header keys and values.
        /// </summary>
        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers
        {
            get
            {
                if (_headers is null)
                {
                    return _empty;
                }
                var ret = new Dictionary<string, IEnumerable<string>>();
                foreach (var kv in _headers)
                {
                    ret.Add(kv.Key, kv.Value);
                }
                return ret;
            }
        }

        private SimpleResponse(int status, Dictionary<string, List<string>> headers)
        {
            Status = status;
            _headers = headers;
        }

        /// <summary>
        /// Constructs a <see cref="SimpleResponse"/> with an HTTP status code.
        /// </summary>
        /// <param name="status">the status code</param>
        /// <returns>a <see cref="SimpleResponse"/></returns>
        public static SimpleResponse Of(int status) =>
            new SimpleResponse(status, null);

        /// <summary>
        /// Constructs a <see cref="SimpleResponse"/> with an HTTP status code and a
        /// serializable response body.
        /// </summary>
        /// <param name="status">the status code</param>
        /// <param name="body">the value to serialize</param>
        /// <typeparam name="T">the value type</typeparam>
        /// <returns>a <see cref="SimpleResponse"/></returns>
        public static SimpleResponse<T> Of<T>(int status, T body) =>
            SimpleResponse<T>.Of(status, body);

        /// <summary>
        /// Copies this response and adds a header key and value.
        /// </summary>
        /// <param name="key">a header key</param>
        /// <param name="value">a header value</param>
        /// <returns>a <see cref="SimpleResponse"/></returns>
        public SimpleResponse WithHeader(string key, string value)
        {
            var newDict = new Dictionary<string, List<string>>();
            var found = false;
            if (_headers != null)
            {
                foreach (var kv in _headers)
                {
                    if (kv.Key.ToLower() == key.ToLower())
                    {
                        var values = new List<string>(kv.Value);
                        values.Add(value);
                        newDict[kv.Key] = values;
                        found = true;
                    }
                    else
                    {
                        newDict[kv.Key] = kv.Value;
                    }
                }
            }
            if (!found)
            {
                newDict[key] = new List<string> { value };
            }
            return new SimpleResponse(Status, newDict);
        }
    }

    /// <summary>
    /// Return type for endpoint handlers with <see cref="SimpleJsonService"/>, when they
    /// return a value to be serialized as a response body.
    /// </summary>
    public struct SimpleResponse<T>
    {
        /// <summary>
        /// The basic response properties.
        /// </summary>
        public SimpleResponse Base { get; private set; }

        /// <summary>
        /// The value to serialize as the response.
        /// </summary>
        public T Body { get; private set; }

        private SimpleResponse(SimpleResponse baseResp, T body)
        {
            Base = baseResp;
            Body = body;
        }

        /// <summary>
        /// Constructs a <see cref="SimpleResponse{T}"/> with an HTTP status code and a
        /// serializable response body.
        /// </summary>
        /// <param name="status">the status code</param>
        /// <param name="value">the value to serialize</param>
        /// <returns>a <see cref="SimpleResponse{T}"/></returns>
        public static SimpleResponse<T> Of(int status, T value) =>
            new SimpleResponse<T>(SimpleResponse.Of(status), value);

        /// <summary>
        /// Copies this response and adds a header key and value.
        /// </summary>
        /// <param name="key">a header key</param>
        /// <param name="value">a header value</param>
        /// <returns>a <see cref="SimpleResponse{T}"/></returns>
        public SimpleResponse<T> WithHeader(string key, string value) =>
            new SimpleResponse<T>(Base.WithHeader(key, value), Body);
    }
}
#endif
