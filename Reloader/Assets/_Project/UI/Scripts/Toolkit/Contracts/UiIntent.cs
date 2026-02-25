using System;
using Reloader.UI.Toolkit.Runtime;

namespace Reloader.UI.Toolkit.Contracts
{
    public readonly struct UiIntent
    {
        public UiIntent(string key, object payload = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Intent key is required.", nameof(key));
            }

            Key = key;
            Payload = payload;
        }

        public string Key { get; }
        public object Payload { get; }
    }

    public static class UiContractGuard
    {
        public static IDisposable Bind(IUiController controller, IUiViewBinder viewBinder)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (viewBinder == null)
            {
                throw new ArgumentNullException(nameof(viewBinder));
            }

            void Handle(UiIntent intent) => controller.HandleIntent(intent);
            viewBinder.IntentRaised += Handle;
            return new Subscription(viewBinder, Handle);
        }

        public static bool TryResolveCommand(UiIntent intent, UiActionMapConfig actionMap, out string commandName)
        {
            if (actionMap == null)
            {
                commandName = null;
                return false;
            }

            return actionMap.TryResolve(intent.Key, out commandName);
        }

        private sealed class Subscription : IDisposable
        {
            private readonly IUiViewBinder _viewBinder;
            private readonly Action<UiIntent> _handler;
            private bool _disposed;

            public Subscription(IUiViewBinder viewBinder, Action<UiIntent> handler)
            {
                _viewBinder = viewBinder;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _viewBinder.IntentRaised -= _handler;
            }
        }
    }
}
