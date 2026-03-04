using System;
using UnityEngine;

namespace Reloader.Core.Events
{
    [Obsolete("GameEvents is legacy. Use RuntimeKernelBootstrapper typed runtime ports or IGameEventsRuntimeHub.", false)]
    public static class GameEvents
    {
        [Obsolete("Use RuntimeKernelBootstrapper.RuntimeEvents.OnSaveStarted.", false)]
        public static event Action OnSaveStarted;
        [Obsolete("Use RuntimeKernelBootstrapper.RuntimeEvents.OnSaveCompleted.", false)]
        public static event Action OnSaveCompleted;
        [Obsolete("Use RuntimeKernelBootstrapper.RuntimeEvents.OnLoadStarted.", false)]
        public static event Action OnLoadStarted;
        [Obsolete("Use RuntimeKernelBootstrapper.RuntimeEvents.OnLoadCompleted.", false)]
        public static event Action OnLoadCompleted;
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.OnItemPickupRequested.", false)]
        public static event Action<string> OnItemPickupRequested;
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.OnItemStored.", false)]
        public static event Action<string, InventoryArea, int> OnItemStored;
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.OnItemPickupRejected.", false)]
        public static event Action<string, PickupRejectReason> OnItemPickupRejected;
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.OnBeltSelectionChanged.", false)]
        public static event Action<int> OnBeltSelectionChanged;
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged.", false)]
        public static event Action OnInventoryChanged;
        [Obsolete("Use RuntimeKernelBootstrapper.WeaponEvents.OnWeaponEquipped.", false)]
        public static event Action<string> OnWeaponEquipped;
        [Obsolete("Use RuntimeKernelBootstrapper.WeaponEvents.OnWeaponFired.", false)]
        public static event Action<string, Vector3, Vector3> OnWeaponFired;
        [Obsolete("Use RuntimeKernelBootstrapper.WeaponEvents.OnWeaponReloaded.", false)]
        public static event Action<string, int, int> OnWeaponReloaded;
        [Obsolete("Use RuntimeKernelBootstrapper.WeaponEvents.OnProjectileHit.", false)]
        public static event Action<string, Vector3, float> OnProjectileHit;

        [Obsolete("Use RuntimeKernelBootstrapper.RuntimeEvents.RaiseSaveStarted().", false)]
        public static void RaiseSaveStarted() => OnSaveStarted?.Invoke();
        [Obsolete("Use RuntimeKernelBootstrapper.RuntimeEvents.RaiseSaveCompleted().", false)]
        public static void RaiseSaveCompleted() => OnSaveCompleted?.Invoke();
        [Obsolete("Use RuntimeKernelBootstrapper.RuntimeEvents.RaiseLoadStarted().", false)]
        public static void RaiseLoadStarted() => OnLoadStarted?.Invoke();
        [Obsolete("Use RuntimeKernelBootstrapper.RuntimeEvents.RaiseLoadCompleted().", false)]
        public static void RaiseLoadCompleted() => OnLoadCompleted?.Invoke();
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.RaiseItemPickupRequested(itemId).", false)]
        public static void RaiseItemPickupRequested(string itemId) => OnItemPickupRequested?.Invoke(itemId);
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.RaiseItemStored(itemId, area, index).", false)]
        public static void RaiseItemStored(string itemId, InventoryArea area, int index) => OnItemStored?.Invoke(itemId, area, index);
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.RaiseItemPickupRejected(itemId, reason).", false)]
        public static void RaiseItemPickupRejected(string itemId, PickupRejectReason reason) => OnItemPickupRejected?.Invoke(itemId, reason);
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.RaiseBeltSelectionChanged(selectedBeltIndex).", false)]
        public static void RaiseBeltSelectionChanged(int selectedBeltIndex) => OnBeltSelectionChanged?.Invoke(selectedBeltIndex);
        [Obsolete("Use RuntimeKernelBootstrapper.InventoryEvents.RaiseInventoryChanged().", false)]
        public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
        [Obsolete("Use RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponEquipped(itemId).", false)]
        public static void RaiseWeaponEquipped(string itemId) => OnWeaponEquipped?.Invoke(itemId);
        [Obsolete("Use RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponFired(itemId, origin, direction).", false)]
        public static void RaiseWeaponFired(string itemId, Vector3 origin, Vector3 direction) => OnWeaponFired?.Invoke(itemId, origin, direction);
        [Obsolete("Use RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponReloaded(itemId, magazineCount, reserveCount).", false)]
        public static void RaiseWeaponReloaded(string itemId, int magazineCount, int reserveCount) => OnWeaponReloaded?.Invoke(itemId, magazineCount, reserveCount);
        [Obsolete("Use RuntimeKernelBootstrapper.WeaponEvents.RaiseProjectileHit(itemId, point, damage).", false)]
        public static void RaiseProjectileHit(string itemId, Vector3 point, float damage) => OnProjectileHit?.Invoke(itemId, point, damage);
    }
}
