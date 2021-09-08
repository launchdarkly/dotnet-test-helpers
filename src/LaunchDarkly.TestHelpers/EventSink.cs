using System;
using System.Collections.Concurrent;
using Xunit;

namespace LaunchDarkly.TestHelpers
{
    /// <summary>
    /// A synchronous blocking queue with Xunit assertion helpers.
    /// </summary>
    /// <typeparam name="T">the type of items in the queue</typeparam>
    public sealed class EventSink<T>
    {
        private readonly BlockingCollection<T> _queue = new BlockingCollection<T>();

        /// <summary>
        /// Equivalent to <see cref="Enqueue(T)"/>, but with a first <c>sender</c> parameter so it
        /// can be used as an event handler.
        /// </summary>
        /// <param name="sender">the event sender</param>
        /// <param name="args">the event data</param>
        public void Add(object sender, T args) => Enqueue(args);

        /// <summary>
        /// Adds a value to the queue.
        /// </summary>
        /// <param name="arg">the value</param>
        public void Enqueue(T arg) => _queue.Add(arg);

        /// <summary>
        /// Equivalent to <see cref="ExpectValue(TimeSpan)"/> with a timeout of one second.
        /// </summary>
        /// <returns>the received value</returns>
        public T ExpectValue() => ExpectValue(TimeSpan.FromSeconds(1));

        /// <summary>
        /// Takes a value from the queue and returns it, or causes an assertion failure if the timeout
        /// expires.
        /// </summary>
        /// <param name="timeout">how long to wait for an item</param>
        /// <returns>the item</returns>
        public T ExpectValue(TimeSpan timeout)
        {
            Assert.True(_queue.TryTake(out var value, timeout), "expected an event but did not get one");
            return value;
        }

        /// <summary>
        /// Takes a value from the queue if one exists.
        /// </summary>
        /// <param name="value">receives the value</param>
        /// <returns>true if successful</returns>
        public bool TryTakeValue(out T value) => _queue.TryTake(out value, TimeSpan.FromSeconds(1));

        /// <summary>
        /// Equivalent to <see cref="ExpectNoValue(TimeSpan)"/> with a timeout of 100 milliseconds.
        /// </summary>
        public void ExpectNoValue() => ExpectNoValue(TimeSpan.FromMilliseconds(100));

        /// <summary>
        /// Causes an assertion failure if the queue contains any items.
        /// </summary>
        /// <param name="timeout">how long to wait</param>
        public void ExpectNoValue(TimeSpan timeout) =>
            Assert.False(_queue.TryTake(out _, timeout), "expected no event but got one");
    }
}
