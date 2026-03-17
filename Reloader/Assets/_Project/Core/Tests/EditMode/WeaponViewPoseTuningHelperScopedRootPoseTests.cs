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
}
