using NUnit.Framework;
using Reloader.Core.Items;
using Reloader.Inventory;
using Reloader.PlayerDevice.Runtime;
using Reloader.PlayerDevice.World;
using UnityEngine;

namespace Reloader.PlayerDevice.Tests.EditMode
{
    public class PlayerDeviceAttachmentInstallEditModeTests
    {
        private const string RangefinderItemId = "attachment.rangefinder";
        private const string FillerItemIdPrefix = "filler.";

        [Test]
        public void TryInstallSelectedAttachmentFromInventory_ConsumesSelectedItem_AndInstallsAttachment()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(9);
            runtime.TryStoreItem(RangefinderItemId, out _, out var beltIndex, out _);
            runtime.SelectBeltSlot(beltIndex);

            var inventoryOwner = new GameObject("InventoryOwner");
            try
            {
                var inventoryController = inventoryOwner.AddComponent<PlayerInventoryController>();
                inventoryController.Configure(null, null, runtime, null);

                var deviceState = new PlayerDeviceRuntimeState();
                var catalog = BuildCatalog();
                var controller = new PlayerDeviceController(deviceState, inventoryController, catalog);

                var installed = controller.TryInstallSelectedAttachmentFromInventory();

                Assert.That(installed, Is.True);
                Assert.That(deviceState.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.True);
                Assert.That(runtime.GetItemQuantity(RangefinderItemId), Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(inventoryOwner);
            }
        }

        [Test]
        public void TryUninstallAttachment_ReturnsItemToBelt_WhenBeltHasSpace()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(9);
            runtime.TryStoreItem(FillerItemIdPrefix + "1", out _, out _, out _);

            var inventoryOwner = new GameObject("InventoryOwner");
            try
            {
                var inventoryController = inventoryOwner.AddComponent<PlayerInventoryController>();
                inventoryController.Configure(null, null, runtime, null);
                runtime.SetBackpackCapacity(1);

                var deviceState = new PlayerDeviceRuntimeState();
                deviceState.InstallAttachment(DeviceAttachmentType.Rangefinder);
                var catalog = BuildCatalog();
                var controller = new PlayerDeviceController(deviceState, inventoryController, catalog);

                var uninstalled = controller.TryUninstallAttachment(DeviceAttachmentType.Rangefinder);

                Assert.That(uninstalled, Is.True);
                Assert.That(deviceState.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.False);
                Assert.That(runtime.GetItemQuantity(RangefinderItemId), Is.EqualTo(1));
                Assert.That(runtime.BeltSlotItemIds, Has.Member(RangefinderItemId));
            }
            finally
            {
                Object.DestroyImmediate(inventoryOwner);
            }
        }

        [Test]
        public void TryUninstallAttachment_ReturnsItemToBackpack_WhenBeltIsFull()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(9);
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                runtime.TryStoreItem(FillerItemIdPrefix + i, out _, out _, out _);
            }

            var inventoryOwner = new GameObject("InventoryOwner");
            try
            {
                var inventoryController = inventoryOwner.AddComponent<PlayerInventoryController>();
                inventoryController.Configure(null, null, runtime, null);

                var deviceState = new PlayerDeviceRuntimeState();
                deviceState.InstallAttachment(DeviceAttachmentType.Rangefinder);
                var catalog = BuildCatalog();
                var controller = new PlayerDeviceController(deviceState, inventoryController, catalog);

                var uninstalled = controller.TryUninstallAttachment(DeviceAttachmentType.Rangefinder);

                Assert.That(uninstalled, Is.True);
                Assert.That(deviceState.IsAttachmentInstalled(DeviceAttachmentType.Rangefinder), Is.False);
                Assert.That(runtime.BeltSlotItemIds, Has.None.EqualTo(RangefinderItemId));
                Assert.That(runtime.BackpackItemIds, Has.Member(RangefinderItemId));
            }
            finally
            {
                Object.DestroyImmediate(inventoryOwner);
            }
        }

        [Test]
        public void CanUninstallAttachment_IsFalse_WhenInventoryHasNoCapacity()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                runtime.TryStoreItem(FillerItemIdPrefix + i, out _, out _, out _);
            }

            var inventoryOwner = new GameObject("InventoryOwner");
            try
            {
                var inventoryController = inventoryOwner.AddComponent<PlayerInventoryController>();
                inventoryController.Configure(null, null, runtime, null);
                runtime.SetBackpackCapacity(0);

                var deviceState = new PlayerDeviceRuntimeState();
                deviceState.InstallAttachment(DeviceAttachmentType.Rangefinder);
                var catalog = BuildCatalog();
                var controller = new PlayerDeviceController(deviceState, inventoryController, catalog);

                Assert.That(controller.CanUninstallAttachment(DeviceAttachmentType.Rangefinder), Is.False);
                Assert.That(controller.TryUninstallAttachment(DeviceAttachmentType.Rangefinder), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(inventoryOwner);
            }
        }

        private static DeviceAttachmentCatalog BuildCatalog()
        {
            var rangefinderItem = ScriptableObject.CreateInstance<ItemDefinition>();
            rangefinderItem.SetValuesForTests(
                RangefinderItemId,
                ItemCategory.Misc,
                "Rangefinder",
                ItemStackPolicy.NonStackable,
                1);

            return DeviceAttachmentCatalog.FromDefinitions(new[]
            {
                new DeviceAttachmentCatalog.Entry(rangefinderItem, DeviceAttachmentType.Rangefinder),
            });
        }
    }
}
