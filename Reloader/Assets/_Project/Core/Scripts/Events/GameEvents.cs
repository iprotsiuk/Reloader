using System;
using System.Collections.Generic;
using Reloader.Core.Runtime;
using UnityEngine;

namespace Reloader.Core.Events
{
    public static class GameEvents
    {
        static GameEvents()
        {
            RuntimeKernelBootstrapper.EventsReconfigured += HandleEventsReconfigured;
        }

        public static bool IsShopTradeMenuOpen => RuntimeEvents.IsShopTradeMenuOpen;
        public static bool IsWorkbenchMenuVisible => RuntimeEvents.IsWorkbenchMenuVisible;
        public static bool IsTabInventoryVisible => RuntimeEvents.IsTabInventoryVisible;
        public static bool IsAnyMenuOpen => RuntimeEvents.IsAnyMenuOpen;

        public static event Action OnSaveStarted
        {
            add => AddSubscription(nameof(OnSaveStarted), value, (hub, handler) => hub.OnSaveStarted += handler, (hub, handler) => hub.OnSaveStarted -= handler);
            remove => RemoveSubscription(nameof(OnSaveStarted), value, (hub, handler) => hub.OnSaveStarted -= handler);
        }

        public static event Action OnSaveCompleted
        {
            add => AddSubscription(nameof(OnSaveCompleted), value, (hub, handler) => hub.OnSaveCompleted += handler, (hub, handler) => hub.OnSaveCompleted -= handler);
            remove => RemoveSubscription(nameof(OnSaveCompleted), value, (hub, handler) => hub.OnSaveCompleted -= handler);
        }

        public static event Action OnLoadStarted
        {
            add => AddSubscription(nameof(OnLoadStarted), value, (hub, handler) => hub.OnLoadStarted += handler, (hub, handler) => hub.OnLoadStarted -= handler);
            remove => RemoveSubscription(nameof(OnLoadStarted), value, (hub, handler) => hub.OnLoadStarted -= handler);
        }

        public static event Action OnLoadCompleted
        {
            add => AddSubscription(nameof(OnLoadCompleted), value, (hub, handler) => hub.OnLoadCompleted += handler, (hub, handler) => hub.OnLoadCompleted -= handler);
            remove => RemoveSubscription(nameof(OnLoadCompleted), value, (hub, handler) => hub.OnLoadCompleted -= handler);
        }

        public static event Action<string> OnItemPickupRequested
        {
            add => AddSubscription(nameof(OnItemPickupRequested), value, (hub, handler) => hub.OnItemPickupRequested += handler, (hub, handler) => hub.OnItemPickupRequested -= handler);
            remove => RemoveSubscription(nameof(OnItemPickupRequested), value, (hub, handler) => hub.OnItemPickupRequested -= handler);
        }

        public static event Action<string, InventoryArea, int> OnItemStored
        {
            add => AddSubscription(nameof(OnItemStored), value, (hub, handler) => hub.OnItemStored += handler, (hub, handler) => hub.OnItemStored -= handler);
            remove => RemoveSubscription(nameof(OnItemStored), value, (hub, handler) => hub.OnItemStored -= handler);
        }

        public static event Action<string, PickupRejectReason> OnItemPickupRejected
        {
            add => AddSubscription(nameof(OnItemPickupRejected), value, (hub, handler) => hub.OnItemPickupRejected += handler, (hub, handler) => hub.OnItemPickupRejected -= handler);
            remove => RemoveSubscription(nameof(OnItemPickupRejected), value, (hub, handler) => hub.OnItemPickupRejected -= handler);
        }

        public static event Action<int> OnBeltSelectionChanged
        {
            add => AddSubscription(nameof(OnBeltSelectionChanged), value, (hub, handler) => hub.OnBeltSelectionChanged += handler, (hub, handler) => hub.OnBeltSelectionChanged -= handler);
            remove => RemoveSubscription(nameof(OnBeltSelectionChanged), value, (hub, handler) => hub.OnBeltSelectionChanged -= handler);
        }

        public static event Action OnInventoryChanged
        {
            add => AddSubscription(nameof(OnInventoryChanged), value, (hub, handler) => hub.OnInventoryChanged += handler, (hub, handler) => hub.OnInventoryChanged -= handler);
            remove => RemoveSubscription(nameof(OnInventoryChanged), value, (hub, handler) => hub.OnInventoryChanged -= handler);
        }

