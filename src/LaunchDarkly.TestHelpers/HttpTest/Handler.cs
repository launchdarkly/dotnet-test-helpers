using System;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// An asynchronous function that handles HTTP requests to simulate the desired server
    /// behavior in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use the factory methods in <see cref="Handlers"/> to create standard implementations.
    /// There are two ways to use these handlers:
    /// </para>
    /// <list type="number">
    /// <item><description> In end-to-end HTTP testing against an <see cref="HttpServer"/>.
    /// This provides the most realistic operating conditions for your HTTP client test code,
    /// since it will do actual HTTP and can be accessed by an external client without any
    /// customization of the client other than specifying the target URI.
    /// </description></item>
    /// <item><description> To simulate responses from an <c>HttpClient</c> using a custom
    /// <c>HttpMessageHandler</c> that does not make any network requests. In this case,
    /// you can configure your handler(s) just the same, but use
    /// <see cref="Handlers.AsMessageHandler(Handler)"/> to convert the result into an
    /// <c>HttpMessageHandler</c> that can be used in an <c>HttpClient</c>. This can be
    /// useful in testing code that makes requests to a real external URI, redirecting it
    /// to your internal fixture. It also allows simulating network errors; see
    /// <see cref="Handlers.Error(Exception)"/>.
    /// </description></item>
    /// </list>
    /// <para>
    /// In either use case, there is always a single handler that represents the server as
    /// a whole. If you want to apply the effects of multiple handles to the same
    /// response, use <see cref="Handlers.Then(Handler, Handler)"/>. If you want the server
    /// to return different responses for different requests, use combinators such as
    /// <see cref="Handlers.Sequential(Handler[])"/> or <see cref="Handlers.Router(out SimpleRouter)"/>.
    /// </para>
    /// </remarks>
    /// <param name="context">the request context</param>
    /// <returns>the asynchronous task</returns>
    public delegate Task Handler(IRequestContext context);
}
