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
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        private readonly BlockingCollection<RequestInfo> _requests = new BlockingCollection<RequestInfo>();

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
        /// Consumes and returns the first request in the queue, blocking until one is available.
        /// Throws an exception if the timeout expires.
        /// </summary>
        /// <param name="timeout">the maximum length of time to wait</param>
        /// <returns>the request information</returns>
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
        public RequestInfo RequireRequest() => RequireRequest(DefaultTimeout);

        public void RequireNoRequests(TimeSpan timeout)
        {
            if (_requests.TryTake(out var _, timeout))
            {
                throw new Exception("received an unexpected request");
            }
        }

#pragma warning disable CS1998 // async method with no awaits
        private async Task DoRequestAsync(IRequestContext ctx)
        {
            _requests.Add(ctx.RequestInfo);
        }
#pragma warning restore CS1998

        public static implicit operator Handler(RequestRecorder me) => me.Handler;
    }
}
