using NUnit.Framework;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponRuntimeStatePlayModeTests
    {
        [Test]
        public void WeaponDefinition_ReturnsCompatibleAttachmentIds_PerSlot()
        {
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 100f, 1f, 20f, 250f);
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x", "att-optic-8x" }),
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Muzzle, new[] { "att-muzzle-brake" })
            });

            var scopeIds = definition.GetCompatibleAttachmentItemIds(WeaponAttachmentSlotType.Scope);
            var muzzleIds = definition.GetCompatibleAttachmentItemIds(WeaponAttachmentSlotType.Muzzle);

            Assert.That(scopeIds, Is.EqualTo(new[] { "att-optic-4x", "att-optic-8x" }));
            Assert.That(muzzleIds, Is.EqualTo(new[] { "att-muzzle-brake" }));

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void WeaponDefinition_ReturnsEmptyCompatibility_WhenSlotNotConfigured()
        {
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 100f, 1f, 20f, 250f);

            var slotIds = definition.GetCompatibleAttachmentItemIds(WeaponAttachmentSlotType.Scope);

            Assert.That(slotIds, Is.Not.Null);
            Assert.That(slotIds.Count, Is.EqualTo(0));

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void RuntimeState_StoresAndRetrievesEquippedAttachmentItemId_PerSlot()
        {
            var state = new WeaponRuntimeState(
                "weapon-kar98k",
                5,
                0.1f,
                0,
                0,
                false);

            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));

            state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-4x");
            state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Muzzle, "att-muzzle-brake");

            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo("att-optic-4x"));
            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Muzzle), Is.EqualTo("att-muzzle-brake"));

            state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, null);
            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));
        }

        [Test]
        public void SetAmmoCounts_ClampsToCapacity_AndSynthesizesChamberRound()
        {
            var state = new WeaponRuntimeState(
                "weapon-kar98k",
                5,
                0.1f,
                0,
                0,
                false);

            state.SetAmmoCounts(999, -12, true);

            Assert.That(state.MagazineCount, Is.EqualTo(5));
            Assert.That(state.GetMagazineRoundsSnapshot().Count, Is.EqualTo(5));
            Assert.That(state.ReserveCount, Is.EqualTo(0));
            Assert.That(state.ChamberLoaded, Is.True);
            Assert.That(state.ChamberRound.HasValue, Is.True);
        }

        [Test]
        public void SetAmmoLoadoutForTests_ClampsMagazineRoundsToCapacity()
        {
            var state = new WeaponRuntimeState(
                "weapon-kar98k",
                3,
                0.1f,
                0,
                0,
                false);

            state.SetAmmoLoadoutForTests(
                null,
                new[]
                {
                    BuildRound("r1"),
                    BuildRound("r2"),
                    BuildRound("r3"),
                    BuildRound("r4"),
                    BuildRound("r5")
                });

            Assert.That(state.ChamberLoaded, Is.False);
            Assert.That(state.ChamberRound.HasValue, Is.False);
            Assert.That(state.MagazineCount, Is.EqualTo(3));
            Assert.That(state.GetMagazineRoundsSnapshot().Count, Is.EqualTo(3));
        }

        private static AmmoBallisticSnapshot BuildRound(string cartridgeId)
        {
            return new AmmoBallisticSnapshot(
                AmmoSourceType.Factory,
                2780f,
                55f,
                147f,
                0.398f,
                4.5f,
                "Factory .308 147gr FMJ",
                cartridgeId,
                "ammo-factory-308-147-fmj");
        }
    }
}
