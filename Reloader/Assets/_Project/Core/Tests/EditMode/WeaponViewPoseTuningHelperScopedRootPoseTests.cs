using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Runtime;
using UnityEngine;

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

        [TestCase("Auto", 0.012f)]
        [TestCase("RenderTexturePiP", 0.012f)]
        public void ResolveOpticEyeReliefBackOffset_UsesAuthoredOpticBaselineForAllVisualModes(
            string visualModeName,
            float expectedEyeRelief)
        {
            var alignerType = System.Type.GetType("Reloader.Game.Weapons.WeaponAimAligner, Reloader.Game.Weapons");
            var opticDefinitionType = System.Type.GetType("Reloader.Game.Weapons.OpticDefinition, Reloader.Game.Weapons");
            var adsVisualModeType = System.Type.GetType("Reloader.Game.Weapons.AdsVisualMode, Reloader.Game.Weapons");
            Assert.That(alignerType, Is.Not.Null, "WeaponAimAligner type should exist.");
            Assert.That(opticDefinitionType, Is.Not.Null, "OpticDefinition type should exist.");
            Assert.That(adsVisualModeType, Is.Not.Null, "AdsVisualMode type should exist.");

            var method = alignerType!.GetMethod(
                "ResolveOpticEyeReliefBackOffset",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private optic eye-relief helper to exist.");

            var opticDefinition = UnityEngine.ScriptableObject.CreateInstance(opticDefinitionType);
            try
            {
                var visualModeField = opticDefinitionType!.GetField("_visualModePolicy", BindingFlags.Instance | BindingFlags.NonPublic);
                var eyeReliefField = opticDefinitionType.GetField("_eyeReliefBackOffset", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(visualModeField, Is.Not.Null, "OpticDefinition visual mode field should exist.");
                Assert.That(eyeReliefField, Is.Not.Null, "OpticDefinition eye relief field should exist.");

                visualModeField!.SetValue(opticDefinition, System.Enum.Parse(adsVisualModeType!, visualModeName));
                eyeReliefField!.SetValue(opticDefinition, expectedEyeRelief);

                var actual = (float)method!.Invoke(null, new object[] { opticDefinition });
                Assert.That(actual, Is.EqualTo(expectedEyeRelief).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(opticDefinition);
            }
        }
    }

    public sealed class RenderTextureScopeControllerReticleOffsetTests
    {
        [Test]
        public void EnableCompositeReticle_FfpOffset_ScalesWithMagnification()
        {
            var controllerType = System.Type.GetType("Reloader.Game.Weapons.RenderTextureScopeController, Reloader.Game.Weapons");
            var reticleDefinitionType = System.Type.GetType("Reloader.Game.Weapons.ScopeReticleDefinition, Reloader.Game.Weapons");
            var reticleModeType = System.Type.GetType("Reloader.Game.Weapons.ScopeReticleMode, Reloader.Game.Weapons");
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(reticleDefinitionType, Is.Not.Null);
            Assert.That(reticleModeType, Is.Not.Null);

            var gameObject = new GameObject("ReticleOffsetTest");
            var controller = gameObject.AddComponent(controllerType!);
            var reticleDefinition = ScriptableObject.CreateInstance(reticleDefinitionType!);

            try
            {
                SetPrivateField(reticleDefinition, "_mode", System.Enum.Parse(reticleModeType!, "Ffp"));
                SetPrivateField(reticleDefinition, "_referenceMagnification", 4f);

                var enableMethod = controllerType!.GetMethod(
                    "EnableCompositeReticle",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(enableMethod, Is.Not.Null);

                enableMethod!.Invoke(controller, new object[] { reticleDefinition, 8f, 1f, new Vector2(0.002f, 0f) });

                var currentOffsetProperty = controllerType.GetProperty("CurrentCompositeReticleOffset", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(currentOffsetProperty, Is.Not.Null);
                var actual = (Vector2)currentOffsetProperty!.GetValue(controller);
                Assert.That(actual.x, Is.EqualTo(0.004f).Within(0.0001f));
                Assert.That(actual.y, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(reticleDefinition);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EnableCompositeReticle_SfpOffset_RemainsConstantAcrossMagnification()
        {
            var controllerType = System.Type.GetType("Reloader.Game.Weapons.RenderTextureScopeController, Reloader.Game.Weapons");
            var reticleDefinitionType = System.Type.GetType("Reloader.Game.Weapons.ScopeReticleDefinition, Reloader.Game.Weapons");
            var reticleModeType = System.Type.GetType("Reloader.Game.Weapons.ScopeReticleMode, Reloader.Game.Weapons");
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(reticleDefinitionType, Is.Not.Null);
            Assert.That(reticleModeType, Is.Not.Null);

            var gameObject = new GameObject("ReticleOffsetTest");
            var controller = gameObject.AddComponent(controllerType!);
            var reticleDefinition = ScriptableObject.CreateInstance(reticleDefinitionType!);

            try
            {
                SetPrivateField(reticleDefinition, "_mode", System.Enum.Parse(reticleModeType!, "Sfp"));
                SetPrivateField(reticleDefinition, "_referenceMagnification", 4f);

                var enableMethod = controllerType!.GetMethod(
                    "EnableCompositeReticle",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(enableMethod, Is.Not.Null);

                enableMethod!.Invoke(controller, new object[] { reticleDefinition, 8f, 1f, new Vector2(0.002f, 0f) });

                var currentOffsetProperty = controllerType.GetProperty("CurrentCompositeReticleOffset", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(currentOffsetProperty, Is.Not.Null);
                var actual = (Vector2)currentOffsetProperty!.GetValue(controller);
                Assert.That(actual.x, Is.EqualTo(0.002f).Within(0.0001f));
                Assert.That(actual.y, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(reticleDefinition);
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {instance.GetType().Name}.");
            field!.SetValue(instance, value);
        }
    }
}
