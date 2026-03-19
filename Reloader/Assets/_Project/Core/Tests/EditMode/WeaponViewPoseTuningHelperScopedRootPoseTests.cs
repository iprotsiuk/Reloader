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

        [Test]
        public void LateUpdate_HeldScopedAdsPose_ContinuesTrackingParentPoseDrift()
        {
            var alignerType = System.Type.GetType("Reloader.Game.Weapons.WeaponAimAligner, Reloader.Game.Weapons");
            var attachmentManagerType = System.Type.GetType("Reloader.Game.Weapons.AttachmentManager, Reloader.Game.Weapons");
            var adsStateControllerType = System.Type.GetType("Reloader.Game.Weapons.AdsStateController, Reloader.Game.Weapons");
            var opticDefinitionType = System.Type.GetType("Reloader.Game.Weapons.OpticDefinition, Reloader.Game.Weapons");
            Assert.That(alignerType, Is.Not.Null, "WeaponAimAligner type should exist.");
            Assert.That(attachmentManagerType, Is.Not.Null, "AttachmentManager type should exist.");
            Assert.That(adsStateControllerType, Is.Not.Null, "AdsStateController type should exist.");
            Assert.That(opticDefinitionType, Is.Not.Null, "OpticDefinition type should exist.");

            var root = new GameObject("ScopedTrackingRoot");
            var cameraTransform = new GameObject("Camera").transform;
            cameraTransform.SetParent(root.transform, false);
            cameraTransform.localPosition = new Vector3(0f, 0.05f, 0f);
            var pivotParent = new GameObject("WeaponRoot").transform;
            pivotParent.SetParent(cameraTransform, false);
            var adsPivot = new GameObject("AdsPivot").transform;
            adsPivot.SetParent(pivotParent, false);
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(adsPivot, false);
            scopeSlot.localPosition = new Vector3(0f, 0.05f, 0.2f);
            var ironSightAnchor = new GameObject("IronSightAnchor").transform;
            ironSightAnchor.SetParent(adsPivot, false);
            ironSightAnchor.localPosition = new Vector3(0f, 0.05f, 0.2f);

            var opticPrefab = new GameObject("TestOpticPrefab");
            var sightAnchor = new GameObject("SightAnchor").transform;
            sightAnchor.SetParent(opticPrefab.transform, false);
            sightAnchor.localPosition = new Vector3(0f, 0f, 0.1f);

            var opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType!);
            SetPrivateField(opticDefinition, "_opticId", "optic-test");
            SetPrivateField(opticDefinition, "_opticPrefab", opticPrefab);
            SetPrivateField(opticDefinition, "_magnificationMin", 5f);
            SetPrivateField(opticDefinition, "_magnificationMax", 25f);
            SetPrivateField(opticDefinition, "_isVariableZoom", true);

            try
            {
                var attachmentManager = root.AddComponent(attachmentManagerType!);
                var adsStateController = root.AddComponent(adsStateControllerType!);
                var aligner = root.AddComponent(alignerType!);

                var configureMountsMethod = attachmentManagerType.GetMethod("ConfigureMounts", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(configureMountsMethod, Is.Not.Null, "AttachmentManager.ConfigureMounts should exist.");
                configureMountsMethod!.Invoke(attachmentManager, new object[] { scopeSlot, ironSightAnchor, null, null });

                var equipOpticMethod = attachmentManagerType.GetMethod("EquipOptic", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(equipOpticMethod, Is.Not.Null, "AttachmentManager.EquipOptic should exist.");
                Assert.That((bool)equipOpticMethod!.Invoke(attachmentManager, new object[] { opticDefinition }), Is.True);

                SetPrivateField(adsStateController, "<AdsT>k__BackingField", 1f);

                var bindRuntimeReferencesMethod = alignerType.GetMethod("BindRuntimeReferences", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(bindRuntimeReferencesMethod, Is.Not.Null, "WeaponAimAligner.BindRuntimeReferences should exist.");
                bindRuntimeReferencesMethod!.Invoke(aligner, new object[] { adsPivot, cameraTransform, attachmentManager, adsStateController });

                var lateUpdateMethod = alignerType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(lateUpdateMethod, Is.Not.Null, "WeaponAimAligner.LateUpdate should exist.");

                lateUpdateMethod!.Invoke(aligner, null);
                pivotParent.localRotation = Quaternion.Euler(0f, 2f, 0f);
                lateUpdateMethod.Invoke(aligner, null);

                var alignmentErrorProperty = alignerType.GetProperty("DebugAlignmentErrorAngleDegrees", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(alignmentErrorProperty, Is.Not.Null, "Expected debug alignment angle property to exist.");
                var angleError = (float)alignmentErrorProperty!.GetValue(aligner);
                Assert.That(
                    angleError,
                    Is.LessThan(0.05f),
                    "Held magnified ADS should keep correcting live parent-pose drift every frame instead of freezing the previous solve.");
            }
            finally
            {
                Object.DestroyImmediate(opticDefinition);
                Object.DestroyImmediate(opticPrefab);
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {instance.GetType().Name}.");
            field!.SetValue(instance, value);
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
