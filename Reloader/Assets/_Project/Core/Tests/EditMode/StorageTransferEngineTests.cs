using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Inventory;

namespace Reloader.Core.Tests.EditMode
{
    public class StorageTransferEngineTests
    {
        [Test]
        public void Move_PlayerBackpackStack_ToChestSlot_PreservesQuantityAndMaxStack()
        {
            var player = new PlayerInventoryRuntime();
            player.SetBackpackCapacity(4);
            player.SetItemMaxStack("ammo-308", 999);
            player.TryAddStackItem("ammo-308", 50, out _, out _, out _);
            player.TryMoveItem(InventoryArea.Belt, 0, InventoryArea.Backpack, 0);

            var chest = new StorageContainerRuntime("chest.mainTown.workbench.001", 20, StorageContainerPolicy.Persistent);
            var registry = new StorageContainerRegistry();
            registry.Upsert(chest);

            var moved = StorageTransferEngine.TryMove(
                player,
                registry,
                "backpack", 0,
                "container:chest.mainTown.workbench.001", 0);

            Assert.That(moved, Is.True);
            Assert.That(player.BackpackItemIds.Count, Is.EqualTo(0));
            Assert.That(chest.TryGetSlotStack(0, out var stack), Is.True);
            Assert.That(stack, Is.Not.Null);
            Assert.That(stack!.ItemId, Is.EqualTo("ammo-308"));
            Assert.That(stack.Quantity, Is.EqualTo(50));
            Assert.That(stack.MaxStack, Is.EqualTo(999));
        }

        [Test]
        public void Move_ChestStack_ToPlayerBackpack_PreservesQuantity()
        {
            var player = new PlayerInventoryRuntime();
            player.SetBackpackCapacity(4);

            var chest = new StorageContainerRuntime("chest.mainTown.workbench.001", 20, StorageContainerPolicy.Persistent);
            chest.TrySetSlotStack(0, new ItemStackState("ammo-308", 50, 999));

            var registry = new StorageContainerRegistry();
            registry.Upsert(chest);

            var moved = StorageTransferEngine.TryMove(
                player,
                registry,
                "container:chest.mainTown.workbench.001", 0,
                "backpack", 0);

            Assert.That(moved, Is.True);
            Assert.That(chest.GetSlotItemId(0), Is.Null);
            Assert.That(player.BackpackItemIds.Count, Is.EqualTo(1));
            Assert.That(player.BackpackItemIds[0], Is.EqualTo("ammo-308"));
            Assert.That(player.GetSlotQuantity(InventoryArea.Backpack, 0), Is.EqualTo(50));
        }
    }
}
