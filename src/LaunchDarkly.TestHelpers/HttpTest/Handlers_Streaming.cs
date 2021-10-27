using System.Text;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    public static partial class Handlers
    {
        /// <summary>
        /// Creates a <see cref="Handler"/> that starts writing a chunked response.
        /// </summary>
        /// <example>
        /// <code>
        ///     var handler = Handlers.StartChunks("text/my-stream-data")
        ///         .Then(Handlers.WriteChunkString("data1"))
        ///         .Then(Handlers.WriteChunkString("data2"));
        /// </code>
        /// </example>
        /// <param name="contentType">the content type</param>
        /// <param name="encoding">character encoding to include in the Content-Type header;
        /// if not specified, Content-Type will not specify an encoding</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="WriteChunk(byte[])"/>
        /// <seealso cref="WriteChunkString(string, Encoding)"/>
        public static Handler StartChunks(string contentType, Encoding encoding = null) =>
            async ctx =>
            {
                ctx.SetHeader("Content-Type", ContentTypeWithEncoding(contentType, encoding));
                await ctx.WriteChunkedDataAsync(null);
            };

        /// <summary>
        /// Creates a <see cref="Handler"/> that writes a chunk of response data.
        /// </summary>
        /// <param name="data">the chunk data</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="StartChunks(string, Encoding)"/>
        /// <seealso cref="WriteChunkString(string, Encoding)"/>
        public static Handler WriteChunk(byte[] data) =>
            async ctx => await ctx.WriteChunkedDataAsync(data);

        /// <summary>
        /// Creates a <see cref="Handler"/> that writes a chunk of response data.
        /// </summary>
        /// <param name="data">the chunk data as a string</param>
        /// <param name="encoding">character encoding to use for this chunk (defaults to UTF8)</param>
        /// <returns>a <see cref="Handler"/></returns>
        /// <seealso cref="StartChunks(string, Encoding)"/>
        /// <seealso cref="WriteChunk(byte[])"/>
        public static Handler WriteChunkString(string data, Encoding encoding = null) =>
            async ctx => await ctx.WriteChunkedDataAsync(
                data == null ? null : (encoding ?? Encoding.UTF8).GetBytes(data));

        /// <summary>
        /// Shortcut handlers for simulating a Server-Sent Events stream.
        /// </summary>
        public static class SSE
        {
            /// <summary>
            /// Starts a chunked stream with the standard content type "text/event-stream",
            /// and the charset UTF-8.
            /// </summary>
            /// <returns>a <see cref="Handler"/></returns>
            public static Handler Start() => StartChunks("text/event-stream", Encoding.UTF8);

            /// <summary>
            /// Writes an SSE comment line.
            /// </summary>
            /// <param name="text">the content that should appear after the colon</param>
            /// <returns>a <see cref="Handler"/></returns>
            public static Handler Comment(string text) => WriteChunkString(":" + text + "\n");

            /// <summary>
            /// Writes an SSE event terminated by two newlines.
            /// </summary>
            /// <param name="content">the full event</param>
            /// <returns>a <see cref="Handler"/></returns>
            public static Handler Event(string content) => WriteChunkString(content + "\n\n");

            /// <summary>
            /// Writes an SSE event created from individual fields.
            /// </summary>
            /// <param name="message">the "event" field</param>
            /// <param name="data">the "data" field</param>
            /// <returns>a <see cref="Handler"/></returns>
            public static Handler Event(string message, string data) =>
                Event("event: " + message + "\ndata: " + data);

            /// <summary>
            /// Waits indefinitely without closing the stream. Equivalent to <see cref="Hang"/>.
            /// </summary>
            /// <returns>a <see cref="Handler"/></returns>
            public static Handler LeaveOpen() => Hang();
        }
    }
}
