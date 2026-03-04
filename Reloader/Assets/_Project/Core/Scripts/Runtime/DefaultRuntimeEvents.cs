using System;
using Reloader.Core.Events;
using UnityEngine;

namespace Reloader.Core.Runtime
{
    public sealed class DefaultRuntimeEvents : IGameEventsRuntimeHub
    {
        public IInventoryEvents InventoryEvents => this;
        public IWeaponEvents WeaponEvents => this;
        public IShopEvents ShopEvents => this;
        public IUiStateEvents UiStateEvents => this;
        public IInteractionHintEvents InteractionHintEvents => this;

        public bool IsShopTradeMenuOpen { get; private set; }
        public bool IsWorkbenchMenuVisible { get; private set; }
        public bool IsTabInventoryVisible { get; private set; }
        public bool IsEscMenuVisible { get; private set; }
        public bool IsAnyMenuOpen => IsShopTradeMenuOpen || IsWorkbenchMenuVisible || IsTabInventoryVisible || IsEscMenuVisible;
        public bool HasInteractionHint { get; private set; }
        public InteractionHintPayload CurrentInteractionHint { get; private set; }

        public event Action OnSaveStarted;
        public event Action OnSaveCompleted;
        public event Action OnLoadStarted;
        public event Action OnLoadCompleted;
        public event Action<string> OnItemPickupRequested;
        public event Action<string, InventoryArea, int> OnItemStored;
        public event Action<string, PickupRejectReason> OnItemPickupRejected;
        public event Action<int> OnBeltSelectionChanged;
        public event Action OnInventoryChanged;
        public event Action<string> OnWeaponEquipStarted;
        public event Action<string> OnWeaponEquipped;
        public event Action<string> OnWeaponUnequipStarted;
        public event Action<string, Vector3, Vector3> OnWeaponFired;
        public event Action<string> OnWeaponReloadStarted;
        public event Action<string, WeaponReloadCancelReason> OnWeaponReloadCancelled;
        public event Action<string, int, int> OnWeaponReloaded;
        public event Action<string, bool> OnWeaponAimChanged;
        public event Action<string, Vector3, float> OnProjectileHit;
        public event Action<string> OnShopTradeOpenRequested;
        public event Action<string> OnShopTradeOpened;
        public event Action OnShopTradeClosed;
        public event Action<string, int> OnShopBuyRequested;
        public event Action<string, int> OnShopSellRequested;
        public event Action<ShopCheckoutRequest> OnShopBuyCheckoutRequested;
        public event Action<ShopCheckoutRequest> OnShopSellCheckoutRequested;
        public event Action<ShopTradeResultPayload> OnShopTradeResultReceived;

        [Obsolete("Use OnShopTradeResultReceived with ShopTradeResultPayload.")]
        public event Action<string, int, bool, bool, string> OnShopTradeResult;
        public event Action<bool> OnWorkbenchMenuVisibilityChanged;
        public event Action<bool> OnTabInventoryVisibilityChanged;
        public event Action<bool> OnEscMenuVisibilityChanged;
        public event Action<int> OnMoneyChanged;
        public event Action<InteractionHintPayload> OnInteractionHintShown;
        public event Action OnInteractionHintCleared;

        public void RaiseSaveStarted() => OnSaveStarted?.Invoke();
        public void RaiseSaveCompleted() => OnSaveCompleted?.Invoke();
        public void RaiseLoadStarted() => OnLoadStarted?.Invoke();
        public void RaiseLoadCompleted() => OnLoadCompleted?.Invoke();
        public void RaiseItemPickupRequested(string itemId) => OnItemPickupRequested?.Invoke(itemId);
        public void RaiseItemStored(string itemId, InventoryArea area, int index) => OnItemStored?.Invoke(itemId, area, index);
        public void RaiseItemPickupRejected(string itemId, PickupRejectReason reason) => OnItemPickupRejected?.Invoke(itemId, reason);
        public void RaiseBeltSelectionChanged(int selectedBeltIndex) => OnBeltSelectionChanged?.Invoke(selectedBeltIndex);
        public void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();
        public void RaiseWeaponEquipStarted(string itemId) => OnWeaponEquipStarted?.Invoke(itemId);
        public void RaiseWeaponEquipped(string itemId) => OnWeaponEquipped?.Invoke(itemId);
        public void RaiseWeaponUnequipStarted(string itemId) => OnWeaponUnequipStarted?.Invoke(itemId);
        public void RaiseWeaponFired(string itemId, Vector3 origin, Vector3 direction) => OnWeaponFired?.Invoke(itemId, origin, direction);
        public void RaiseWeaponReloadStarted(string itemId) => OnWeaponReloadStarted?.Invoke(itemId);
        public void RaiseWeaponReloadCancelled(string itemId, WeaponReloadCancelReason reason) => OnWeaponReloadCancelled?.Invoke(itemId, reason);
        public void RaiseWeaponReloaded(string itemId, int magazineCount, int reserveCount) => OnWeaponReloaded?.Invoke(itemId, magazineCount, reserveCount);
        public void RaiseWeaponAimChanged(string itemId, bool isAiming) => OnWeaponAimChanged?.Invoke(itemId, isAiming);
        public void RaiseProjectileHit(string itemId, Vector3 point, float damage) => OnProjectileHit?.Invoke(itemId, point, damage);
        public void RaiseShopTradeOpenRequested(string vendorId) => OnShopTradeOpenRequested?.Invoke(vendorId);

