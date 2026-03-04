using System;

namespace Reloader.Core.UI
{
    public sealed class RuntimeHubChannelBinder<TChannel>
        where TChannel : class
    {
        private readonly Func<TChannel> _runtimeChannelResolver;
        private readonly Action<TChannel> _subscribe;
        private readonly Action<TChannel> _unsubscribe;

        private TChannel _configuredChannel;
        private TChannel _subscribedChannel;

        public RuntimeHubChannelBinder(
            Func<TChannel> runtimeChannelResolver,
            Action<TChannel> subscribe,
            Action<TChannel> unsubscribe)
        {
            _runtimeChannelResolver = runtimeChannelResolver ?? throw new ArgumentNullException(nameof(runtimeChannelResolver));
            _subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));
            _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
            UsesRuntimeChannel = true;
        }

        public bool UsesRuntimeChannel { get; private set; }

        public void Configure(TChannel channel)
        {
            _configuredChannel = channel;
            UsesRuntimeChannel = channel == null;
        }

        public TChannel ResolveAndBind()
        {
            var resolvedChannel = UsesRuntimeChannel
                ? _runtimeChannelResolver()
                : _configuredChannel;

            if (UsesRuntimeChannel)
            {
                _configuredChannel = resolvedChannel;
            }

            Bind(resolvedChannel);
            return resolvedChannel;
        }

        public void Unbind()
        {
            if (_subscribedChannel == null)
            {
                return;
            }

            _unsubscribe(_subscribedChannel);
            _subscribedChannel = null;
        }

        private void Bind(TChannel channel)
        {
            if (channel == null)
            {
                Unbind();
                return;
            }

            if (ReferenceEquals(_subscribedChannel, channel))
            {
                return;
            }

            Unbind();
            _subscribe(channel);
            _subscribedChannel = channel;
        }
    }
}
