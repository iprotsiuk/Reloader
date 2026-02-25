using NUnit.Framework;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class WeaponRuntimeStateTests
    {
        [Test]
        public void TryFire_ConsumesMagazineRoundAndSetsCooldown()
        {
            var state = new WeaponRuntimeState("weapon-rifle-01", magazineCapacity: 5, fireIntervalSeconds: 0.2f, magazineCount: 2, reserveCount: 10, chamberLoaded: true);

            var fired = state.TryFire(1f, out _);

            Assert.That(fired, Is.True);
            Assert.That(state.ChamberLoaded, Is.True);
            Assert.That(state.MagazineCount, Is.EqualTo(1));
            Assert.That(state.NextFireTime, Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void TryFire_WithoutChamberedRound_ReturnsFalse()
        {
            var state = new WeaponRuntimeState("weapon-rifle-01", magazineCapacity: 5, fireIntervalSeconds: 0.2f, magazineCount: 0, reserveCount: 10, chamberLoaded: false);

            var fired = state.TryFire(1f, out _);

            Assert.That(fired, Is.False);
            Assert.That(state.NextFireTime, Is.EqualTo(0f));
        }

        [Test]
        public void TryReload_MovesRoundsFromReserveToMagazine()
        {
            var state = new WeaponRuntimeState("weapon-rifle-01", magazineCapacity: 5, fireIntervalSeconds: 0.2f, magazineCount: 2, reserveCount: 9, chamberLoaded: true);

            var reloaded = state.TryReload();

            Assert.That(reloaded, Is.True);
            Assert.That(state.MagazineCount, Is.EqualTo(5));
            Assert.That(state.ReserveCount, Is.EqualTo(6));
        }

        [Test]
        public void TryReload_WhenChamberEmpty_ChambersRound()
        {
            var state = new WeaponRuntimeState("weapon-rifle-01", magazineCapacity: 5, fireIntervalSeconds: 0.2f, magazineCount: 0, reserveCount: 5, chamberLoaded: false);

            var reloaded = state.TryReload();

            Assert.That(reloaded, Is.True);
            Assert.That(state.ChamberLoaded, Is.True);
            Assert.That(state.MagazineCount, Is.EqualTo(4));
            Assert.That(state.ReserveCount, Is.EqualTo(0));
        }

        [Test]
        public void Registry_TryGetWeaponDefinition_ResolvesByItemId()
        {
            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();

            var definition = ScriptableObject.CreateInstance<Reloader.Weapons.Data.WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-rifle-01", "Rifle", 5, 0.2f, 120f, 1f, 30f, 200f);
            registry.SetDefinitionsForTests(new[] { definition });

            var resolved = registry.TryGetWeaponDefinition("weapon-rifle-01", out var found);

            Assert.That(resolved, Is.True);
            Assert.That(found, Is.EqualTo(definition));

            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(registryGo);
        }

        [Test]
        public void TryFire_WithBallisticSnapshots_ReturnsFiredRoundAndAdvancesQueue()
        {
            var state = new WeaponRuntimeState("weapon-rifle-01", magazineCapacity: 5, fireIntervalSeconds: 0.2f, magazineCount: 2, reserveCount: 0, chamberLoaded: true);
            var chamber = new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2500f, 12f, 168f, 0.46f, 1f);
            var nextRound = new AmmoBallisticSnapshot(AmmoSourceType.Handload, 2550f, 8f, 175f, 0.51f, 0.7f);
            var secondRound = new AmmoBallisticSnapshot(AmmoSourceType.Handload, 2520f, 9f, 175f, 0.5f, 0.8f);
            state.SetAmmoLoadoutForTests(chamber, new[] { nextRound, secondRound });

            var fired = state.TryFire(1f, out var fireData);

            Assert.That(fired, Is.True);
            Assert.That(fireData.FiredRound.HasValue, Is.True);
            Assert.That(fireData.FiredRound.Value.MuzzleVelocityFps, Is.EqualTo(2500f).Within(0.001f));
            Assert.That(state.MagazineCount, Is.EqualTo(1));

            var firedAgain = state.TryFire(2f, out var secondFireData);
            Assert.That(firedAgain, Is.True);
            Assert.That(secondFireData.FiredRound.HasValue, Is.True);
            Assert.That(secondFireData.FiredRound.Value.MuzzleVelocityFps, Is.EqualTo(2550f).Within(0.001f));
        }

        [Test]
        public void TryReload_WhenSnapshotsPresent_ChambersFromMagazineQueue()
        {
            var state = new WeaponRuntimeState("weapon-rifle-01", magazineCapacity: 5, fireIntervalSeconds: 0.2f, magazineCount: 0, reserveCount: 3, chamberLoaded: false);
            var firstRound = new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2650f, 15f, 168f, 0.45f, 1.3f);
            var secondRound = new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2660f, 14f, 168f, 0.45f, 1.2f);
            state.SetAmmoLoadoutForTests(null, new[] { firstRound, secondRound });

            var reloaded = state.TryReload();

            Assert.That(reloaded, Is.True);
            Assert.That(state.ChamberLoaded, Is.True);

            var fired = state.TryFire(1f, out var fireData);
            Assert.That(fired, Is.True);
            Assert.That(fireData.FiredRound.HasValue, Is.True);
            Assert.That(fireData.FiredRound.Value.MuzzleVelocityFps, Is.EqualTo(2650f).Within(0.001f));
        }

        [Test]
        public void TryReload_WithoutPreloadedSnapshots_GeneratesNamedAmmoSnapshots()
        {
            var state = new WeaponRuntimeState("weapon-rifle-01", magazineCapacity: 5, fireIntervalSeconds: 0.2f, magazineCount: 0, reserveCount: 5, chamberLoaded: false);

            var reloaded = state.TryReload();

            Assert.That(reloaded, Is.True);
            Assert.That(state.ChamberLoaded, Is.True);
            Assert.That(state.ChamberRound.HasValue, Is.True);
            Assert.That(state.ChamberRound.Value.DisplayName, Is.Not.Empty);
            Assert.That(state.GetMagazineRoundsSnapshot().Count, Is.EqualTo(4));
            Assert.That(state.GetMagazineRoundsSnapshot()[0].DisplayName, Is.Not.Empty);
        }
    }
}
