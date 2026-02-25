using NUnit.Framework;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Tests.EditMode
{
    public class InventoryModuleCompatibilityTests
    {
        [Test]
        public void InventoryModule_RoundTrip_PreservesBeltBackpackCapacityAndSelection()
        {
            var module = new InventoryModule
            {
                BackpackCapacity = 2,
                SelectedBeltIndex = 3
            };
            module.CarriedItemIds.Add("c-1");
            module.BeltSlotItemIds.Add("belt-0");
            module.BeltSlotItemIds.Add(null);
            module.BeltSlotItemIds.Add("belt-2");
            module.BeltSlotItemIds.Add(null);
            module.BeltSlotItemIds.Add("belt-4");
            module.BackpackItemIds.Add("pack-0");
            module.BackpackItemIds.Add("pack-1");

            var json = module.CaptureModuleStateJson();

            var restored = new InventoryModule();
            restored.RestoreModuleStateFromJson(json);

            Assert.That(restored.BeltSlotItemIds.Count, Is.EqualTo(InventoryModule.BeltSlotCount));
            Assert.That(restored.BeltSlotItemIds[0], Is.EqualTo("belt-0"));
            Assert.That(restored.BeltSlotItemIds[2], Is.EqualTo("belt-2"));
            Assert.That(restored.BeltSlotItemIds[4], Is.EqualTo("belt-4"));
            Assert.That(restored.BackpackItemIds, Is.EqualTo(new[] { "pack-0", "pack-1" }));
            Assert.That(restored.BackpackCapacity, Is.EqualTo(2));
            Assert.That(restored.SelectedBeltIndex, Is.EqualTo(3));
        }

        [Test]
        public void InventoryModule_Restore_LegacyPayload_DefaultsNewFieldsSafely()
        {
            var legacyPayload = "{\"carriedItemIds\":[\"c-1\",\"c-2\"]}";

            var restored = new InventoryModule();
            restored.RestoreModuleStateFromJson(legacyPayload);

            Assert.That(restored.CarriedItemIds, Is.EqualTo(new[] { "c-1", "c-2" }));
            Assert.That(restored.BeltSlotItemIds.Count, Is.EqualTo(InventoryModule.BeltSlotCount));
            Assert.That(restored.BeltSlotItemIds.TrueForAll(string.IsNullOrEmpty), Is.True);
            Assert.That(restored.BackpackItemIds.Count, Is.EqualTo(0));
            Assert.That(restored.BackpackCapacity, Is.EqualTo(0));
            Assert.That(restored.SelectedBeltIndex, Is.EqualTo(-1));
        }
    }
}
