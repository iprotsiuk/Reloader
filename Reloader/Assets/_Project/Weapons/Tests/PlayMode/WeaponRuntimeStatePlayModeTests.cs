using NUnit.Framework;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Runtime;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponRuntimeStatePlayModeTests
    {
        [Test]
        public void SetAmmoCounts_ClampsToCapacity_AndSynthesizesChamberRound()
        {
            var state = new WeaponRuntimeState(
                "weapon-rifle-01",
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
                "weapon-rifle-01",
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