        public static event Action<string> OnWeaponEquipStarted
        {
            add => AddSubscription(nameof(OnWeaponEquipStarted), value, (hub, handler) => hub.OnWeaponEquipStarted += handler, (hub, handler) => hub.OnWeaponEquipStarted -= handler);
            remove => RemoveSubscription(nameof(OnWeaponEquipStarted), value, (hub, handler) => hub.OnWeaponEquipStarted -= handler);
        }

        public static event Action<string> OnWeaponEquipped
        {
            add => AddSubscription(nameof(OnWeaponEquipped), value, (hub, handler) => hub.OnWeaponEquipped += handler, (hub, handler) => hub.OnWeaponEquipped -= handler);
            remove => RemoveSubscription(nameof(OnWeaponEquipped), value, (hub, handler) => hub.OnWeaponEquipped -= handler);
        }

        public static event Action<string> OnWeaponUnequipStarted
        {
            add => AddSubscription(nameof(OnWeaponUnequipStarted), value, (hub, handler) => hub.OnWeaponUnequipStarted += handler, (hub, handler) => hub.OnWeaponUnequipStarted -= handler);
            remove => RemoveSubscription(nameof(OnWeaponUnequipStarted), value, (hub, handler) => hub.OnWeaponUnequipStarted -= handler);
        }

        public static event Action<string, Vector3, Vector3> OnWeaponFired
        {
            add => AddSubscription(nameof(OnWeaponFired), value, (hub, handler) => hub.OnWeaponFired += handler, (hub, handler) => hub.OnWeaponFired -= handler);
            remove => RemoveSubscription(nameof(OnWeaponFired), value, (hub, handler) => hub.OnWeaponFired -= handler);
        }

        public static event Action<string> OnWeaponReloadStarted
        {
            add => AddSubscription(nameof(OnWeaponReloadStarted), value, (hub, handler) => hub.OnWeaponReloadStarted += handler, (hub, handler) => hub.OnWeaponReloadStarted -= handler);
            remove => RemoveSubscription(nameof(OnWeaponReloadStarted), value, (hub, handler) => hub.OnWeaponReloadStarted -= handler);
        }

        public static event Action<string, WeaponReloadCancelReason> OnWeaponReloadCancelled
        {
            add => AddSubscription(nameof(OnWeaponReloadCancelled), value, (hub, handler) => hub.OnWeaponReloadCancelled += handler, (hub, handler) => hub.OnWeaponReloadCancelled -= handler);
            remove => RemoveSubscription(nameof(OnWeaponReloadCancelled), value, (hub, handler) => hub.OnWeaponReloadCancelled -= handler);
        }

        public static event Action<string, int, int> OnWeaponReloaded
        {
            add => AddSubscription(nameof(OnWeaponReloaded), value, (hub, handler) => hub.OnWeaponReloaded += handler, (hub, handler) => hub.OnWeaponReloaded -= handler);
            remove => RemoveSubscription(nameof(OnWeaponReloaded), value, (hub, handler) => hub.OnWeaponReloaded -= handler);
        }

        public static event Action<string, bool> OnWeaponAimChanged
        {
            add => AddSubscription(nameof(OnWeaponAimChanged), value, (hub, handler) => hub.OnWeaponAimChanged += handler, (hub, handler) => hub.OnWeaponAimChanged -= handler);
            remove => RemoveSubscription(nameof(OnWeaponAimChanged), value, (hub, handler) => hub.OnWeaponAimChanged -= handler);
        }

        public static event Action<string, Vector3, float> OnProjectileHit
        {
            add => AddSubscription(nameof(OnProjectileHit), value, (hub, handler) => hub.OnProjectileHit += handler, (hub, handler) => hub.OnProjectileHit -= handler);
            remove => RemoveSubscription(nameof(OnProjectileHit), value, (hub, handler) => hub.OnProjectileHit -= handler);
        }

        public static event Action<string> OnShopTradeOpenRequested
        {
            add => AddSubscription(nameof(OnShopTradeOpenRequested), value, (hub, handler) => hub.OnShopTradeOpenRequested += handler, (hub, handler) => hub.OnShopTradeOpenRequested -= handler);
            remove => RemoveSubscription(nameof(OnShopTradeOpenRequested), value, (hub, handler) => hub.OnShopTradeOpenRequested -= handler);
        }

