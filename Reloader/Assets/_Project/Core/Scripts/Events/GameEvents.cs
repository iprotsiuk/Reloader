using System;
using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.Core.Events
{
    public static class GameEvents
    {
        public static bool IsShopTradeMenuOpen => RuntimeEvents.IsShopTradeMenuOpen;
        public static bool IsWorkbenchMenuVisible => RuntimeEvents.IsWorkbenchMenuVisible;
        public static bool IsTabInventoryVisible => RuntimeEvents.IsTabInventoryVisible;
        public static bool IsAnyMenuOpen => RuntimeEvents.IsAnyMenuOpen;

        public static event Action OnSaveStarted
        {
            add => RuntimeEvents.OnSaveStarted += value;
            remove => RuntimeEvents.OnSaveStarted -= value;
        }

        public static event Action OnSaveCompleted
        {
            add => RuntimeEvents.OnSaveCompleted += value;
            remove => RuntimeEvents.OnSaveCompleted -= value;
        }

        public static event Action OnLoadStarted
        {
            add => RuntimeEvents.OnLoadStarted += value;
            remove => RuntimeEvents.OnLoadStarted -= value;
        }

        public static event Action OnLoadCompleted
        {
            add => RuntimeEvents.OnLoadCompleted += value;
            remove => RuntimeEvents.OnLoadCompleted -= value;
        }

        public static event Action<string> OnItemPickupRequested
        {
            add => RuntimeEvents.OnItemPickupRequested += value;
            remove => RuntimeEvents.OnItemPickupRequested -= value;
        }

        public static event Action<string, InventoryArea, int> OnItemStored
        {
            add => RuntimeEvents.OnItemStored += value;
            remove => RuntimeEvents.OnItemStored -= value;
        }

        public static event Action<string, PickupRejectReason> OnItemPickupRejected
        {
            add => RuntimeEvents.OnItemPickupRejected += value;
            remove => RuntimeEvents.OnItemPickupRejected -= value;
        }

        public static event Action<int> OnBeltSelectionChanged
        {
            add => RuntimeEvents.OnBeltSelectionChanged += value;
            remove => RuntimeEvents.OnBeltSelectionChanged -= value;
        }

        public static event Action OnInventoryChanged
        {
            add => RuntimeEvents.OnInventoryChanged += value;
            remove => RuntimeEvents.OnInventoryChanged -= value;
        }

        public static event Action<string> OnWeaponEquipStarted
        {
            add => RuntimeEvents.OnWeaponEquipStarted += value;
            remove => RuntimeEvents.OnWeaponEquipStarted -= value;
        }

        public static event Action<string> OnWeaponEquipped
        {
            add => RuntimeEvents.OnWeaponEquipped += value;
            remove => RuntimeEvents.OnWeaponEquipped -= value;
        }

        public static event Action<string> OnWeaponUnequipStarted
        {
            add => RuntimeEvents.OnWeaponUnequipStarted += value;
            remove => RuntimeEvents.OnWeaponUnequipStarted -= value;
        }

        public static event Action<string, Vector3, Vector3> OnWeaponFired
        {
            add => RuntimeEvents.OnWeaponFired += value;
            remove => RuntimeEvents.OnWeaponFired -= value;
        }

        public static event Action<string> OnWeaponReloadStarted
        {
            add => RuntimeEvents.OnWeaponReloadStarted += value;
            remove => RuntimeEvents.OnWeaponReloadStarted -= value;
        }

        public static event Action<string, WeaponReloadCancelReason> OnWeaponReloadCancelled
        {
            add => RuntimeEvents.OnWeaponReloadCancelled += value;
            remove => RuntimeEvents.OnWeaponReloadCancelled -= value;
        }

        public static event Action<string, int, int> OnWeaponReloaded
        {
            add => RuntimeEvents.OnWeaponReloaded += value;
            remove => RuntimeEvents.OnWeaponReloaded -= value;
        }

        public static event Action<string, bool> OnWeaponAimChanged
        {
            add => RuntimeEvents.OnWeaponAimChanged += value;
            remove => RuntimeEvents.OnWeaponAimChanged -= value;
        }

        public static event Action<string, Vector3, float> OnProjectileHit
        {
            add => RuntimeEvents.OnProjectileHit += value;
            remove => RuntimeEvents.OnProjectileHit -= value;
        }

        public static event Action<string> OnShopTradeOpenRequested
        {
            add => RuntimeEvents.OnShopTradeOpenRequested += value;
            remove => RuntimeEvents.OnShopTradeOpenRequested -= value;
        }

        public static event Action<string> OnShopTradeOpened
        {
            add => RuntimeEvents.OnShopTradeOpened += value;
            remove => RuntimeEvents.OnShopTradeOpened -= value;
        }

        public static event Action OnShopTradeClosed
        {
            add => RuntimeEvents.OnShopTradeClosed += value;
            remove => RuntimeEvents.OnShopTradeClosed -= value;
        }

        public static event Action<string, int> OnShopBuyRequested
        {
            add => RuntimeEvents.OnShopBuyRequested += value;
            remove => RuntimeEvents.OnShopBuyRequested -= value;
        }

        public static event Action<string, int> OnShopSellRequested
        {
            add => RuntimeEvents.OnShopSellRequested += value;
            remove => RuntimeEvents.OnShopSellRequested -= value;
        }

        public static event Action<ShopCheckoutRequest> OnShopBuyCheckoutRequested
        {
            add => RuntimeEvents.OnShopBuyCheckoutRequested += value;
            remove => RuntimeEvents.OnShopBuyCheckoutRequested -= value;
        }

        public static event Action<ShopCheckoutRequest> OnShopSellCheckoutRequested
        {
            add => RuntimeEvents.OnShopSellCheckoutRequested += value;
            remove => RuntimeEvents.OnShopSellCheckoutRequested -= value;
        }

        public static event Action<string, int, bool, bool, string> OnShopTradeResult
        {
            add => RuntimeEvents.OnShopTradeResult += value;
            remove => RuntimeEvents.OnShopTradeResult -= value;
        }

        public static event Action<bool> OnWorkbenchMenuVisibilityChanged
        {
            add => RuntimeEvents.OnWorkbenchMenuVisibilityChanged += value;
            remove => RuntimeEvents.OnWorkbenchMenuVisibilityChanged -= value;
        }

        public static event Action<bool> OnTabInventoryVisibilityChanged
        {
            add => RuntimeEvents.OnTabInventoryVisibilityChanged += value;
            remove => RuntimeEvents.OnTabInventoryVisibilityChanged -= value;
        }

        public static event Action<int> OnMoneyChanged
        {
            add => RuntimeEvents.OnMoneyChanged += value;
            remove => RuntimeEvents.OnMoneyChanged -= value;
        }

        public static void RaiseSaveStarted() => RuntimeEvents.RaiseSaveStarted();
        public static void RaiseSaveCompleted() => RuntimeEvents.RaiseSaveCompleted();
        public static void RaiseLoadStarted() => RuntimeEvents.RaiseLoadStarted();
        public static void RaiseLoadCompleted() => RuntimeEvents.RaiseLoadCompleted();
        public static void RaiseItemPickupRequested(string itemId) => RuntimeEvents.RaiseItemPickupRequested(itemId);
        public static void RaiseItemStored(string itemId, InventoryArea area, int index) => RuntimeEvents.RaiseItemStored(itemId, area, index);
        public static void RaiseItemPickupRejected(string itemId, PickupRejectReason reason) => RuntimeEvents.RaiseItemPickupRejected(itemId, reason);
        public static void RaiseBeltSelectionChanged(int selectedBeltIndex) => RuntimeEvents.RaiseBeltSelectionChanged(selectedBeltIndex);
        public static void RaiseInventoryChanged() => RuntimeEvents.RaiseInventoryChanged();
        public static void RaiseWeaponEquipStarted(string itemId) => RuntimeEvents.RaiseWeaponEquipStarted(itemId);
        public static void RaiseWeaponEquipped(string itemId) => RuntimeEvents.RaiseWeaponEquipped(itemId);
        public static void RaiseWeaponUnequipStarted(string itemId) => RuntimeEvents.RaiseWeaponUnequipStarted(itemId);
        public static void RaiseWeaponFired(string itemId, Vector3 origin, Vector3 direction) => RuntimeEvents.RaiseWeaponFired(itemId, origin, direction);
        public static void RaiseWeaponReloadStarted(string itemId) => RuntimeEvents.RaiseWeaponReloadStarted(itemId);
        public static void RaiseWeaponReloadCancelled(string itemId, WeaponReloadCancelReason reason) => RuntimeEvents.RaiseWeaponReloadCancelled(itemId, reason);
        public static void RaiseWeaponReloaded(string itemId, int magazineCount, int reserveCount) => RuntimeEvents.RaiseWeaponReloaded(itemId, magazineCount, reserveCount);
        public static void RaiseWeaponAimChanged(string itemId, bool isAiming) => RuntimeEvents.RaiseWeaponAimChanged(itemId, isAiming);
        public static void RaiseProjectileHit(string itemId, Vector3 point, float damage) => RuntimeEvents.RaiseProjectileHit(itemId, point, damage);
        public static void RaiseShopTradeOpenRequested(string vendorId) => RuntimeEvents.RaiseShopTradeOpenRequested(vendorId);
        public static void RaiseShopTradeOpened(string vendorId) => RuntimeEvents.RaiseShopTradeOpened(vendorId);
        public static void RaiseShopTradeClosed() => RuntimeEvents.RaiseShopTradeClosed();
        public static void RaiseShopBuyRequested(string itemId, int quantity) => RuntimeEvents.RaiseShopBuyRequested(itemId, quantity);
        public static void RaiseShopSellRequested(string itemId, int quantity) => RuntimeEvents.RaiseShopSellRequested(itemId, quantity);
        public static void RaiseShopBuyCheckoutRequested(ShopCheckoutRequest request) => RuntimeEvents.RaiseShopBuyCheckoutRequested(request);
        public static void RaiseShopSellCheckoutRequested(ShopCheckoutRequest request) => RuntimeEvents.RaiseShopSellCheckoutRequested(request);
        public static void RaiseShopTradeResult(string itemId, int quantity, bool isBuy, bool success, string failureReason)
            => RuntimeEvents.RaiseShopTradeResult(itemId, quantity, isBuy, success, failureReason);
        public static void RaiseWorkbenchMenuVisibilityChanged(bool isVisible) => RuntimeEvents.RaiseWorkbenchMenuVisibilityChanged(isVisible);
        public static void RaiseTabInventoryVisibilityChanged(bool isVisible) => RuntimeEvents.RaiseTabInventoryVisibilityChanged(isVisible);
        public static void RaiseMoneyChanged(int amount) => RuntimeEvents.RaiseMoneyChanged(amount);

        private static IGameEventsRuntimeHub RuntimeEvents
        {
            get => RuntimeKernelBootstrapper.Events;
        }
    }
}
