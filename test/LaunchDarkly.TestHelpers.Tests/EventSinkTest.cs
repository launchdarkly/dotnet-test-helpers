using System;
using System.Threading;
using Xunit;
using Xunit.Sdk;

namespace LaunchDarkly.TestHelpers
{
    public class EventSinkTest
    {
        [Fact]
        public void ExpectNoValueSuccessWithDefaultTimeout()
        {
            var es = new EventSink<string>();
            es.ExpectNoValue();
        }

        [Fact]
        public void ExpectNoValueSuccessWithSpecifiedTimeout()
        {
            var es = new EventSink<string>();
            es.ExpectNoValue(TimeSpan.FromMilliseconds(200));
        }

        [Fact]
        public void ExpectNoValueImmediateFailure()
        {
            var es = new EventSink<string>();
            es.Enqueue("a");
            Assert.ThrowsAny<XunitException>(() => es.ExpectNoValue());
        }

        [Fact]
        public void ExpectNoValueFailureWithinTimeout()
        {
            var es = new EventSink<string>();
            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                es.Enqueue("a");
            }).Start();
            Assert.ThrowsAny<XunitException>(() => es.ExpectNoValue(TimeSpan.FromMilliseconds(200)));
        }

        [Fact]
        public void ExpectValueImmediateSuccess()
        {
            var es = new EventSink<string>();
            es.Enqueue("a");
            es.Enqueue("b");
            Assert.Equal("a", es.ExpectValue());
            Assert.Equal("b", es.ExpectValue());
        }

        [Fact]
        public void ExpectValueSuccessWithinTimeout()
        {
            var es = new EventSink<string>();
            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                es.Enqueue("a");
            }).Start();
            Assert.Equal("a", es.ExpectValue(TimeSpan.FromMilliseconds(200)));
        }

        [Fact]
        public void ExpectValueTimeout()
        {
            var es = new EventSink<string>();
            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
                es.Enqueue("a");
            }).Start();
            Assert.ThrowsAny<XunitException>(() => es.ExpectValue(TimeSpan.FromMilliseconds(50)));
        }

        [Fact]
        public void AddIsEquivalentToEnqueue()
        {
            var es = new EventSink<string>();
            es.Add(this, "a");
            Assert.Equal("a", es.ExpectValue());
        }

        [Fact]
        public void TryTakeValueImmediateSuccess()
        {
            var es = new EventSink<string>();
            es.Enqueue("a");
            Assert.True(es.TryTakeValue(out var v));
            Assert.Equal("a", v);
        }

        [Fact]
        public void TryTakeValueSuccessWithinTimeout()
        {
            var es = new EventSink<string>();
            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                es.Enqueue("a");
            }).Start();
            Assert.True(es.TryTakeValue(out var v));
            Assert.Equal("a", v);
        }
    }
}
