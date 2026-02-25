using NUnit.Framework;
using Reloader.Player.Viewmodel;
using Reloader.Weapons.Data;
using UnityEngine;

namespace Reloader.Core.Tests.EditMode
{
    public class AnimationProfileContractsTests
    {
        [Test]
        public void WeaponAnimationProfile_DefaultAdsSpeedMultiplier_IsPointSeven()
        {
            var profile = ScriptableObject.CreateInstance<WeaponAnimationProfile>();
            try
            {
                Assert.That(profile.AdsSpeedMultiplier, Is.EqualTo(0.7f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void AnimationContractProfile_DefaultParameterNames_AreStable()
        {
            var profile = ScriptableObject.CreateInstance<AnimationContractProfile>();
            try
            {
                Assert.That(profile.MoveSpeedParameter, Is.EqualTo("MoveSpeed01"));
                Assert.That(profile.AimWeightParameter, Is.EqualTo("AimWeight"));
                Assert.That(profile.IsAimingParameter, Is.EqualTo("IsAiming"));
                Assert.That(profile.ReloadTrigger, Is.EqualTo("Reload"));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CharacterViewmodelProfile_DefaultOptionalOffsets_AreEmpty()
        {
            var profile = ScriptableObject.CreateInstance<CharacterViewmodelProfile>();
            try
            {
                Assert.That(profile.WeaponFamilyOffsetOverrides, Is.Not.Null);
                Assert.That(profile.WeaponFamilyOffsetOverrides.Count, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void AnimationContractValidator_MissingRequiredPoint_ReportsError()
        {
            var root = new GameObject("Root");
            try
            {
                var result = AnimationContractValidator.ValidateBindings(root.transform, contractMajorVersion: 1, expectedMajorVersion: 1);
                Assert.That(result.ErrorsCount, Is.GreaterThan(0));
                Assert.That(result.WarningsCount, Is.GreaterThanOrEqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AnimationContractValidator_MajorVersionMismatch_ReportsError()
        {
            var root = new GameObject("Root");
            var muzzle = new GameObject("Muzzle");
            var rightGrip = new GameObject("RightHandGrip");
            var leftIk = new GameObject("LeftHandIKTarget");
            var aimRef = new GameObject("AimReference");
            muzzle.transform.SetParent(root.transform);
            rightGrip.transform.SetParent(root.transform);
            leftIk.transform.SetParent(root.transform);
            aimRef.transform.SetParent(root.transform);

            try
            {
                var result = AnimationContractValidator.ValidateBindings(root.transform, contractMajorVersion: 2, expectedMajorVersion: 1);
                Assert.That(result.ErrorsCount, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
