using System;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.Core.Tests.EditMode
{
    public class RuntimeEventHubBehaviorTests
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
        public void RaiseItemStored_TypedPortInvokesConfiguredRuntimeHub()
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
                RuntimeKernelBootstrapper.InventoryEvents.RaiseItemStored("item-bridge", InventoryArea.Backpack, 4);
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

            RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged += Handler;
            try
            {
                hub.RaiseInventoryChanged();
            }
            finally
            {
                RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged -= Handler;
            }

            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void RuntimePorts_PointToReplacementHub_WhenConfigureSwapsHub()
        {
            var initialHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var replacementHub = new DefaultRuntimeEvents();
            var initialHubCount = 0;
            var replacementHubCount = 0;
            void InitialHandler() => initialHubCount++;
            void ReplacementHandler() => replacementHubCount++;

            RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged += InitialHandler;
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), replacementHub);
            try
            {
                RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged += ReplacementHandler;
                initialHub.RaiseInventoryChanged();
                replacementHub.RaiseInventoryChanged();
            }
            finally
            {
                initialHub.OnInventoryChanged -= InitialHandler;
                RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged -= ReplacementHandler;
            }

            Assert.That(initialHubCount, Is.EqualTo(1));
            Assert.That(replacementHubCount, Is.EqualTo(1));
        }

        [Test]
        public void RuntimePorts_PointToReplacementHub_WhenEventsPropertySwapsHub()
        {
            var initialHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), initialHub);

            var replacementHub = new DefaultRuntimeEvents();
            var initialHubCount = 0;
            var replacementHubCount = 0;
            void InitialHandler() => initialHubCount++;
            void ReplacementHandler() => replacementHubCount++;

            RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged += InitialHandler;
            try
            {
                RuntimeKernelBootstrapper.Events = replacementHub;
                RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged += ReplacementHandler;
                initialHub.RaiseInventoryChanged();
                replacementHub.RaiseInventoryChanged();
            }
            finally
            {
                initialHub.OnInventoryChanged -= InitialHandler;
                RuntimeKernelBootstrapper.InventoryEvents.OnInventoryChanged -= ReplacementHandler;
            }

            Assert.That(initialHubCount, Is.EqualTo(1));
            Assert.That(replacementHubCount, Is.EqualTo(1));
        }

        [Test]
        public void RaiseWeaponReloaded_TypedPortInvokesConfiguredRuntimeHub()
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
                RuntimeKernelBootstrapper.WeaponEvents.RaiseWeaponReloaded("weapon-bridge", 5, 27);
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
        public void MenuVisibilityFlags_ArePreservedOnRuntimeHub()
        {
            var hub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Configure(Array.Empty<RuntimeModuleRegistration>(), hub);

            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsShopTradeMenuOpen, Is.False);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsWorkbenchMenuVisible, Is.False);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsTabInventoryVisible, Is.False);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsAnyMenuOpen, Is.False);

            RuntimeKernelBootstrapper.ShopEvents.RaiseShopTradeOpened("vendor-bridge");
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsShopTradeMenuOpen, Is.True);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsAnyMenuOpen, Is.True);
            Assert.That(hub.IsShopTradeMenuOpen, Is.True);

            RuntimeKernelBootstrapper.ShopEvents.RaiseShopTradeClosed();
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsShopTradeMenuOpen, Is.False);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsAnyMenuOpen, Is.False);
            Assert.That(hub.IsShopTradeMenuOpen, Is.False);

            RuntimeKernelBootstrapper.UiStateEvents.RaiseWorkbenchMenuVisibilityChanged(true);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsWorkbenchMenuVisible, Is.True);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsAnyMenuOpen, Is.True);
            Assert.That(hub.IsWorkbenchMenuVisible, Is.True);

            RuntimeKernelBootstrapper.UiStateEvents.RaiseTabInventoryVisibilityChanged(true);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsTabInventoryVisible, Is.True);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsAnyMenuOpen, Is.True);
            Assert.That(hub.IsTabInventoryVisible, Is.True);

            RuntimeKernelBootstrapper.UiStateEvents.RaiseWorkbenchMenuVisibilityChanged(false);
            RuntimeKernelBootstrapper.UiStateEvents.RaiseTabInventoryVisibilityChanged(false);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsWorkbenchMenuVisible, Is.False);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsTabInventoryVisible, Is.False);
            Assert.That(RuntimeKernelBootstrapper.UiStateEvents.IsAnyMenuOpen, Is.False);
            Assert.That(hub.IsWorkbenchMenuVisible, Is.False);
            Assert.That(hub.IsTabInventoryVisible, Is.False);
        }
    }
}
