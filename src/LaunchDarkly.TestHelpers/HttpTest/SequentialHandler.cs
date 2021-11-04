using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LaunchDarkly.TestHelpers.HttpTest
{
    internal sealed class SequentialHandler
    {
        private readonly List<Handler> _handlers;
        private readonly bool _repeatLast;
        private int _index = 0;

        internal Handler Handler => DoRequestAsync;

        internal SequentialHandler(bool repeatLast, Handler[] handlers)
        {
            _handlers = new List<Handler>(handlers);
            _repeatLast = repeatLast;
        }

        private async Task DoRequestAsync(IRequestContext ctx)
        {
            int i = Interlocked.Increment(ref _index);
            if (i > _handlers.Count)
            {
                if (!_repeatLast)
                {
                    throw new System.Exception("Test server received unexpected request");
                }
                i = _handlers.Count;
            }
            await _handlers[i - 1](ctx);
        }
    }
}