        public static event Action<string> OnShopTradeOpened
        {
            add => AddSubscription(nameof(OnShopTradeOpened), value, (hub, handler) => hub.OnShopTradeOpened += handler, (hub, handler) => hub.OnShopTradeOpened -= handler);
            remove => RemoveSubscription(nameof(OnShopTradeOpened), value, (hub, handler) => hub.OnShopTradeOpened -= handler);
        }

        public static event Action OnShopTradeClosed
        {
            add => AddSubscription(nameof(OnShopTradeClosed), value, (hub, handler) => hub.OnShopTradeClosed += handler, (hub, handler) => hub.OnShopTradeClosed -= handler);
            remove => RemoveSubscription(nameof(OnShopTradeClosed), value, (hub, handler) => hub.OnShopTradeClosed -= handler);
        }

        public static event Action<string, int> OnShopBuyRequested
        {
            add => AddSubscription(nameof(OnShopBuyRequested), value, (hub, handler) => hub.OnShopBuyRequested += handler, (hub, handler) => hub.OnShopBuyRequested -= handler);
            remove => RemoveSubscription(nameof(OnShopBuyRequested), value, (hub, handler) => hub.OnShopBuyRequested -= handler);
        }

        public static event Action<string, int> OnShopSellRequested
        {
            add => AddSubscription(nameof(OnShopSellRequested), value, (hub, handler) => hub.OnShopSellRequested += handler, (hub, handler) => hub.OnShopSellRequested -= handler);
            remove => RemoveSubscription(nameof(OnShopSellRequested), value, (hub, handler) => hub.OnShopSellRequested -= handler);
        }

        public static event Action<ShopCheckoutRequest> OnShopBuyCheckoutRequested
        {
            add => AddSubscription(nameof(OnShopBuyCheckoutRequested), value, (hub, handler) => hub.OnShopBuyCheckoutRequested += handler, (hub, handler) => hub.OnShopBuyCheckoutRequested -= handler);
            remove => RemoveSubscription(nameof(OnShopBuyCheckoutRequested), value, (hub, handler) => hub.OnShopBuyCheckoutRequested -= handler);
        }

        public static event Action<ShopCheckoutRequest> OnShopSellCheckoutRequested
        {
            add => AddSubscription(nameof(OnShopSellCheckoutRequested), value, (hub, handler) => hub.OnShopSellCheckoutRequested += handler, (hub, handler) => hub.OnShopSellCheckoutRequested -= handler);
            remove => RemoveSubscription(nameof(OnShopSellCheckoutRequested), value, (hub, handler) => hub.OnShopSellCheckoutRequested -= handler);
        }

        public static event Action<string, int, bool, bool, string> OnShopTradeResult
        {
            add => AddSubscription(nameof(OnShopTradeResult), value, (hub, handler) => hub.OnShopTradeResult += handler, (hub, handler) => hub.OnShopTradeResult -= handler);
            remove => RemoveSubscription(nameof(OnShopTradeResult), value, (hub, handler) => hub.OnShopTradeResult -= handler);
        }

        public static event Action<bool> OnWorkbenchMenuVisibilityChanged
        {
            add => AddSubscription(nameof(OnWorkbenchMenuVisibilityChanged), value, (hub, handler) => hub.OnWorkbenchMenuVisibilityChanged += handler, (hub, handler) => hub.OnWorkbenchMenuVisibilityChanged -= handler);
            remove => RemoveSubscription(nameof(OnWorkbenchMenuVisibilityChanged), value, (hub, handler) => hub.OnWorkbenchMenuVisibilityChanged -= handler);
        }

        public static event Action<bool> OnTabInventoryVisibilityChanged
        {
            add => AddSubscription(nameof(OnTabInventoryVisibilityChanged), value, (hub, handler) => hub.OnTabInventoryVisibilityChanged += handler, (hub, handler) => hub.OnTabInventoryVisibilityChanged -= handler);
            remove => RemoveSubscription(nameof(OnTabInventoryVisibilityChanged), value, (hub, handler) => hub.OnTabInventoryVisibilityChanged -= handler);
        }

