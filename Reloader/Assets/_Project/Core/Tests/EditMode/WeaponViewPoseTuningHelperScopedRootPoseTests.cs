using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Runtime;

namespace Reloader.Core.Tests.EditMode
{
    public sealed class WeaponViewPoseTuningHelperScopedRootPoseTests
    {
        [TestCase(false, true, 0.95f, false)]
        [TestCase(false, true, 0.999f, true)]
        [TestCase(false, true, 1.0f, true)]
        [TestCase(true, true, 0.95f, true)]
        [TestCase(true, true, 0.949f, false)]
        [TestCase(false, false, 1.0f, false)]
        public void ShouldHoldScopedAdsRootPose_UsesOnlyStableMagnifiedScopedAds(
            bool isCurrentlyHoldingScopedAdsRootPose,
            bool useDirectScopedBlend,
            float targetAdsBlendT,
            bool expected)
        {
            var method = typeof(WeaponViewPoseTuningHelper).GetMethod(
                "ShouldHoldScopedAdsRootPose",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private scoped-root hold helper to exist.");

            var actual = (bool)method!.Invoke(null, new object[] { isCurrentlyHoldingScopedAdsRootPose, useDirectScopedBlend, targetAdsBlendT });
            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    public sealed class WeaponAimAlignerScopedPoseHoldTests
    {
        [TestCase(false, 0.999f, false)]
        [TestCase(true, 1.0f, false)]
        [TestCase(true, 0.95f, false)]
        public void ShouldHoldScopedAdsPose_ReleasesImmediatelyWhenActiveOpticIsMissing(
            bool isCurrentlyHoldingScopedAdsPose,
            float adsBlendT,
            bool expected)
        {
            var alignerType = System.Type.GetType("Reloader.Game.Weapons.WeaponAimAligner, Reloader.Game.Weapons");
            Assert.That(alignerType, Is.Not.Null, "WeaponAimAligner type should exist.");

            var method = alignerType!.GetMethod(
                "ShouldHoldScopedAdsPose",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private scoped-pose hold helper to exist.");

            var actual = (bool)method!.Invoke(null, new object[] { isCurrentlyHoldingScopedAdsPose, null, adsBlendT });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ApplyEyeReliefOffset_UsesCameraForwardAxis()
        {
            var alignerType = System.Type.GetType("Reloader.Game.Weapons.WeaponAimAligner, Reloader.Game.Weapons");
            Assert.That(alignerType, Is.Not.Null, "WeaponAimAligner type should exist.");

            var method = alignerType!.GetMethod(
                "ApplyEyeReliefOffset",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private eye-relief offset helper to exist.");

            var start = new UnityEngine.Vector3(10f, 20f, 30f);
            var cameraForward = UnityEngine.Vector3.right;
            var actual = (UnityEngine.Vector3)method!.Invoke(null, new object[] { start, cameraForward, 2f });

            Assert.That(actual, Is.EqualTo(new UnityEngine.Vector3(8f, 20f, 30f)));
        }
    }
}
