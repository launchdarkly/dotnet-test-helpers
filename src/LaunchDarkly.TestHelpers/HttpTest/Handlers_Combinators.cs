
namespace LaunchDarkly.TestHelpers.HttpTest
{
    public static partial class Handlers
    {
        /// <summary>
        /// Chains another handler to be executed immediately after this one.
        /// </summary>
        /// <remarks>
        /// Changing the status code or headers after a previous handler has already written a
        /// response body will not work.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var handler = Handlers.Delay(TimeSpan.FromSeconds(2)).Then(Handlers.Status(200));
        /// </code>
        /// </example>
        /// <param name="first">the first handler to execute</param>
        /// <param name="second">the next handler to execute</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Then(this Handler first, Handler second) =>
            async ctx =>
            {
                await first(ctx);
                if (!ctx.CancellationToken.IsCancellationRequested)
                {
                    await second(ctx);
                }
            };

        /// <summary>
        /// Creates a <see cref="SimpleRouter"/> for delegating to other handlers based on the
        /// request path.
        /// </summary>
        /// <example>
        /// <code>
        ///     var server = HttpServer.Start(Handlers.Router(out var router));
        ///     router.AddPath("/goodpath", Handlers.Status(200));
        ///     router.AddPath("/badpath", Handlers.Status(400));
        /// </code>
        /// </example>
        /// <param name="router">receives the new <see cref="SimpleRouter"/> instance</param>
        /// <returns></returns>
        public static Handler Router(out SimpleRouter router)
        {
            router = new SimpleRouter();
            return router.Handler;
        }

        /// <summary>
        /// Creates a <see cref="Handler"/> that delegates to each of the specified handlers in sequence
        /// as each request is received.
        /// </summary>
        /// <remarks>
        /// Any requests that happen after the last handler in the list has been used will receive a
        /// 500 error.
        /// </remarks>
        /// <param name="handlers">a list of handlers</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Sequential(params Handler[] handlers) =>
            new SequentialHandler(handlers).Handler;

        /// <summary>
        /// Creates a <see cref="HandlerSwitcher"/> for changing handler behavior dynamically.
        /// It is initially set to delegate to <see cref="Handlers.Default"/>.
        /// </summary>
        /// <param name="switchable">receives the new <see cref="HandlerSwitcher"/> instance</param>
        /// <returns>a <see cref="HandlerSwitcher"/></returns>
        public static Handler Switchable(out HandlerSwitcher switchable)
        {
            switchable = new HandlerSwitcher(Handlers.Default);
            return switchable.Handler;
        }
    }
}
