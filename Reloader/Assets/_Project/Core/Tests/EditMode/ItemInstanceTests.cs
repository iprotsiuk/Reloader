using NUnit.Framework;
using Reloader.Core.Items;

namespace Reloader.Core.Tests.EditMode
{
    public class ItemInstanceTests
    {
        [Test]
        public void Ctor_ClampsDurabilityAndQuantity()
        {
            var instance = new ItemInstance("id-1", "powder-varget", 0, 1.5f, 0, "{}");

            Assert.That(instance.Quantity, Is.EqualTo(1));
            Assert.That(instance.Durability01, Is.EqualTo(1f));
        }
    }
}