        public void RaiseShopTradeOpened(string vendorId)
        {
            IsShopTradeMenuOpen = true;
            OnShopTradeOpened?.Invoke(vendorId);
        }

        public void RaiseShopTradeClosed()
        {
            IsShopTradeMenuOpen = false;
            OnShopTradeClosed?.Invoke();
        }

        public void RaiseShopBuyRequested(string itemId, int quantity) => OnShopBuyRequested?.Invoke(itemId, quantity);
        public void RaiseShopSellRequested(string itemId, int quantity) => OnShopSellRequested?.Invoke(itemId, quantity);
        public void RaiseShopBuyCheckoutRequested(ShopCheckoutRequest request) => OnShopBuyCheckoutRequested?.Invoke(request);
        public void RaiseShopSellCheckoutRequested(ShopCheckoutRequest request) => OnShopSellCheckoutRequested?.Invoke(request);

        public void RaiseShopTradeResult(ShopTradeResultPayload payload)
        {
            OnShopTradeResultReceived?.Invoke(payload);
#pragma warning disable CS0618
            OnShopTradeResult?.Invoke(
                payload.ItemId,
                payload.Quantity,
                payload.IsBuy,
                payload.Success,
                payload.Success ? string.Empty : payload.FailureReason.ToString());
#pragma warning restore CS0618
        }

        [Obsolete("Use RaiseShopTradeResult(ShopTradeResultPayload payload).")]
        public void RaiseShopTradeResult(string itemId, int quantity, bool isBuy, bool success, string failureReason)
        {
            RaiseShopTradeResult(new ShopTradeResultPayload(
                itemId,
                quantity,
                isBuy,
                success,
                ShopTradeResultPayload.ParseLegacyFailureReason(failureReason, success)));
        }

        public void RaiseWorkbenchMenuVisibilityChanged(bool isVisible)
        {
            IsWorkbenchMenuVisible = isVisible;
            OnWorkbenchMenuVisibilityChanged?.Invoke(isVisible);
        }

        public void RaiseTabInventoryVisibilityChanged(bool isVisible)
        {
            IsTabInventoryVisible = isVisible;
            OnTabInventoryVisibilityChanged?.Invoke(isVisible);
        }

        public void RaiseEscMenuVisibilityChanged(bool isVisible)
        {
            IsEscMenuVisible = isVisible;
            OnEscMenuVisibilityChanged?.Invoke(isVisible);
        }

        public void RaiseMoneyChanged(int amount) => OnMoneyChanged?.Invoke(amount);

        public void RaiseInteractionHintShown(InteractionHintPayload payload)
        {
            CurrentInteractionHint = payload;
            HasInteractionHint = true;
            OnInteractionHintShown?.Invoke(payload);
        }

        public void RaiseInteractionHintCleared(string contextId = null)
        {
            if (!string.IsNullOrWhiteSpace(contextId)
                && !string.Equals(CurrentInteractionHint.ContextId, contextId, StringComparison.Ordinal))
            {
                return;
            }

            CurrentInteractionHint = new InteractionHintPayload(string.Empty, string.Empty, string.Empty);
            HasInteractionHint = false;
            OnInteractionHintCleared?.Invoke();
        }
    }
}
