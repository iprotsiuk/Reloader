using System.Reflection;
using NUnit.Framework;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Core.Tests.EditMode
{
    public sealed class WeaponViewPoseTuningHelperScopedRootPoseTests
    {
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void ShouldHoldScopedAdsRootPose_UsesControllerScopedPresentationState(
            bool stableScopedPresentationActive,
            bool expected)
        {
            var method = typeof(WeaponViewPoseTuningHelper).GetMethod(
                "ShouldHoldScopedAdsRootPose",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected private scoped-root hold helper to exist.");

            var actual = (bool)method!.Invoke(null, new object[] { stableScopedPresentationActive });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void LateUpdate_HipfireRuntimeWithoutPreview_DoesNotRewriteRootPose()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<PlayerWeaponController>();
            var view = new GameObject("WeaponView");
            view.transform.SetParent(root.transform, false);
            var helper = view.AddComponent<WeaponViewPoseTuningHelper>();
#if UNITY_EDITOR
            var previousSelection = Selection.activeObject;
#endif

            try
            {
                SetPrivateField(helper, "_weaponController", controller);
                SetPrivateField(helper, "_enabledInPlayMode", true);
                SetPrivateField(helper, "_targetWeaponItemId", "weapon-kar98k");
                SetPrivateField(helper, "_hipLocalPosition", new Vector3(0.25f, -0.5f, 1.5f));
                SetPrivateField(helper, "_hipLocalEuler", new Vector3(1f, 2f, 3f));
                SetPrivateField(helper, "_adsLocalPosition", new Vector3(4f, 5f, 6f));
                SetPrivateField(helper, "_adsLocalEuler", new Vector3(7f, 8f, 9f));
                SetPrivateField(helper, "_rifleLocalEulerOffset", Vector3.zero);
                SetPrivateField(helper, "_blendSpeed", 24f);
                SetPrivateField(controller, "_equippedItemId", "weapon-kar98k");
                SetPrivateField(controller, "_equippedWeaponView", view);
                SetPrivateField(controller, "_isStableMagnifiedScopedAds", false);

                view.transform.localPosition = new Vector3(-9f, -8f, -7f);
                view.transform.localRotation = Quaternion.Euler(11f, 12f, 13f);

                InvokePrivateLateUpdate(helper);

                AssertLocalPose(
                    view.transform,
                    new Vector3(-9f, -8f, -7f),
                    Quaternion.Euler(11f, 12f, 13f));
            }
            finally
            {
#if UNITY_EDITOR
                Selection.activeObject = previousSelection;
#endif
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LateUpdate_StableScopedAdsWithoutPreview_DoesNotRewriteRootPose()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<PlayerWeaponController>();
            var view = new GameObject("WeaponView");
            view.transform.SetParent(root.transform, false);
            var helper = view.AddComponent<WeaponViewPoseTuningHelper>();
#if UNITY_EDITOR
            var previousSelection = Selection.activeObject;
#endif

            try
            {
                SetPrivateField(helper, "_weaponController", controller);
                SetPrivateField(helper, "_enabledInPlayMode", true);
                SetPrivateField(helper, "_targetWeaponItemId", "weapon-kar98k");
                SetPrivateField(helper, "_hipLocalPosition", new Vector3(0.25f, -0.5f, 1.5f));
                SetPrivateField(helper, "_hipLocalEuler", new Vector3(1f, 2f, 3f));
                SetPrivateField(helper, "_adsLocalPosition", new Vector3(4f, 5f, 6f));
                SetPrivateField(helper, "_adsLocalEuler", new Vector3(7f, 8f, 9f));
                SetPrivateField(helper, "_rifleLocalEulerOffset", Vector3.zero);
                SetPrivateField(helper, "_blendSpeed", 24f);
                SetPrivateField(controller, "_equippedItemId", "weapon-kar98k");
                SetPrivateField(controller, "_equippedWeaponView", view);
                SetPrivateField(controller, "_isStableMagnifiedScopedAds", true);

                view.transform.localPosition = new Vector3(-9f, -8f, -7f);
                view.transform.localRotation = Quaternion.Euler(11f, 12f, 13f);

                InvokePrivateLateUpdate(helper);

                AssertLocalPose(
                    view.transform,
                    new Vector3(-9f, -8f, -7f),
                    Quaternion.Euler(11f, 12f, 13f));

                view.transform.localPosition = new Vector3(2f, 3f, 4f);
                view.transform.localRotation = Quaternion.Euler(15f, 25f, 35f);

                InvokePrivateLateUpdate(helper);

                AssertLocalPose(
                    view.transform,
                    new Vector3(2f, 3f, 4f),
                    Quaternion.Euler(15f, 25f, 35f));
            }
            finally
            {
#if UNITY_EDITOR
                Selection.activeObject = previousSelection;
#endif
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LateUpdate_SelectedPreview_RewritesRootPoseFromResolvedPose()
        {
            var root = new GameObject("PlayerRoot");
            var controller = root.AddComponent<PlayerWeaponController>();
            var view = new GameObject("WeaponView");
            view.transform.SetParent(root.transform, false);
            var helper = view.AddComponent<WeaponViewPoseTuningHelper>();
#if UNITY_EDITOR
            var previousSelection = Selection.activeObject;
#endif

            try
            {
                SetPrivateField(helper, "_weaponController", controller);
                SetPrivateField(helper, "_enabledInPlayMode", true);
                SetPrivateField(helper, "_targetWeaponItemId", "weapon-kar98k");
                SetPrivateField(helper, "_hipLocalPosition", new Vector3(0.25f, -0.5f, 1.5f));
                SetPrivateField(helper, "_hipLocalEuler", new Vector3(1f, 2f, 3f));
                SetPrivateField(helper, "_adsLocalPosition", new Vector3(4f, 5f, 6f));
                SetPrivateField(helper, "_adsLocalEuler", new Vector3(7f, 8f, 9f));
                SetPrivateField(helper, "_rifleLocalEulerOffset", new Vector3(0f, 10f, 0f));
                SetPrivateField(helper, "_blendSpeed", 24f);
                SetPrivateField(controller, "_equippedItemId", "weapon-kar98k");
                SetPrivateField(controller, "_equippedWeaponView", view);
                SetPrivateField(controller, "_isStableMagnifiedScopedAds", false);

                view.transform.localPosition = new Vector3(-9f, -8f, -7f);
                view.transform.localRotation = Quaternion.Euler(11f, 12f, 13f);
#if UNITY_EDITOR
                Selection.activeObject = helper;
                Selection.activeGameObject = view;
#endif

                InvokePrivateLateUpdate(helper);

                AssertLocalPose(
                    view.transform,
                    new Vector3(0.25f, -0.5f, 1.5f),
                    Quaternion.Euler(1f, 2f, 3f) * Quaternion.Euler(0f, 10f, 0f));
            }
            finally
            {
#if UNITY_EDITOR
                Selection.activeObject = previousSelection;
#endif
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertLocalPose(Transform target, Vector3 expectedPosition, Quaternion expectedRotation)
        {
            Assert.That(Vector3.Distance(target.localPosition, expectedPosition), Is.LessThanOrEqualTo(0.0001f));
            Assert.That(Quaternion.Angle(target.localRotation, expectedRotation), Is.LessThanOrEqualTo(0.0001f));
        }

        private static void InvokePrivateLateUpdate(WeaponViewPoseTuningHelper helper)
        {
            var method = typeof(WeaponViewPoseTuningHelper).GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected WeaponViewPoseTuningHelper LateUpdate to exist.");
            method!.Invoke(helper, null);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field!.SetValue(instance, value);
        }
    }

    public sealed class RenderTextureScopeControllerReticleOffsetTests
    {
        [Test]
        public void SetScopeActive_RenderTexturePipWithoutExplicitProfile_UsesNativeBaselinePercentScaling()
        {
            var controllerType = System.Type.GetType("Reloader.Game.Weapons.RenderTextureScopeController, Reloader.Game.Weapons");
            var opticDefinitionType = System.Type.GetType("Reloader.Game.Weapons.OpticDefinition, Reloader.Game.Weapons");
            var adsVisualModeType = System.Type.GetType("Reloader.Game.Weapons.AdsVisualMode, Reloader.Game.Weapons");
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            var controllerGo = new GameObject("AdaptiveResolutionTest");
            var scopeCameraGo = new GameObject("ScopeCamera");
            var controller = controllerGo.AddComponent(controllerType!);
            var scopeCamera = scopeCameraGo.AddComponent<Camera>();
            var opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType!);

            try
            {
                SetPrivateField(controller, "_scopeCamera", scopeCamera);
                SetPrivateField(opticDefinition, "_visualModePolicy", System.Enum.Parse(adsVisualModeType!, "RenderTexturePiP"));
                SetPrivateField(opticDefinition, "_isVariableZoom", true);
                SetPrivateField(opticDefinition, "_magnificationMin", 5f);
                SetPrivateField(opticDefinition, "_magnificationMax", 25f);
                SetPrivateField(opticDefinition, "_magnificationStep", 1f);

                var setScopeActiveMethod = controllerType!.GetMethod("SetScopeActive", BindingFlags.Instance | BindingFlags.Public);
                var setScopedPipResolutionPercentMethod = controllerType.GetMethod("SetScopedPipResolutionPercent", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(setScopeActiveMethod, Is.Not.Null);
                Assert.That(setScopedPipResolutionPercentMethod, Is.Not.Null);

                var nativeSquareBaseline = Mathf.Max(Screen.width, Screen.height);
                if (nativeSquareBaseline <= 0)
                {
                    nativeSquareBaseline = 1024;
                }

                setScopedPipResolutionPercentMethod!.Invoke(controller, new object[] { 100 });
                setScopeActiveMethod!.Invoke(controller, new object[] { true, opticDefinition, null, 60f, 10f, 0, 0 });
                var mediumResolution = scopeCamera.targetTexture != null ? scopeCamera.targetTexture.width : 0;

                setScopedPipResolutionPercentMethod.Invoke(controller, new object[] { 10 });
                setScopeActiveMethod.Invoke(controller, new object[] { true, opticDefinition, null, 60f, 10f, 0, 0 });
                var lowResolution = scopeCamera.targetTexture != null ? scopeCamera.targetTexture.width : 0;

                setScopedPipResolutionPercentMethod.Invoke(controller, new object[] { 400 });
                setScopeActiveMethod.Invoke(controller, new object[] { true, opticDefinition, null, 60f, 10f, 0, 0 });
                var highResolution = scopeCamera.targetTexture != null ? scopeCamera.targetTexture.width : 0;

                Assert.That(mediumResolution, Is.EqualTo(nativeSquareBaseline));
                Assert.That(lowResolution, Is.EqualTo(Mathf.Clamp(Mathf.CeilToInt(nativeSquareBaseline * 0.1f), 256, 8192)));
                Assert.That(highResolution, Is.EqualTo(Mathf.Clamp(nativeSquareBaseline * 4, 256, 8192)));
            }
            finally
            {
                if (scopeCamera != null)
                {
                    scopeCamera.targetTexture = null;
                }

                Object.DestroyImmediate(opticDefinition);
                Object.DestroyImmediate(controllerGo);
                Object.DestroyImmediate(scopeCameraGo);
            }
        }

        [Test]
        public void SetScopeActive_ExplicitRenderProfile_PreservesAuthoredResolution()
        {
            var controllerType = System.Type.GetType("Reloader.Game.Weapons.RenderTextureScopeController, Reloader.Game.Weapons");
            var opticDefinitionType = System.Type.GetType("Reloader.Game.Weapons.OpticDefinition, Reloader.Game.Weapons");
            var adsVisualModeType = System.Type.GetType("Reloader.Game.Weapons.AdsVisualMode, Reloader.Game.Weapons");
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            var controllerGo = new GameObject("ProfileResolutionTest");
            var scopeCameraGo = new GameObject("ScopeCamera");
            var controller = controllerGo.AddComponent(controllerType!);
            var scopeCamera = scopeCameraGo.AddComponent<Camera>();
            var opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType!);

            try
            {
                SetPrivateField(controller, "_scopeCamera", scopeCamera);
                SetPrivateField(opticDefinition, "_visualModePolicy", System.Enum.Parse(adsVisualModeType!, "RenderTexturePiP"));
                SetPrivateField(opticDefinition, "_hasScopeRenderProfile", true);
                SetPrivateField(opticDefinition, "_scopeRenderProfile", CreateScopeRenderProfile(opticDefinitionType!, 1536, 20f));

                var setScopeActiveMethod = controllerType!.GetMethod("SetScopeActive", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(setScopeActiveMethod, Is.Not.Null);

                setScopeActiveMethod!.Invoke(controller, new object[] { true, opticDefinition, null, 60f, 10f, 0, 0 });

                Assert.That(scopeCamera.targetTexture, Is.Not.Null);
                Assert.That(scopeCamera.targetTexture!.width, Is.EqualTo(1536));
                Assert.That(scopeCamera.targetTexture.height, Is.EqualTo(1536));
            }
            finally
            {
                if (scopeCamera != null)
                {
                    scopeCamera.targetTexture = null;
                }

                Object.DestroyImmediate(opticDefinition);
                Object.DestroyImmediate(controllerGo);
                Object.DestroyImmediate(scopeCameraGo);
            }
        }

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

        private static object CreateScopeRenderProfile(System.Type opticDefinitionType, int renderTextureResolution, float scopeCameraFov)
        {
            var renderProfileType = opticDefinitionType.GetNestedType("ScopeRenderProfile", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(renderProfileType, Is.Not.Null);

            var profile = System.Activator.CreateInstance(renderProfileType!);
            SetPrivateField(profile!, "_renderTextureResolution", renderTextureResolution);
            SetPrivateField(profile, "_scopeCameraFov", scopeCameraFov);
            return profile;
        }
    }
}
