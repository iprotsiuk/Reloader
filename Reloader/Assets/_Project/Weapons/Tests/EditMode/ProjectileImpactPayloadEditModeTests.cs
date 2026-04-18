using NUnit.Framework;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Runtime;
using UnityEngine;

namespace Reloader.Weapons.Tests.EditMode
{
    public sealed class ProjectileImpactPayloadEditModeTests
    {
        [Test]
        public void LegacyFiveArgumentConstructor_PreservesDefaultImpactMetadata()
        {
            var hitObject = new GameObject("ImpactTarget");

            try
            {
                var payload = new ProjectileImpactPayload(
                    "weapon-kar98k",
                    new Vector3(1f, 2f, 3f),
                    Vector3.up,
                    20f,
                    hitObject);

                Assert.That(payload.ItemId, Is.EqualTo("weapon-kar98k"));
                Assert.That(payload.HitObject, Is.SameAs(hitObject));
                Assert.That(payload.SourcePoint, Is.Null);
                Assert.That(payload.Direction, Is.EqualTo(Vector3.forward));
                Assert.That(payload.ImpactSpeedMetersPerSecond, Is.EqualTo(0f));
                Assert.That(payload.ProjectileMassGrains, Is.EqualTo(0f));
                Assert.That(payload.DeliveredEnergyJoules, Is.EqualTo(0f));
                Assert.That(payload.CoverPenetrationPower, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
            }
        }

        [Test]
        public void Constructor_WithExplicitImpactEnergy_PreservesDeliveredEnergyAndCoverPenetrationMetadata()
        {
            var hitObject = new GameObject("ImpactTarget");

            try
            {
                var payload = new ProjectileImpactPayload(
                    "weapon-kar98k",
                    new Vector3(1f, 2f, 3f),
                    Vector3.up,
                    20f,
                    hitObject,
                    Vector3.zero,
                    Vector3.forward,
                    847.344f,
                    147f,
                    3419.6f,
                    1.75f);

                Assert.That(payload.ImpactSpeedMetersPerSecond, Is.EqualTo(847.344f));
                Assert.That(payload.ProjectileMassGrains, Is.EqualTo(147f));
                Assert.That(payload.DeliveredEnergyJoules, Is.EqualTo(3419.6f));
                Assert.That(payload.CoverPenetrationPower, Is.EqualTo(1.75f));
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
            }
        }

        [Test]
        public void CartridgeBallisticSpecBuilder_Build_PreservesCoverPenetrationPower()
        {
            var snapshot = new AmmoBallisticSnapshot(
                AmmoSourceType.Handload,
                2725f,
                8f,
                175f,
                0.51f,
                0.7f,
                "Handload .308",
                "cartridge-a",
                "ammo-handload-308",
                2.25f);

            var spec = CartridgeBallisticSpecBuilder.Build(snapshot, rngSample01: 0.5f);

            Assert.That(spec.CoverPenetrationPower, Is.EqualTo(2.25f));
        }

        [Test]
        public void CartridgeBallisticSpecBuilder_Build_ClampsNegativeCoverPenetrationPowerToZero()
        {
            var snapshot = new AmmoBallisticSnapshot(
                AmmoSourceType.Handload,
                2725f,
                8f,
                175f,
                0.51f,
                0.7f,
                "Handload .308",
                "cartridge-b",
                "ammo-handload-308",
                -2f);

            var spec = CartridgeBallisticSpecBuilder.Build(snapshot, rngSample01: 0.5f);

            Assert.That(spec.CoverPenetrationPower, Is.EqualTo(0f));
        }

        [Test]
        public void WeaponAmmoDefaults_Factory308LocksExpectedBallisticInputs()
        {
            var round = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");

            Assert.That(round.AmmoItemId, Is.EqualTo("ammo-factory-308-147-fmj"));
            Assert.That(round.DisplayName, Is.EqualTo("Factory .308 147gr FMJ"));
            Assert.That(round.MuzzleVelocityFps, Is.EqualTo(2780f));
            Assert.That(round.VelocityStdDevFps, Is.EqualTo(55f));
            Assert.That(round.ProjectileMassGrains, Is.EqualTo(147f));
            Assert.That(round.BallisticCoefficientG1, Is.EqualTo(0.398f));
            Assert.That(round.CoverPenetrationPower, Is.EqualTo(0f));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void WeaponAmmoDefaults_Specialty308ApDefaultsBlankOrNullAmmoItemIdToSpecialtyId(string ammoItemId)
        {
            var round = WeaponAmmoDefaults.BuildSpecialtyRound(ammoItemId);

            Assert.That(round.AmmoItemId, Is.EqualTo(WeaponAmmoDefaults.SpecialtyAmmoItemId));
            Assert.That(round.DisplayName, Is.EqualTo(WeaponAmmoDefaults.SpecialtyAmmoDisplayName));
            Assert.That(round.MuzzleVelocityFps, Is.EqualTo(2780f));
            Assert.That(round.VelocityStdDevFps, Is.EqualTo(55f));
            Assert.That(round.ProjectileMassGrains, Is.EqualTo(150f));
            Assert.That(round.BallisticCoefficientG1, Is.EqualTo(0.398f));
            Assert.That(round.CoverPenetrationPower, Is.EqualTo(1f));
        }

        [Test]
        public void WeaponProjectile_Initialize_StoresCoverPenetrationPower()
        {
            var projectileGo = new GameObject("Projectile");

            try
            {
                var projectile = projectileGo.AddComponent<WeaponProjectile>();
                projectile.Initialize(
                    "weapon-kar98k",
                    Vector3.forward,
                    speed: 120f,
                    gravityMultiplier: 0f,
                    damage: 33f,
                    coverPenetrationPower: 1.5f);

                Assert.That(projectile.CoverPenetrationPower, Is.EqualTo(1.5f));
            }
            finally
            {
                Object.DestroyImmediate(projectileGo);
            }
        }

    }
}
