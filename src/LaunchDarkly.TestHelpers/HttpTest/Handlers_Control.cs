using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public static partial class Handlers
    {
        /// <summary>
        /// Creates a <see cref="Handler"/> that sleeps for the specified amount of time.
        /// </summary>
        /// <param name="delay">how long to delay</param>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Delay(TimeSpan delay) =>
            async ctx => await Task.Delay(delay, ctx.CancellationToken);

        /// <summary>
        /// Creates a <see cref="Handler"/> that sleeps indefinitely, holding the connection open,
        /// until the server is closed.
        /// </summary>
        /// <returns>a <see cref="Handler"/></returns>
        public static Handler Hang() => Delay(Timeout.InfiniteTimeSpan);

        /// <summary>
        /// Creates a <see cref="Handler"/> that throws an exception. The effect of this depends
        /// on how the handler is being used - see Remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When used in a real end-to-end HTTP scenario with an <see cref="HttpServer"/>, this
        /// simply results in an HTTP 500 response. There is no way to simulate a network error
        /// in this framework.
        /// </para>
        /// <para>
        /// However, when used with <see cref="Handlers.AsMessageHandler(Handler)"/>,
        /// this causes the exception to be thrown out of the <c>HttpClient</c>. Therefore, you can
        /// use this handler in test code that needs to simulate an I/O error from <c>HttpClient</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///     var messageHandler = Handlers.Error(new IOException("bad hostname")).AsMessageHandler();
        ///     var httpClient = new HttpClient(messageHandler);
        /// </code>
        /// </example>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static Handler Error(Exception ex) => Sync(_ => throw ex);

        /// <summary>
        /// Creates a <see cref="RequestRecorder"/> that captures requests.
        /// </summary>
        /// <remarks>
        /// You won't normally need to use this directly, since the <see cref="HttpServer"/> has a
        /// <see cref="RequestRecorder"/> built in, but you could use it to capture a subset of
        /// requests.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var handler = Handlers.Record(out var recorder).Then(Handlers.Status(200));
        /// </code>
        /// </example>
        /// <param name="recorder">receives the new <see cref="RequestRecorder"/> instance</param>
        /// <returns>a <see cref="Handler"/> that will pass requests to the recorder</returns>
        /// <seealso cref="RequestRecorder"/>
        public static Handler Record(out RequestRecorder recorder)
        {
            recorder = new RequestRecorder();
            return recorder.Handler;
        }
    }
}
