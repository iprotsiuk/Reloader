using NUnit.Framework;
using Reloader.Core.Items;

namespace Reloader.Core.Tests.EditMode
{
    public class ItemLocationServiceTests
    {
        [Test]
        public void Move_OverwritesPreviousLocation_KeepsSingleActiveLocation()
        {
            var locations = new ItemLocationService();

            locations.Move("inst-1", new ItemLocation(ItemOwnerType.World, "drop-1", "World", 0));
            locations.Move("inst-1", new ItemLocation(ItemOwnerType.PlayerInventory, "player", "Belt", 2));

            Assert.That(locations.TryGet("inst-1", out var resolved), Is.True);
            Assert.That(resolved.OwnerType, Is.EqualTo(ItemOwnerType.PlayerInventory));
            Assert.That(resolved.OwnerId, Is.EqualTo("player"));
            Assert.That(resolved.SlotType, Is.EqualTo("Belt"));
            Assert.That(resolved.SlotIndex, Is.EqualTo(2));
        }

        [Test]
        public void Remove_RemovesExistingLocation()
        {
            var locations = new ItemLocationService();
            locations.Move("inst-1", new ItemLocation(ItemOwnerType.World, "drop-1", "World", 0));

            var removed = locations.Remove("inst-1");

            Assert.That(removed, Is.True);
            Assert.That(locations.TryGet("inst-1", out _), Is.False);
        }
    }
}
