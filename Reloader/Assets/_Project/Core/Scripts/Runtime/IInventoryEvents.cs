using System;
using Reloader.Core.Events;

namespace Reloader.Core.Runtime
{
    public interface IInventoryEvents
    {
        event Action OnSaveStarted;
        event Action OnSaveCompleted;
        event Action OnLoadStarted;
        event Action OnLoadCompleted;
        event Action<string> OnItemPickupRequested;
        event Action<string, InventoryArea, int> OnItemStored;
        event Action<string, PickupRejectReason> OnItemPickupRejected;
        event Action<int> OnBeltSelectionChanged;
        event Action OnInventoryChanged;
        event Action<int> OnMoneyChanged;

        void RaiseSaveStarted();
        void RaiseSaveCompleted();
        void RaiseLoadStarted();
        void RaiseLoadCompleted();
        void RaiseItemPickupRequested(string itemId);
        void RaiseItemStored(string itemId, InventoryArea area, int index);
        void RaiseItemPickupRejected(string itemId, PickupRejectReason reason);
        void RaiseBeltSelectionChanged(int selectedBeltIndex);
        void RaiseInventoryChanged();
        void RaiseMoneyChanged(int amount);
    }
}
