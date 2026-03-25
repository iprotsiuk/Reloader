using NUnit.Framework;
using Reloader.Weapons.Ballistics;
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
    }
}
