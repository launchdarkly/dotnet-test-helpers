using System;
using System.Threading;
using Xunit;
using Xunit.Sdk;

namespace LaunchDarkly.TestHelpers
{
    public class AssertionsTest
    {
        [Fact]
        public void AssertEventuallySuccessOnFirstTry()
        {
            int calls = 0;
            Assertions.AssertEventually(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(10), () =>
            {
                Interlocked.Increment(ref calls);
                return true;
            });
            Assert.Equal(1, calls);
        }

        [Fact]
        public void AssertEventuallySuccessBeforeTimeout()
        {
            int calls = 0;
            Assertions.AssertEventually(TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(10), () =>
            {
                var n = Interlocked.Increment(ref calls);
                return n > 50;
            });
        }

        [Fact]
        public void AssertEventuallyTimeout()
        {
            int calls = 0;
            Assert.ThrowsAny<XunitException>(() =>
                Assertions.AssertEventually(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10), () =>
                {
                    var n = Interlocked.Increment(ref calls);
                    return n > 50;
                })
                );
        }

#pragma warning disable CS1998
        [Fact]
        public async void AssertEventuallyAsyncSuccessOnFirstTry()
        {
            int calls = 0;
            await Assertions.AssertEventuallyAsync(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(10), async () =>
            {
                Interlocked.Increment(ref calls);
                return true;
            });
            Assert.Equal(1, calls);
        }

        [Fact]
        public async void AssertEventuallyAsyncSuccessBeforeTimeout()
        {
            int calls = 0;
            await Assertions.AssertEventuallyAsync(TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(10), async () =>
            {
                var n = Interlocked.Increment(ref calls);
                return n > 50;
            });
        }

        [Fact]
        public async void AssertEventuallyAsyncTimeout()
        {
            int calls = 0;
            await Assert.ThrowsAnyAsync<XunitException>(async () =>
                await Assertions.AssertEventuallyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10), async () =>
                {
                    var n = Interlocked.Increment(ref calls);
                    return n > 50;
                })
                );
        }
#pragma warning restore CS1998
    }
}
