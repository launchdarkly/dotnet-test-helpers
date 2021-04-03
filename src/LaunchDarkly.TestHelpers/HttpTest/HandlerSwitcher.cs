
namespace LaunchDarkly.TestHelpers.HttpTest
{
    /// <summary>
    /// A delegator that forwards requests to another handler, which can be changed at any time.
    /// </summary>
    /// <remarks>
    /// There is an implicit conversion allowing a <c>HandlerSwitcher</c> to be used as a
    /// <see cref="Handler"/>.
    /// </remarks>
    public class HandlerSwitcher
    {
        private volatile Handler _target;

        /// <summary>
        /// The handler that will actually handle the request.
        /// </summary>
        public Handler Target
        {
            get => _target;
            set
            {
                _target = value;
            }
        }

        /// <summary>
        /// Returns the stable <see cref="Handler"/> that is the external entry point to this
        /// delegator. This is used implicitly if you use a <c>HandlerDelegator</c> anywhere that
        /// a <see cref="Handler"/> is expected.
        /// </summary>
        public Handler Handler => ctx => _target(ctx);

        internal HandlerSwitcher(Handler target)
        {
            _target = target;
        }

        public static implicit operator Handler(HandlerSwitcher me) => me.Handler;
    }
}
