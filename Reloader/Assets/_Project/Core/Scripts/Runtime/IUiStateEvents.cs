using System;

namespace Reloader.Core.Runtime
{
    public interface IUiStateEvents
    {
        bool IsShopTradeMenuOpen { get; }
        bool IsWorkbenchMenuVisible { get; }
        bool IsTabInventoryVisible { get; }
        bool IsEscMenuVisible { get; }
        bool IsAnyMenuOpen { get; }

        event Action<bool> OnWorkbenchMenuVisibilityChanged;
        event Action<bool> OnTabInventoryVisibilityChanged;
        event Action<bool> OnEscMenuVisibilityChanged;

        void RaiseWorkbenchMenuVisibilityChanged(bool isVisible);
        void RaiseTabInventoryVisibilityChanged(bool isVisible);
        void RaiseEscMenuVisibilityChanged(bool isVisible);
    }
}
