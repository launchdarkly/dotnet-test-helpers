using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LaunchDarkly.TestHelpers
{
    /// <summary>
    /// Miscellaneous Xunit helpers.
    /// </summary>
    public static class Assertions
    {
        /// <summary>
        /// Polls a function repeatedly until it returns true, failing if it times out.
        /// </summary>
        /// <param name="timeout">the maximum time to wait</param>
        /// <param name="interval">the interval to poll at</param>
        /// <param name="test">the function to test</param>
        public static void AssertEventually(TimeSpan timeout, TimeSpan interval, Func<bool> test)
        {
            var deadline = DateTime.Now.Add(timeout);
            while (DateTime.Now < deadline)
            {
                if (test())
                {
                    return;
                }
                Thread.Sleep(interval);
            }
            Assert.True(false, "timed out before test condition was satisfied");
        }

        /// <summary>
        /// Polls a function repeatedly until it returns true, failing if it times out.
        /// </summary>
        /// <param name="timeout">the maximum time to wait</param>
        /// <param name="interval">the interval to poll at</param>
        /// <param name="test">the function to test</param>
        public static async Task AssertEventuallyAsync(TimeSpan timeout, TimeSpan interval, Func<Task<bool>> test)
        {
            var deadline = DateTime.Now.Add(timeout);
            while (DateTime.Now < deadline)
            {
                if (await test())
                {
                    return;
                }
                await Task.Delay(interval);
            }
            Assert.True(false, "timed out before test condition was satisfied");
        }
    }
}
