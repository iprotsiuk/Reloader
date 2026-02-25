using NUnit.Framework;
using Reloader.Inventory;

namespace Reloader.Core.Tests.EditMode
{
    public class InventoryContainerStateTests
    {
        [Test]
        public void Ctor_InitializesRequestedSlotCount()
        {
            var container = new InventoryContainerState(
                InventoryContainerType.PlayerBackpack,
                8,
                ContainerPermissions.PlayerMutable);

            Assert.That(container.SlotCount, Is.EqualTo(8));
        }

        [Test]
        public void TrySetSlot_WhenIndexInvalid_ReturnsFalse()
        {
            var container = new InventoryContainerState(
                InventoryContainerType.PlayerBackpack,
                2,
                ContainerPermissions.PlayerMutable);

            var set = container.TrySetSlot(3, new ItemStackState("item", 2, 20));

            Assert.That(set, Is.False);
        }

        [Test]
        public void TrySetSlot_StoresStackAndTryGetSlotResolvesIt()
        {
            var container = new InventoryContainerState(
                InventoryContainerType.PlayerBelt,
                4,
                ContainerPermissions.PlayerMutable);

            var set = container.TrySetSlot(1, new ItemStackState("ammo-22lr", 50, 500));
            var got = container.TryGetSlot(1, out var stack);

            Assert.That(set, Is.True);
            Assert.That(got, Is.True);
            Assert.That(stack.ItemId, Is.EqualTo("ammo-22lr"));
            Assert.That(stack.Quantity, Is.EqualTo(50));
            Assert.That(stack.MaxStack, Is.EqualTo(500));
        }
    }
}
