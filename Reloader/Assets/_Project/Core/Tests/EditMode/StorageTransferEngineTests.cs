using NUnit.Framework;
using Reloader.Inventory;

namespace Reloader.Core.Tests.EditMode
{
    public class StorageTransferEngineTests
    {
        [Test]
        public void Move_PlayerBackpack_ToChestSlot_MovesItem()
        {
            var player = new PlayerInventoryRuntime();
            player.SetBackpackCapacity(4);
            player.BackpackItemIds.Add("powder-a");

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
            Assert.That(chest.GetSlotItemId(0), Is.EqualTo("powder-a"));
        }
    }
}