        public static event Action<int> OnMoneyChanged
        {
            add => AddSubscription(nameof(OnMoneyChanged), value, (hub, handler) => hub.OnMoneyChanged += handler, (hub, handler) => hub.OnMoneyChanged -= handler);
            remove => RemoveSubscription(nameof(OnMoneyChanged), value, (hub, handler) => hub.OnMoneyChanged -= handler);
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

        private static readonly Dictionary<SubscriptionKey, SubscriptionState> SubscriptionStates =
            new Dictionary<SubscriptionKey, SubscriptionState>();

        private static void AddSubscription<THandler>(
            string eventName,
            THandler handler,
            Action<IGameEventsRuntimeHub, THandler> subscribe,
            Action<IGameEventsRuntimeHub, THandler> unsubscribe) where THandler : Delegate
        {
            if (handler == null)
            {
                return;
            }

            var hub = RuntimeEvents;
            var key = new SubscriptionKey(eventName, handler);
            if (!SubscriptionStates.TryGetValue(key, out var state))
            {
                state = new SubscriptionState(
                    handler,
                    (targetHub, storedHandler) => subscribe(targetHub, (THandler)storedHandler),
                    (targetHub, storedHandler) => unsubscribe(targetHub, (THandler)storedHandler));
                SubscriptionStates.Add(key, state);
            }

            state.Subscribe(hub);
        }

        private static void RemoveSubscription<THandler>(
            string eventName,
            THandler handler,
            Action<IGameEventsRuntimeHub, THandler> unsubscribe) where THandler : Delegate
        {
            if (handler == null)
            {
                return;
            }

            var key = new SubscriptionKey(eventName, handler);
            if (SubscriptionStates.TryGetValue(key, out var state) && state.Count > 0)
            {
                state.UnsubscribeLatest();
                if (state.Count == 0)
                {
                    SubscriptionStates.Remove(key);
                }

                return;
            }

            unsubscribe(RuntimeEvents, handler);
        }

        private static void HandleEventsReconfigured()
        {
            var replacementHub = RuntimeEvents;
            foreach (var state in SubscriptionStates.Values)
            {
                state.Rebind(replacementHub);
            }
        }

        private sealed class SubscriptionState
        {
            private readonly Delegate handler;
            private readonly Action<IGameEventsRuntimeHub, Delegate> subscribe;
            private readonly Action<IGameEventsRuntimeHub, Delegate> unsubscribe;
            private readonly Stack<IGameEventsRuntimeHub> hubs = new Stack<IGameEventsRuntimeHub>();

            public SubscriptionState(
                Delegate handler,
                Action<IGameEventsRuntimeHub, Delegate> subscribe,
                Action<IGameEventsRuntimeHub, Delegate> unsubscribe)
            {
                this.handler = handler;
                this.subscribe = subscribe;
                this.unsubscribe = unsubscribe;
            }

            public int Count => hubs.Count;

            public void Subscribe(IGameEventsRuntimeHub hub)
            {
                subscribe(hub, handler);
                hubs.Push(hub);
            }

            public void UnsubscribeLatest()
            {
                if (hubs.Count == 0)
                {
                    return;
                }

                var hub = hubs.Pop();
                unsubscribe(hub, handler);
            }

            public void Rebind(IGameEventsRuntimeHub replacementHub)
            {
                if (hubs.Count == 0)
                {
                    return;
                }

                var subscriptionCount = hubs.Count;
                while (hubs.Count > 0)
                {
                    var hub = hubs.Pop();
                    unsubscribe(hub, handler);
                }

                for (var i = 0; i < subscriptionCount; i++)
                {
                    subscribe(replacementHub, handler);
                    hubs.Push(replacementHub);
                }
            }
        }

        private readonly struct SubscriptionKey : IEquatable<SubscriptionKey>
        {
            public SubscriptionKey(string eventName, Delegate handler)
            {
                EventName = eventName ?? string.Empty;
                Handler = handler;
            }

            private string EventName { get; }
            private Delegate Handler { get; }

            public bool Equals(SubscriptionKey other)
            {
                return string.Equals(EventName, other.EventName, StringComparison.Ordinal)
                    && Equals(Handler, other.Handler);
            }

            public override bool Equals(object obj)
            {
                return obj is SubscriptionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (EventName.GetHashCode() * 397) ^ (Handler != null ? Handler.GetHashCode() : 0);
                }
            }
        }
    }
}
