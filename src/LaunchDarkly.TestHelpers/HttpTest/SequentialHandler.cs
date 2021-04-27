using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    internal sealed class SequentialHandler
    {
        private readonly List<Handler> _handlers;
        private int _index = 0;

        internal Handler Handler => DoRequestAsync;

        internal SequentialHandler(Handler[] handlers)
        {
            _handlers = new List<Handler>(handlers);
        }

        private async Task DoRequestAsync(IRequestContext ctx)
        {
            int i = Interlocked.Increment(ref _index);
            if (i > _handlers.Count)
            {
                throw new System.Exception("Test server received unexpected request");
            }
            else
            {
                await _handlers[i - 1](ctx);
            }
        }
    }
}
