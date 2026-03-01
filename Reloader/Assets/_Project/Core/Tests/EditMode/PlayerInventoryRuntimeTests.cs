using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Inventory;

namespace Reloader.Core.Tests.EditMode
{
    public class PlayerInventoryRuntimeTests
    {
        [Test]
        public void TryStoreItem_FillsFirstEmptyBeltSlot()
        {
            var runtime = new PlayerInventoryRuntime();

            var stored = runtime.TryStoreItem("item-1", out var area, out var index, out var rejectReason);

            Assert.That(stored, Is.True);
            Assert.That(area, Is.EqualTo(InventoryArea.Belt));
            Assert.That(index, Is.EqualTo(0));
            Assert.That(runtime.BeltSlotItemIds[0], Is.EqualTo("item-1"));
            Assert.That(rejectReason, Is.EqualTo(PickupRejectReason.NoSpace));
        }

        [Test]
        public void TryStoreItem_WhenNoSpaceAndBackpackLocked_ReturnsNoSpace()
        {
            var runtime = new PlayerInventoryRuntime();
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                runtime.BeltSlotItemIds[i] = "filled-" + i;
            }

            var stored = runtime.TryStoreItem("item-overflow", out var area, out var index, out var rejectReason);

            Assert.That(stored, Is.False);
            Assert.That(area, Is.EqualTo(InventoryArea.Belt));
            Assert.That(index, Is.EqualTo(-1));
            Assert.That(rejectReason, Is.EqualTo(PickupRejectReason.NoSpace));
        }

        [Test]
        public void SelectBeltSlot_AllowsSelectingEmptySlot()
        {
            var runtime = new PlayerInventoryRuntime();

            runtime.SelectBeltSlot(2);

            Assert.That(runtime.SelectedBeltIndex, Is.EqualTo(2));
        }

        [Test]
        public void SelectBeltSlot_SameIndex_IsNoOp()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SelectBeltSlot(4);

            runtime.SelectBeltSlot(4);

