using System;

namespace Reloader.Core.Runtime
{
    public interface IUiStateEvents
    {
        bool IsShopTradeMenuOpen { get; }
        bool IsWorkbenchMenuVisible { get; }
        bool IsTabInventoryVisible { get; }
        bool IsAnyMenuOpen { get; }

        event Action<bool> OnWorkbenchMenuVisibilityChanged;
        event Action<bool> OnTabInventoryVisibilityChanged;

        void RaiseWorkbenchMenuVisibilityChanged(bool isVisible);
        void RaiseTabInventoryVisibilityChanged(bool isVisible);
    }
}
