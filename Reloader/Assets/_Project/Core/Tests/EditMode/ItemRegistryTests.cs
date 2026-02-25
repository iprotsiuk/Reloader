using NUnit.Framework;
using Reloader.Core.Items;

namespace Reloader.Core.Tests.EditMode
{
    public class ItemRegistryTests
    {
        [Test]
        public void Create_AssignsUniqueInstanceId_AndStoresInstance()
        {
            var registry = new ItemRegistry();

            var first = registry.Create("powder-varget", 1, 1f, 0, "{}");
            var second = registry.Create("powder-varget", 1, 1f, 0, "{}");

            Assert.That(first.InstanceId, Is.Not.EqualTo(second.InstanceId));
            Assert.That(registry.TryGet(first.InstanceId, out var resolved), Is.True);
            Assert.That(resolved.DefinitionId, Is.EqualTo("powder-varget"));
        }

        [Test]
        public void TryAdd_DuplicateInstanceId_ReturnsFalse()
        {
            var registry = new ItemRegistry();
            var instance = new ItemInstance("instance-a", "bullet-308", 10, 1f, 0, "{}");

            var firstAdd = registry.TryAdd(instance);
            var secondAdd = registry.TryAdd(instance);

            Assert.That(firstAdd, Is.True);
            Assert.That(secondAdd, Is.False);
        }
    }
}
