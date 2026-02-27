using System;
using UnityEngine;

namespace Reloader.Core.Events
{
    public static class GameEvents
    {
        public static event Action OnSaveStarted;
        public static event Action OnSaveCompleted;
        public static event Action OnLoadStarted;
        public static event Action OnLoadCompleted;
        public static event Action<string> OnItemPickupRequested;
        public static event Action<string, InventoryArea, int> OnItemStored;
        public static event Action<string, PickupRejectReason> OnItemPickupRejected;
        public static event Action<int> OnBeltSelectionChanged;
        public static event Action OnInventoryChanged;
        public static event Action<string> OnWeaponEquipped;
        public static event Action<string, Vector3, Vector3> OnWeaponFired;
        public static event Action<string, int, int> OnWeaponReloaded;
        public static event Action<string, Vector3, float> OnProjectileHit;

        public static void RaiseSaveStarted() => OnSaveStarted?.Invoke();
        public static void RaiseSaveCompleted() => OnSaveCompleted?.Invoke();
        public static void RaiseLoadStarted() => OnLoadStarted?.Invoke();
        public static void RaiseLoadCompleted() => OnLoadCompleted?.Invoke();
        public static void RaiseItemPickupRequested(string itemId) => OnItemPickupRequested?.Invoke(itemId);
        public static void RaiseItemStored(string itemId, InventoryArea area, int index) => OnItemStored?.Invoke(itemId, area, index);
        public static void RaiseItemPickupRejected(string itemId, PickupRejectReason reason) => OnItemPickupRejected?.Invoke(itemId, reason);
        public static void RaiseBeltSelectionChanged(int selectedBeltIndex) => OnBeltSelectionChanged?.Invoke(selectedBeltIndex);
        public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
        public static void RaiseWeaponEquipped(string itemId) => OnWeaponEquipped?.Invoke(itemId);
        public static void RaiseWeaponFired(string itemId, Vector3 origin, Vector3 direction) => OnWeaponFired?.Invoke(itemId, origin, direction);
        public static void RaiseWeaponReloaded(string itemId, int magazineCount, int reserveCount) => OnWeaponReloaded?.Invoke(itemId, magazineCount, reserveCount);
        public static void RaiseProjectileHit(string itemId, Vector3 point, float damage) => OnProjectileHit?.Invoke(itemId, point, damage);
    }
}
