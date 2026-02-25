using NUnit.Framework;
using Reloader.Inventory;

namespace Reloader.Core.Tests.EditMode
{
    public class InventoryTransferEngineTests
    {
        [Test]
        public void TryTransfer_EmptyTarget_MovesStack()
        {
            var source = BuildContainer();
            var target = BuildContainer();
            source.TrySetSlot(0, new ItemStackState("item-a", 5, 20));

            var moved = InventoryTransferEngine.TryTransfer(source, 0, target, 0, out var result);

            Assert.That(moved, Is.True);
            Assert.That(result, Is.EqualTo(InventoryTransferResult.Moved));
            Assert.That(source.TryGetSlot(0, out _), Is.False);
            Assert.That(target.TryGetSlot(0, out var targetStack), Is.True);
            Assert.That(targetStack.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void TryTransfer_OccupiedDifferentItem_SwapsStacks()
        {
            var source = BuildContainer();
            var target = BuildContainer();
            source.TrySetSlot(0, new ItemStackState("item-a", 5, 20));
            target.TrySetSlot(0, new ItemStackState("item-b", 3, 10));

            var moved = InventoryTransferEngine.TryTransfer(source, 0, target, 0, out var result);

            Assert.That(moved, Is.True);
            Assert.That(result, Is.EqualTo(InventoryTransferResult.Swapped));
            Assert.That(source.TryGetSlot(0, out var sourceStack), Is.True);
            Assert.That(sourceStack.ItemId, Is.EqualTo("item-b"));
            Assert.That(target.TryGetSlot(0, out var targetStack), Is.True);
            Assert.That(targetStack.ItemId, Is.EqualTo("item-a"));
        }

        [Test]
        public void TryTransfer_SameItemOverflowMerge_FillsTargetAndLeavesRemainderInSource()
        {
            var source = BuildContainer();
            var target = BuildContainer();
            source.TrySetSlot(0, new ItemStackState("ammo-22lr", 90, 100));
            target.TrySetSlot(0, new ItemStackState("ammo-22lr", 30, 100));

            var moved = InventoryTransferEngine.TryTransfer(source, 0, target, 0, out var result);

            Assert.That(moved, Is.True);
            Assert.That(result, Is.EqualTo(InventoryTransferResult.MergedPartial));
            Assert.That(target.TryGetSlot(0, out var targetStack), Is.True);
            Assert.That(targetStack.Quantity, Is.EqualTo(100));
            Assert.That(source.TryGetSlot(0, out var sourceStack), Is.True);
            Assert.That(sourceStack.Quantity, Is.EqualTo(20));
        }

        [Test]
        public void TryTransfer_SameItemWithinCapacity_FullyMergesAndClearsSource()
        {
            var source = BuildContainer();
            var target = BuildContainer();
            source.TrySetSlot(0, new ItemStackState("ammo-50bmg", 5, 40));
            target.TrySetSlot(0, new ItemStackState("ammo-50bmg", 10, 40));

            var moved = InventoryTransferEngine.TryTransfer(source, 0, target, 0, out var result);

            Assert.That(moved, Is.True);
            Assert.That(result, Is.EqualTo(InventoryTransferResult.MergedFull));
            Assert.That(source.TryGetSlot(0, out _), Is.False);
            Assert.That(target.TryGetSlot(0, out var targetStack), Is.True);
            Assert.That(targetStack.Quantity, Is.EqualTo(15));
        }

        [Test]
        public void TryTransfer_WhenTargetCannotAcceptDrop_ReturnsPermissionDeniedWithoutMutation()
        {
            var source = BuildContainer();
            var target = new InventoryContainerState(
                InventoryContainerType.VendorStock,
                4,
                ContainerPermissions.ReadOnlyVendor);

            source.TrySetSlot(0, new ItemStackState("item-a", 5, 20));

            var moved = InventoryTransferEngine.TryTransfer(source, 0, target, 0, out var result);

            Assert.That(moved, Is.False);
            Assert.That(result, Is.EqualTo(InventoryTransferResult.PermissionDenied));
            Assert.That(source.TryGetSlot(0, out var sourceStack), Is.True);
            Assert.That(sourceStack.ItemId, Is.EqualTo("item-a"));
            Assert.That(target.TryGetSlot(0, out _), Is.False);
        }

        private static InventoryContainerState BuildContainer()
        {
            return new InventoryContainerState(
                InventoryContainerType.PlayerBackpack,
                4,
                ContainerPermissions.PlayerMutable);
        }
    }
}
