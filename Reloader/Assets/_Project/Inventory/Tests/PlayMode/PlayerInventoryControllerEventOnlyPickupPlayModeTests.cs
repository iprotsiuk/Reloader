using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using System.Reflection;
using UnityEngine;

namespace Reloader.Inventory.Tests.PlayMode
{
    public class PlayerInventoryControllerEventOnlyPickupPlayModeTests
    {
        [Test]
        public void RaiseItemPickupRequested_WithoutPendingWorldTarget_StoresItemInRuntimeInventory()
        {
            var root = new GameObject("InventoryControllerRoot");
            var controller = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            var events = new FakeInventoryEvents();
            controller.Configure(null, null, runtime, events);

            events.RaiseItemPickupRequested("item-event-only");

            Assert.That(runtime.GetItemQuantity("item-event-only"), Is.EqualTo(1));
            Assert.That(events.ItemStoredCount, Is.EqualTo(1));
            Assert.That(events.InventoryChangedCount, Is.EqualTo(1));
            Assert.That(events.ItemPickupRejectedCount, Is.EqualTo(0));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void OnEnable_WhenRuntimeWasCleared_ReinitializesRuntime()
        {
            var root = new GameObject("InventoryControllerReloadRecoveryRoot");
            var controller = root.AddComponent<PlayerInventoryController>();

            controller.enabled = false;
            SetRuntimeForTests(controller, null);
            controller.enabled = true;

            Assert.That(controller.Runtime, Is.Not.Null);
            Assert.That(controller.Runtime.BackpackCapacity, Is.EqualTo(9));
            Assert.That(controller.TryStoreItemWithBeltPriority("item-after-reenable"), Is.True);

            Object.DestroyImmediate(root);
        }

        private static void SetRuntimeForTests(PlayerInventoryController controller, PlayerInventoryRuntime runtime)
        {
            var field = typeof(PlayerInventoryController).GetField(
                "<Runtime>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(controller, runtime);
        }

        private sealed class FakeInventoryEvents : IInventoryEvents
        {
            public int ItemStoredCount { get; private set; }
            public int InventoryChangedCount { get; private set; }
            public int ItemPickupRejectedCount { get; private set; }

            public event System.Action OnSaveStarted;
            public event System.Action OnSaveCompleted;
            public event System.Action OnLoadStarted;
            public event System.Action OnLoadCompleted;
            public event System.Action<string> OnItemPickupRequested;
            public event System.Action<string, InventoryArea, int> OnItemStored;
            public event System.Action<string, PickupRejectReason> OnItemPickupRejected;
            public event System.Action<int> OnBeltSelectionChanged;
            public event System.Action OnInventoryChanged;
            public event System.Action<int> OnMoneyChanged;

            public void RaiseSaveStarted() => OnSaveStarted?.Invoke();
            public void RaiseSaveCompleted() => OnSaveCompleted?.Invoke();
            public void RaiseLoadStarted() => OnLoadStarted?.Invoke();
            public void RaiseLoadCompleted() => OnLoadCompleted?.Invoke();
            public void RaiseItemPickupRequested(string itemId) => OnItemPickupRequested?.Invoke(itemId);

            public void RaiseItemStored(string itemId, InventoryArea area, int index)
            {
                ItemStoredCount++;
                OnItemStored?.Invoke(itemId, area, index);
            }

            public void RaiseItemPickupRejected(string itemId, PickupRejectReason reason)
            {
                ItemPickupRejectedCount++;
                OnItemPickupRejected?.Invoke(itemId, reason);
            }

            public void RaiseBeltSelectionChanged(int selectedBeltIndex) => OnBeltSelectionChanged?.Invoke(selectedBeltIndex);

            public void RaiseInventoryChanged()
            {
                InventoryChangedCount++;
                OnInventoryChanged?.Invoke();
            }

            public void RaiseMoneyChanged(int amount) => OnMoneyChanged?.Invoke(amount);
        }
    }
}