            Assert.That(runtime.SelectedBeltIndex, Is.EqualTo(4));
        }

        [Test]
        public void TryStoreItem_FillingSelectedEmptySlot_MakesSelectedItemAvailable()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.BeltSlotItemIds[0] = "existing-0";
            runtime.BeltSlotItemIds[1] = "existing-1";
            runtime.SelectBeltSlot(2);

            var stored = runtime.TryStoreItem("new-item", out _, out _, out _);

            Assert.That(stored, Is.True);
            Assert.That(runtime.SelectedBeltIndex, Is.EqualTo(2));
            Assert.That(runtime.SelectedBeltItemId, Is.EqualTo("new-item"));
        }

        [Test]
        public void TryMoveItem_BeltToBelt_SwapsItems()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.BeltSlotItemIds[0] = "belt-a";
            runtime.BeltSlotItemIds[2] = "belt-b";

            var moved = runtime.TryMoveItem(InventoryArea.Belt, 0, InventoryArea.Belt, 2);

            Assert.That(moved, Is.True);
            Assert.That(runtime.BeltSlotItemIds[0], Is.EqualTo("belt-b"));
            Assert.That(runtime.BeltSlotItemIds[2], Is.EqualTo("belt-a"));
        }

        [Test]
        public void TryMoveItem_BeltToBackpackAppends_WhenTargetIsNextFreeIndex()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            runtime.BeltSlotItemIds[0] = "belt-item";
            runtime.BackpackItemIds.Add("pack-1");

            var moved = runtime.TryMoveItem(InventoryArea.Belt, 0, InventoryArea.Backpack, 1);

            Assert.That(moved, Is.True);
            Assert.That(runtime.BeltSlotItemIds[0], Is.Null);
            Assert.That(runtime.BackpackItemIds, Is.EqualTo(new[] { "pack-1", "belt-item" }));
        }

        [Test]
        public void TryMoveItem_BackpackToBeltSwaps_WhenBeltSlotOccupied()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);
            runtime.BackpackItemIds.Add("pack-item");
            runtime.BeltSlotItemIds[3] = "belt-item";

            var moved = runtime.TryMoveItem(InventoryArea.Backpack, 0, InventoryArea.Belt, 3);

            Assert.That(moved, Is.True);
            Assert.That(runtime.BeltSlotItemIds[3], Is.EqualTo("pack-item"));
            Assert.That(runtime.BackpackItemIds[0], Is.EqualTo("belt-item"));
        }

        [Test]
        public void TryAddStackItem_WhenItemAlreadyExists_IncreasesQuantityWithoutUsingNewSlot()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(4);

            var firstStore = runtime.TryAddStackItem("powder-varget", 120, out var firstArea, out var firstIndex, out _);
            var secondStore = runtime.TryAddStackItem("powder-varget", 80, out var secondArea, out var secondIndex, out _);

            Assert.That(firstStore, Is.True);
            Assert.That(secondStore, Is.True);
            Assert.That(firstArea, Is.EqualTo(InventoryArea.Belt));
            Assert.That(firstIndex, Is.EqualTo(0));
            Assert.That(secondArea, Is.EqualTo(InventoryArea.Belt));
            Assert.That(secondIndex, Is.EqualTo(0));
            Assert.That(runtime.BeltSlotItemIds[0], Is.EqualTo("powder-varget"));
            Assert.That(runtime.GetItemQuantity("powder-varget"), Is.EqualTo(200));
        }

        [Test]
        public void TryRemoveStackItem_PartialRemoval_KeepsSlotAndReducesQuantity()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.TryAddStackItem("primer-cci", 200, out _, out _, out _);

            var removed = runtime.TryRemoveStackItem("primer-cci", 50);

            Assert.That(removed, Is.True);
            Assert.That(runtime.BeltSlotItemIds[0], Is.EqualTo("primer-cci"));
            Assert.That(runtime.GetItemQuantity("primer-cci"), Is.EqualTo(150));
        }

        [Test]
        public void TryRemoveStackItem_RemovingFullQuantity_ClearsSlot()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.TryAddStackItem("bullet-smk-168", 60, out _, out _, out _);

            var removed = runtime.TryRemoveStackItem("bullet-smk-168", 60);

            Assert.That(removed, Is.True);
            Assert.That(runtime.BeltSlotItemIds[0], Is.Null);
            Assert.That(runtime.GetItemQuantity("bullet-smk-168"), Is.EqualTo(0));
        }

        [Test]
        public void TryRemoveStackItem_WhenQuantityInsufficient_ReturnsFalseAndKeepsState()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.TryAddStackItem("case-lapua-308", 20, out _, out _, out _);

            var removed = runtime.TryRemoveStackItem("case-lapua-308", 21);

            Assert.That(removed, Is.False);
            Assert.That(runtime.BeltSlotItemIds[0], Is.EqualTo("case-lapua-308"));
            Assert.That(runtime.GetItemQuantity("case-lapua-308"), Is.EqualTo(20));
        }

        [Test]
        public void TryAddStackItem_WhenQuantityExceedsMaxStack_SplitsAcrossSlots()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetItemMaxStack("ammo-22lr", 100);

            var stored = runtime.TryAddStackItem("ammo-22lr", 120, out _, out _, out _);

            Assert.That(stored, Is.True);
            Assert.That(runtime.BeltSlotItemIds[0], Is.EqualTo("ammo-22lr"));
            Assert.That(runtime.BeltSlotItemIds[1], Is.EqualTo("ammo-22lr"));
            Assert.That(runtime.GetSlotQuantity(InventoryArea.Belt, 0), Is.EqualTo(100));
            Assert.That(runtime.GetSlotQuantity(InventoryArea.Belt, 1), Is.EqualTo(20));
            Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(120));
        }

        [Test]
        public void TryMoveItem_MergeOverflow_KeepsRemainderInSourceSlot()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetItemMaxStack("ammo-22lr", 100);
            runtime.TryAddStackItem("ammo-22lr", 120, out _, out _, out _);

            var moved = runtime.TryMoveItem(InventoryArea.Belt, 0, InventoryArea.Belt, 1);

            Assert.That(moved, Is.True);
            Assert.That(runtime.GetSlotQuantity(InventoryArea.Belt, 1), Is.EqualTo(100));
            Assert.That(runtime.GetSlotQuantity(InventoryArea.Belt, 0), Is.EqualTo(20));
            Assert.That(runtime.GetItemQuantity("ammo-22lr"), Is.EqualTo(120));
        }

        [Test]
        public void TryRemoveFromSlot_BackpackSlot_RemovesFullStackAndReturnsMetadata()
        {
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(2);
            runtime.TryAddStackItem("case-lapua-308", 75, out _, out _, out _);
            runtime.TryAddStackItem("tool-trimmer", 1, out _, out _, out _);
            Assert.That(runtime.TryMoveItem(InventoryArea.Belt, 1, InventoryArea.Backpack, 0), Is.True);

            var removed = runtime.TryRemoveFromSlot(InventoryArea.Backpack, 0, out var removedItemId, out var removedQuantity);

            Assert.That(removed, Is.True);
            Assert.That(removedItemId, Is.EqualTo("tool-trimmer"));
            Assert.That(removedQuantity, Is.EqualTo(1));
            Assert.That(runtime.BackpackItemIds.Count, Is.EqualTo(0));
        }
    }
}
