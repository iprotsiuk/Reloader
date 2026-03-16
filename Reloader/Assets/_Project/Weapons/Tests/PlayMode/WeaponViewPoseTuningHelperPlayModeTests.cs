using NUnit.Framework;
using UnityEngine;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class WeaponAimAlignerPlayModeTests
    {
        [Test]
        public void ResolveNextPivotLocalPosition_WhenScopedAdsTargetChanges_DampsTowardSolvedPose()
        {
            var next = InvokeResolveNextPivotLocalPosition(
                currentLocalPosition: Vector3.zero,
                restLocalPosition: Vector3.zero,
                targetLocalPosition: new Vector3(0.3f, 0f, 0f),
                adsT: 1f,
                positionLerpSpeed: 24f,
                deltaTime: 1f / 60f);

            Assert.That(next.x, Is.GreaterThan(0f));
            Assert.That(next.x, Is.LessThan(0.3f), "Scoped pivot movement should be damped instead of snapping to the solved target in one frame.");
        }

        [Test]
        public void ResolveNextPivotLocalRotation_WhenScopedAdsTargetChanges_DampsTowardSolvedPose()
        {
            var next = InvokeResolveNextPivotLocalRotation(
                currentLocalRotation: Quaternion.identity,
                restLocalRotation: Quaternion.identity,
                targetLocalRotation: Quaternion.Euler(0f, 12f, 0f),
                adsT: 1f,
                rotationLerpSpeed: 24f,
                deltaTime: 1f / 60f);

            Assert.That(Quaternion.Angle(Quaternion.identity, next), Is.GreaterThan(0f));
            Assert.That(
                Quaternion.Angle(next, Quaternion.Euler(0f, 12f, 0f)),
                Is.GreaterThan(0.01f),
                "Scoped pivot rotation should still be easing toward the solved target after one frame.");
        }

        private static Vector3 InvokeResolveNextPivotLocalPosition(
            Vector3 currentLocalPosition,
            Vector3 restLocalPosition,
            Vector3 targetLocalPosition,
            float adsT,
            float positionLerpSpeed,
            float deltaTime)
        {
            var method = ResolveWeaponAimAlignerType().GetMethod(
                "ResolveNextPivotLocalPosition",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected WeaponAimAligner to expose a scoped pivot damping helper.");

            return (Vector3)method!.Invoke(null, new object[]
            {
                currentLocalPosition,
                restLocalPosition,
                targetLocalPosition,
                adsT,
                positionLerpSpeed,
                deltaTime
            });
        }

        private static Quaternion InvokeResolveNextPivotLocalRotation(
            Quaternion currentLocalRotation,
            Quaternion restLocalRotation,
            Quaternion targetLocalRotation,
            float adsT,
            float rotationLerpSpeed,
            float deltaTime)
        {
            var method = ResolveWeaponAimAlignerType().GetMethod(
                "ResolveNextPivotLocalRotation",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Expected WeaponAimAligner to expose a scoped pivot damping helper.");

            return (Quaternion)method!.Invoke(null, new object[]
            {
                currentLocalRotation,
                restLocalRotation,
                targetLocalRotation,
                adsT,
                rotationLerpSpeed,
                deltaTime
            });
        }

        private static System.Type ResolveWeaponAimAlignerType()
        {
            var type = System.Type.GetType("Reloader.Game.Weapons.WeaponAimAligner, Assembly-CSharp");
            Assert.That(type, Is.Not.Null, "Expected WeaponAimAligner runtime type to be available.");
            return type!;
        }
    }
}
