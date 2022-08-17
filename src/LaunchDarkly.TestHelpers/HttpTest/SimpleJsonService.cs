using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaunchDarkly.TestHelpers.HttpTest
{   
    /// <summary>
    /// A minimal framework for a JSON-based REST service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class builds on <see cref="Handlers"/> and <see cref="SimpleRouter"/> to provide a
    /// simplified structure for implementing a basic REST service that can use JSON-serializable
    /// types for request and response bodies. For very basic services that are used in a test
    /// scenario, this is much lighter-weight and more portable than using ASP.NET Core or ASP.NET.
    /// </para>
    /// <para>
    /// The basic pattern is to call the <c>Route</c> methods to map any number of endpoint
    /// handlers. Each handler method takes an <see cref="IRequestContext"/> as the first
    /// parameter, and can optionally take a value of some JSON-deserializable type as a second
    /// parameter, which will be automatically parsed from the request body. Its return value is
    /// either a <see cref="SimpleResponse"/> if there is no response body, or a
    /// <see cref="SimpleResponse{T}"/> containing a value that will be serialized to JSON as a
    /// response body.
    /// </para>
    /// <para>
    /// For simple path parameters, you may inclue a regex containing capture groups in the path,
    /// and then call <see cref="IRequestContext.GetPathParam(int)"/> to get the value. Any path
    /// containing parentheses is assumed to be a regex, otherwise it is taken as a literal.
    /// </para>
    /// <para>
    /// This class uses <c>System.Text.Json</c> for JSON conversions.
    /// </para>
    /// </remarks>
    public sealed class SimpleJsonService
    {
        /// <summary>
        /// Returns the stable <see cref="Handler"/> that is the external entry point to this
        /// delegator. This is used implicitly if you use a <c>SimpleJsonService</c> anywhere that
        /// a <see cref="Handler"/> is expected.
        /// </summary>
        public Handler Handler => _handler;

#pragma warning disable CS1591 // no doc comment for this implicit conversion
        public static implicit operator Handler(SimpleJsonService me) => me.Handler;
#pragma warning restore CS1591

        /// <summary>
        /// Options for System.Text.Json to enable the standard behavior of camelcasing property names.
        /// </summary>
        public static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly Handler _handler;
        private readonly SimpleRouter _router;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public SimpleJsonService()
        {
            _handler = Handlers.Router(out _router);
        }

        /// <summary>
        /// Adds an asynchronous endpoint handler with no request body parameters and no response body.
        /// </summary>
        /// <param name="method">the HTTP method</param>
        /// <param name="path">the endpoint path</param>
        /// <param name="asyncHandler">the handler</param>
        public void Route(
            HttpMethod method,
            string path,
            Func<IRequestContext, Task<SimpleResponse>> asyncHandler
        ) =>
            AddPathInternal(method, path, Wrap(asyncHandler));

        /// <summary>
        /// Adds an asynchronous endpoint handler with a JSON request body parameter and no response body.
        /// </summary>
        /// <param name="method">the HTTP method</param>
        /// <param name="path">the endpoint path</param>
        /// <param name="asyncHandler">the handler</param>
        public void Route<TInput>(
            HttpMethod method,
            string path,
            Func<IRequestContext, TInput, Task<SimpleResponse>> asyncHandler
        ) =>
            AddPathInternal(method, path, Wrap(asyncHandler));

        /// <summary>
        /// Adds an asynchronous endpoint handler with no request body parameters and a JSON response.
        /// </summary>
        /// <param name="method">the HTTP method</param>
        /// <param name="path">the endpoint path</param>
        /// <param name="asyncHandler">the handler</param>
        public void Route<TOutput>(
            HttpMethod method,
            string path,
            Func<IRequestContext, Task<SimpleResponse<TOutput>>> asyncHandler
        ) =>
            AddPathInternal(method, path, Wrap(asyncHandler));

        /// <summary>
        /// Adds an asynchronous endpoint handler with a JSON request body parameter and a JSON response.
        /// </summary>
        /// <param name="method">the HTTP method</param>
        /// <param name="path">the endpoint path</param>
        /// <param name="asyncHandler">the handler</param>
        public void Route<TInput, TOutput>(
            HttpMethod method,
            string path,
            Func<IRequestContext, TInput, Task<SimpleResponse<TOutput>>> asyncHandler
        ) =>
            AddPathInternal(method, path, Wrap(asyncHandler));

        /// <summary>
        /// Adds a synchronous endpoint handler with no request body parameters and no response body.
        /// </summary>
        /// <param name="method">the HTTP method</param>
        /// <param name="path">the endpoint path</param>
        /// <param name="syncHandler">the handler</param>
        public void Route(
            HttpMethod method,
            string path,
            Func<IRequestContext, SimpleResponse> syncHandler
        ) =>
            AddPathInternal(method, path, Wrap(context => Task.FromResult(syncHandler(context))));

        /// <summary>
        /// Adds a synchronous endpoint handler with no request body parameters and a JSON response.
        /// </summary>
        /// <param name="method">the HTTP method</param>
        /// <param name="path">the endpoint path</param>
        /// <param name="syncHandler">the handler</param>
        public void Route<TInput>(
            HttpMethod method,
            string path,
            Func<IRequestContext, TInput, SimpleResponse> syncHandler
        ) =>
            AddPathInternal(method, path, Wrap(
                (IRequestContext context, TInput input) => Task.FromResult(syncHandler(context, input))));

        /// <summary>
        /// Adds a synchronous endpoint handler with no request body parameters and a JSON response.
        /// </summary>
        /// <param name="method">the HTTP method</param>
        /// <param name="path">the endpoint path</param>
        /// <param name="syncHandler">the handler</param>
        public void Route<TOutput>(
        HttpMethod method,
            string path,
            Func<IRequestContext, SimpleResponse<TOutput>> syncHandler
        ) =>
            AddPathInternal(method, path, Wrap(context => Task.FromResult(syncHandler(context))));

        /// <summary>
        /// Adds an asynchronous endpoint handler with a JSON request body parameter and a JSON response.
        /// </summary>
        /// <param name="method">the HTTP method</param>
        /// <param name="path">the endpoint path</param>
        /// <param name="syncHandler">the handler</param>
        public void Route<TInput, TOutput>(
            HttpMethod method,
            string path,
            Func<IRequestContext, TInput, SimpleResponse<TOutput>> syncHandler
        ) =>
            AddPathInternal(method, path, Wrap(
                (IRequestContext context, TInput input) => Task.FromResult(syncHandler(context, input))));

        private void AddPathInternal(HttpMethod method, string path, Handler handler)
        {
            if (path.Contains("("))
            {
                _router.AddRegex(method, path, handler);
            }
            else
            {
                _router.AddPath(method, path, handler);
            }
        }

        private Handler Wrap(Func<IRequestContext, Task<SimpleResponse>> handler) =>
            async context =>
            {
                try
                {
                    var result = await handler(context);
                    await Handlers.Status(result.Status)(context);
                    foreach (var kv in result.Headers)
                    {
                        foreach (var v in kv.Value)
                        {
                            await Handlers.Header(kv.Key, v)(context);
                        }
                    }
                }
                catch (BadRequestException e)
                {
                    await Handlers.Status(400)(context);
                    await Handlers.BodyString("text/plain", e.Message)(context);
                }
            };

        private Handler Wrap<TInput>(Func<IRequestContext, TInput, Task<SimpleResponse>> handler) =>
            Wrap(async context =>
            {
                var input = ParseInput<TInput>(context);
                return await handler(context, input);
            });

        private Handler Wrap<TOutput>(Func<IRequestContext, Task<SimpleResponse<TOutput>>> handler) =>
            Wrap(async context =>
            {
                var result = await handler(context);
                if (typeof(TOutput).IsValueType || !result.Body.Equals(default(TOutput)))
                {
                    await Handlers.BodyJson(JsonSerializer.Serialize(result.Body, SerializerOptions))(context);
                }
                return result.Base;
            });

        private Handler Wrap<TInput, TOutput>(Func<IRequestContext, TInput, Task<SimpleResponse<TOutput>>> handler) =>
            Wrap(async context =>
            {
                var input = ParseInput<TInput>(context);
                var result = await handler(context, input);
                await Handlers.BodyJson(JsonSerializer.Serialize(result.Body, SerializerOptions))(context);
                return result.Base;
            });

        private TInput ParseInput<TInput>(IRequestContext context)
        {
            try
            {
                return JsonSerializer.Deserialize<TInput>(context.RequestInfo.Body, SerializerOptions);
            }
            catch (JsonException e)
            {
                throw new BadRequestException(e.Message);
            }
        }
        
        private sealed class BadRequestException : Exception
        {
            public BadRequestException(string message) : base(message) { }
        }
    }
}
