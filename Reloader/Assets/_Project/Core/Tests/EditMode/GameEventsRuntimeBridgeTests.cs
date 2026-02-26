using System;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.Core.Tests.EditMode
{
    public class GameEventsRuntimeBridgeTests
    {
        [SetUp]
        public void SetUp()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), new DefaultRuntimeEvents());
        }

        [Test]
        public void RaiseItemStored_ForwardsToRuntimeEventHub()
        {
            var hub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), hub);

            string receivedItemId = null;
            var receivedArea = InventoryArea.Belt;
            var receivedIndex = -1;

            void Handler(string itemId, InventoryArea area, int index)
            {
                receivedItemId = itemId;
                receivedArea = area;
                receivedIndex = index;
            }

            hub.OnItemStored += Handler;
            try
            {
                GameEvents.RaiseItemStored("item-bridge", InventoryArea.Backpack, 4);
            }
            finally
            {
                hub.OnItemStored -= Handler;
            }

            Assert.That(receivedItemId, Is.EqualTo("item-bridge"));
            Assert.That(receivedArea, Is.EqualTo(InventoryArea.Backpack));
            Assert.That(receivedIndex, Is.EqualTo(4));
        }

        [Test]
        public void EventSubscription_UsesCurrentRuntimeEventHub()
        {
            var hub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), hub);

            var invocationCount = 0;
            void Handler() => invocationCount++;

            GameEvents.OnInventoryChanged += Handler;
            try
            {
                hub.RaiseInventoryChanged();
            }
            finally
            {
                GameEvents.OnInventoryChanged -= Handler;
            }

            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void EventUnsubscription_RemovesFromOriginalHub_WhenHubIsReconfigured()
        {
            var initialHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var replacementHub = new DefaultRuntimeEvents();
            var invocationCount = 0;
            void Handler() => invocationCount++;

            GameEvents.OnInventoryChanged += Handler;
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);
            GameEvents.OnInventoryChanged -= Handler;

            initialHub.RaiseInventoryChanged();
            replacementHub.RaiseInventoryChanged();

            Assert.That(invocationCount, Is.EqualTo(0));
        }

        [Test]
        public void EventSubscription_StaticSubscribers_RebindToReplacementHub_WhenConfigureSwapsHub()
        {
            var initialHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var replacementHub = new DefaultRuntimeEvents();
            var invocationCount = 0;
            void Handler() => invocationCount++;

            GameEvents.OnInventoryChanged += Handler;
            try
            {
                RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);
                replacementHub.RaiseInventoryChanged();
            }
            finally
            {
                GameEvents.OnInventoryChanged -= Handler;
            }

            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void EventSubscription_StaticSubscribers_RebindToReplacementHub_WhenEventsPropertySwapsHub()
        {
            var initialHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var replacementHub = new DefaultRuntimeEvents();
            var invocationCount = 0;
            void Handler() => invocationCount++;

            GameEvents.OnInventoryChanged += Handler;
            try
            {
                RuntimeKernelBootstrapper.Events = replacementHub;
                replacementHub.RaiseInventoryChanged();
            }
            finally
            {
                GameEvents.OnInventoryChanged -= Handler;
            }

            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void RaiseWeaponReloaded_ForwardsToRuntimeEventHub()
        {
            var hub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), hub);

            string receivedItemId = null;
            var receivedMagazineCount = -1;
            var receivedReserveCount = -1;

            void Handler(string itemId, int magazineCount, int reserveCount)
            {
                receivedItemId = itemId;
                receivedMagazineCount = magazineCount;
                receivedReserveCount = reserveCount;
            }

            hub.OnWeaponReloaded += Handler;
            try
            {
                GameEvents.RaiseWeaponReloaded("weapon-bridge", 5, 27);
            }
            finally
            {
                hub.OnWeaponReloaded -= Handler;
            }

            Assert.That(receivedItemId, Is.EqualTo("weapon-bridge"));
            Assert.That(receivedMagazineCount, Is.EqualTo(5));
            Assert.That(receivedReserveCount, Is.EqualTo(27));
        }

        [Test]
        public void MenuVisibilityFlags_ArePreservedThroughRuntimeHubBridge()
        {
            var hub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), hub);

            Assert.That(GameEvents.IsShopTradeMenuOpen, Is.False);
            Assert.That(GameEvents.IsWorkbenchMenuVisible, Is.False);
            Assert.That(GameEvents.IsTabInventoryVisible, Is.False);
            Assert.That(GameEvents.IsAnyMenuOpen, Is.False);

            GameEvents.RaiseShopTradeOpened("vendor-bridge");
            Assert.That(GameEvents.IsShopTradeMenuOpen, Is.True);
            Assert.That(GameEvents.IsAnyMenuOpen, Is.True);
            Assert.That(hub.IsShopTradeMenuOpen, Is.True);

            GameEvents.RaiseShopTradeClosed();
            Assert.That(GameEvents.IsShopTradeMenuOpen, Is.False);
            Assert.That(GameEvents.IsAnyMenuOpen, Is.False);
            Assert.That(hub.IsShopTradeMenuOpen, Is.False);

            GameEvents.RaiseWorkbenchMenuVisibilityChanged(true);
            Assert.That(GameEvents.IsWorkbenchMenuVisible, Is.True);
            Assert.That(GameEvents.IsAnyMenuOpen, Is.True);
            Assert.That(hub.IsWorkbenchMenuVisible, Is.True);

            GameEvents.RaiseTabInventoryVisibilityChanged(true);
            Assert.That(GameEvents.IsTabInventoryVisible, Is.True);
            Assert.That(GameEvents.IsAnyMenuOpen, Is.True);
            Assert.That(hub.IsTabInventoryVisible, Is.True);

            GameEvents.RaiseWorkbenchMenuVisibilityChanged(false);
            GameEvents.RaiseTabInventoryVisibilityChanged(false);
            Assert.That(GameEvents.IsWorkbenchMenuVisible, Is.False);
            Assert.That(GameEvents.IsTabInventoryVisible, Is.False);
            Assert.That(GameEvents.IsAnyMenuOpen, Is.False);
            Assert.That(hub.IsWorkbenchMenuVisible, Is.False);
            Assert.That(hub.IsTabInventoryVisible, Is.False);
        }
    }
}
