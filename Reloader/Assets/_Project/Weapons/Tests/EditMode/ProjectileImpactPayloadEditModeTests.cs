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
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
            }
        }

        [Test]
        public void Constructor_WithExplicitImpactEnergy_PreservesDeliveredEnergyMetadata()
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
                    sourcePoint: Vector3.zero,
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 847.344f,
                    projectileMassGrains: 147f,
                    deliveredEnergyJoules: 3419.6f);

                Assert.That(payload.ImpactSpeedMetersPerSecond, Is.EqualTo(847.344f));
                Assert.That(payload.ProjectileMassGrains, Is.EqualTo(147f));
                Assert.That(payload.DeliveredEnergyJoules, Is.EqualTo(3419.6f));
            }
            finally
            {
                Object.DestroyImmediate(hitObject);
            }
        }

        [Test]
        public void WeaponAmmoDefaults_Factory308LocksExpectedBallisticInputs()
        {
            var round = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");

            Assert.That(round.AmmoItemId, Is.EqualTo("ammo-factory-308-147-fmj"));
            Assert.That(round.MuzzleVelocityFps, Is.EqualTo(2780f));
            Assert.That(round.VelocityStdDevFps, Is.EqualTo(55f));
            Assert.That(round.ProjectileMassGrains, Is.EqualTo(147f));
            Assert.That(round.BallisticCoefficientG1, Is.EqualTo(0.398f));
        }
    }
}
