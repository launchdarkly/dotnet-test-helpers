using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// A delegator that provides simple request path/method matching. The request is sent to the
    /// handler for the first matching path. If there is no matching path, it returns a 404. If
    /// there is a matching path but only for a different HTTP method, it returns a 405.
    /// </summary>
    public sealed class SimpleRouter
    {
        /// <summary>
        /// Returns the stable <see cref="Handler"/> that is the external entry point to this
        /// delegator. This is used implicitly if you use a <c>SimpleRouter</c> anywhere that
        /// a <see cref="Handler"/> is expected.
        /// </summary>
        public Handler Handler => DoRequestAsync;

        private struct Route
        {
            internal HttpMethod Method { get; set; }
            internal string Path { get; set; }
            internal Regex PathPattern { get; set; }
            internal Handler Handler { get; set; }
        }

        private readonly List<Route> _routes = new List<Route>();

        /// <summary>
        /// Adds an exact-match path.
        /// </summary>
        /// <param name="path">the desired path</param>
        /// <param name="handler">the handler to call for a matching request</param>
        /// <returns>the same instance</returns>
        public SimpleRouter AddPath(string path, Handler handler)
        {
            _routes.Add(new Route { Path = path, Handler = handler });
            return this;
        }

        /// <summary>
        /// Adds an exact-match path, specifying the HTTP method.
        /// </summary>
        /// <param name="method">the desired method</param>
        /// <param name="path">the desired path</param>
        /// <param name="handler">the handler to call for a matching request</param>
        /// <returns>the same instance</returns>
        public SimpleRouter AddPath(HttpMethod method, string path, Handler handler)
        {
            _routes.Add(new Route { Method = method, Path = path, Handler = handler });
            return this;
        }

        /// <summary>
        /// Adds a regex path pattern. If it contains any capture groups, the matched groups
        /// will be available from <see cref="IRequestContext.GetPathParam(int)"/>.
        /// </summary>
        /// <param name="pattern">the regex to match</param>
        /// <param name="handler">the handler to call for a matching request</param>
        /// <returns>the same instance</returns>
        public SimpleRouter AddRegex(string pattern, Handler handler)
        {
            _routes.Add(new Route { PathPattern = new Regex(pattern), Handler = handler });
            return this;
        }

        /// <summary>
        /// Adds a regex path pattern, specifying the HTTP method. If it contains any capture
        /// groups, the matched groups will be available from <see cref="IRequestContext.GetPathParam(int)"/>.
        /// </summary>
        /// <param name="method">the desired method</param>
        /// <param name="pattern">the regex to match</param>
        /// <param name="handler">the handler to call for a matching request</param>
        /// <returns>the same instance</returns>
        public SimpleRouter AddRegex(HttpMethod method, string pattern, Handler handler)
        {
            _routes.Add(new Route { Method = method, PathPattern = new Regex(pattern), Handler = handler });
            return this;
        }

#pragma warning disable CS1591 // no doc comment for this implicit conversion
        public static implicit operator Handler(SimpleRouter me) => me.Handler;
#pragma warning restore CS1591

        private async Task DoRequestAsync(IRequestContext ctx)
        {
            var matchedPath = false;
            foreach (var route in _routes)
            {
                var matchedRoute = false;
                List<string> captures = null;
                if (route.Path != null && route.Path == ctx.RequestInfo.Path)
                {
                    matchedRoute = true;
                }
                else if (route.PathPattern != null)
                {
                    var match = route.PathPattern.Match(ctx.RequestInfo.Path);
                    if (match.Success)
                    {
                        matchedRoute = true;
                        if (match.Groups != null && match.Groups.Count > 1)
                        {
                            captures = new List<string>();
                            for (var i = 1; i < match.Groups.Count; i++)
                            {
                                captures.Add(match.Groups[i].Value);
                            }
                        }
                    }
                }
                if (matchedRoute)
                {
                    matchedPath = true;
                    var ctx1 = captures == null ? ctx : ctx.WithPathParams(captures.ToArray());
                    if (route.Method is null || route.Method.ToString().ToUpper() == ctx.RequestInfo.Method)
                    {
                        await route.Handler(ctx1);
                        return;
                    }
                }
            }
            await Handlers.Status(matchedPath ? 405 : 404)(ctx);
        }
    }
}
