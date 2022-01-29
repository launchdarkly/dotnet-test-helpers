using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// An object that records all requests.
    /// </summary>
    /// <remarks>
    /// Normally you won't need to use this class directly, because <see cref="HttpServer"/>
    /// has a built-in instance that captures all requests. You can use it if you need to
    /// capture only a subset of requests.
    /// </remarks>
    public class RequestRecorder
    {
        /// <summary>
        /// The default timeout for <see cref="RequireRequest()"/>: 5 seconds.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        private readonly BlockingCollection<RequestInfo> _requests = new BlockingCollection<RequestInfo>();
        private readonly object _lock = new object();
        private volatile bool _enabled = true;

        /// <summary>
        /// Returns the stable <see cref="Handler"/> that is the external entry point to this
        /// delegator. This is used implicitly if you use a <c>RequestRecorder</c> anywhere that
        /// a <see cref="Handler"/> is expected.
        /// </summary>
        public Handler Handler => DoRequestAsync;

        /// <summary>
        /// The number of requests currently in the queue.
        /// </summary>
        public int Count => _requests.Count;

        /// <summary>
        /// Set this property to false to turn off recording of requests. It is true by default.
        /// </summary>
        public bool Enabled
        {
            get
            {
                lock (_lock)
                {
                    return _enabled;
                }
            }
            set
            {
                lock (_lock)
                {
                    _enabled = value;
                }
            }
        }

        /// <summary>
        /// Consumes and returns the first request in the queue, blocking until one is available.
        /// </summary>
        /// <param name="timeout">the maximum length of time to wait</param>
        /// <returns>the request information</returns>
        /// <exception cref="TimeoutException">if the timeout expires</exception>
        public RequestInfo RequireRequest(TimeSpan timeout)
        {
            if (!_requests.TryTake(out var req, timeout))
            {
                throw new TimeoutException("timed out waiting for request");
            }
            return req;
        }

        /// <summary>
        /// Returns the first request in the queue, blocking until one is available,
        /// using <see cref="DefaultTimeout"/>.
        /// </summary>
        /// <returns>the request information</returns>
        /// <exception cref="TimeoutException">if the timeout expires</exception>
        public RequestInfo RequireRequest() => RequireRequest(DefaultTimeout);

        /// <summary>
        /// Asserts that there are no requests in the queue and none are received within
        /// the specified timeout.
        /// </summary>
        /// <param name="timeout">the maximum length of time to wait</param>
        /// <exception cref="InvalidOperationException">if a request was received</exception>
        public void RequireNoRequests(TimeSpan timeout)
        {
            if (_requests.TryTake(out var _, timeout))
            {
                throw new InvalidOperationException("received an unexpected request");
            }
        }

#pragma warning disable CS1998 // async method with no awaits
        private async Task DoRequestAsync(IRequestContext ctx)
        {
            if (Enabled)
            {
                _requests.Add(ctx.RequestInfo);
            }
        }
#pragma warning restore CS1998

#pragma warning disable CS1591 // no doc comment for this implicit conversion
        public static implicit operator Handler(RequestRecorder me) => me.Handler;
#pragma warning restore CS1591
    }
}
