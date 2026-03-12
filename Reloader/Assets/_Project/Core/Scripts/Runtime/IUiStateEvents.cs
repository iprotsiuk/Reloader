using System;

namespace Reloader.Core.Runtime
{
    public interface IUiStateEvents
    {
        bool IsShopTradeMenuOpen { get; }
        bool IsWorkbenchMenuVisible { get; }
        bool IsTabInventoryVisible { get; }
        bool IsEscMenuVisible { get; }
        bool IsDevConsoleVisible { get; }
        bool IsAnyMenuOpen { get; }

        event Action<bool> OnWorkbenchMenuVisibilityChanged;
        event Action<bool> OnTabInventoryVisibilityChanged;
        event Action<bool> OnEscMenuVisibilityChanged;
        event Action<bool> OnDevConsoleVisibilityChanged;

        void RaiseWorkbenchMenuVisibilityChanged(bool isVisible);
        void RaiseTabInventoryVisibilityChanged(bool isVisible);
        void RaiseEscMenuVisibilityChanged(bool isVisible);
        void RaiseDevConsoleVisibilityChanged(bool isVisible);
    }
}
