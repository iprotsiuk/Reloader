using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Reloader.Weapons.Tests.EditMode
{
    public sealed class WeaponAimAlignerEditModeTests
    {
        [Test]
        public void PeripheralScopeEffects_EmptyScopedBehaviours_DoNotActivateSiblingScreenMask()
        {
            var effectsType = ResolveType("Reloader.Game.Weapons.PeripheralScopeEffects");
            var screenMaskType = ResolveType("Reloader.Game.Weapons.PeripheralScopeScreenMask");
            var runtimeStateType = ResolveType("Reloader.Game.Weapons.Rendering.PeripheralScopeBlurRuntimeState");

            Assert.That(effectsType, Is.Not.Null);
            Assert.That(screenMaskType, Is.Not.Null);
            Assert.That(runtimeStateType, Is.Not.Null);

            var root = new GameObject("PeripheralScopeEffectsRoot");

            try
            {
                var effects = root.AddComponent(effectsType);
                var screenMask = root.AddComponent(screenMaskType);
                SetField(effects, "_scopedBehaviours", Array.Empty<Behaviour>());

                var isActiveProperty = runtimeStateType!.GetProperty("IsActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var blurAmountProperty = runtimeStateType.GetProperty("BlurAmount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var resetMethod = runtimeStateType.GetMethod("Reset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                Assert.That(isActiveProperty, Is.Not.Null);
                Assert.That(blurAmountProperty, Is.Not.Null);
                Assert.That(resetMethod, Is.Not.Null);

                resetMethod!.Invoke(null, null);
                Assert.That((bool)isActiveProperty!.GetValue(null), Is.False);
                Assert.That((float)blurAmountProperty!.GetValue(null), Is.EqualTo(0f).Within(0.0001f));

                Invoke(effects, "SetState", true, 1f, 1f);

                Assert.That(((Behaviour)screenMask).enabled, Is.False, "PeripheralScopeEffects should no longer enable a sibling screen-mask fallback.");
                Assert.That((bool)isActiveProperty.GetValue(null), Is.True, "PeripheralScopeEffects should still publish blur runtime state.");
                Assert.That((float)blurAmountProperty.GetValue(null), Is.GreaterThan(0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WeaponAimAligner_AlignNow_SolvesActiveSightAnchorToCameraEyeRelief()
        {
            var alignerType = ResolveType("Reloader.Weapons.Runtime.WeaponAimAligner");
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var viewMountsType = ResolveType("Reloader.Weapons.Runtime.WeaponViewAttachmentMounts");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");

            Assert.That(alignerType, Is.Not.Null);
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(viewMountsType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            var root = new GameObject("WeaponAimAlignerRoot");
            var viewRoot = new GameObject("ViewRoot");
            viewRoot.transform.SetParent(root.transform, false);
            var adsPivot = new GameObject("AdsPivot").transform;
            adsPivot.SetParent(viewRoot.transform, false);
            adsPivot.localPosition = new Vector3(0.015f, 0.15f, 0.005f);
            adsPivot.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(viewRoot.transform, false);
            scopeSlot.localPosition = new Vector3(0.08f, -0.04f, 0.36f);
            scopeSlot.localRotation = Quaternion.Euler(2f, -3f, 1f);

            var ironSightAnchor = new GameObject("IronSightAnchor").transform;
            ironSightAnchor.SetParent(viewRoot.transform, false);
            ironSightAnchor.localPosition = new Vector3(0f, 0.085f, 0.24f);

            var mounts = viewRoot.AddComponent(viewMountsType);
            SetField(mounts, "_adsPivot", adsPivot);
            SetField(mounts, "_ironSightAnchor", ironSightAnchor);
            SetField(mounts, "_hasScopedPoseAuthoring", true);
            SetField(mounts, "_scopedHipLocalPosition", new Vector3(0.015f, 0.13f, 0.005f));
            SetField(mounts, "_scopedAdsLocalPosition", new Vector3(0f, 0.2f, 0.05f));

            var manager = viewRoot.AddComponent(attachmentManagerType);
            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironSightAnchor);

            var worldCameraGo = new GameObject("WorldCamera");
            worldCameraGo.tag = "MainCamera";
            var worldCamera = worldCameraGo.AddComponent<Camera>();
            worldCamera.transform.position = new Vector3(10f, 4f, -6f);
            worldCamera.transform.rotation = Quaternion.Euler(11f, 32f, 0f);

            var viewmodelCameraGo = new GameObject("ViewmodelCamera");
            var viewmodelCamera = viewmodelCameraGo.AddComponent<Camera>();

            var adsState = root.AddComponent(adsStateControllerType);
            SetField(adsState, "_worldCamera", worldCamera);
            SetField(adsState, "_viewmodelCamera", viewmodelCamera);
            SetField(adsState, "_attachmentManager", manager);
            SetField(adsState, "_useLegacyInput", false);
            SetField(adsState, "_allowExternalAdsControl", true);
            SetField(adsState, "_allowExternalZoomControl", true);

            var aligner = root.AddComponent(alignerType);

            var opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
            SetField(opticDefinition, "_opticId", "scope-aligner-test");
            SetField(opticDefinition, "_magnificationMin", 4f);
            SetField(opticDefinition, "_magnificationMax", 8f);
            SetField(opticDefinition, "_magnificationStep", 1f);
            SetField(opticDefinition, "_visualModePolicy", Enum.Parse(adsVisualModeType, "RenderTexturePiP"));
            SetField(opticDefinition, "_eyeReliefBackOffset", 0.037f);

            var opticPrefab = new GameObject("OpticPrefab");
            var sightAnchor = new GameObject("SightAnchor").transform;
            sightAnchor.SetParent(opticPrefab.transform, false);
            sightAnchor.localPosition = new Vector3(0.013f, -0.009f, -0.041f);
            sightAnchor.localRotation = Quaternion.Euler(4f, -6f, 1.5f);
            SetField(opticDefinition, "_opticPrefab", opticPrefab);

            try
            {
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                Invoke(adsState, "SetAdsHeld", true);
                Invoke(aligner, "BindRuntimeReferences", worldCamera, manager, adsState, mounts);

                Invoke(aligner, "AlignNow");

                var activeSightAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
                Assert.That(activeSightAnchor, Is.Not.Null);
                Assert.That(SightAnchorMatchesCameraEyeRelief(worldCamera.transform, activeSightAnchor, 0.037f), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(worldCameraGo);
                UnityEngine.Object.DestroyImmediate(viewmodelCameraGo);
                UnityEngine.Object.DestroyImmediate(opticPrefab);
                UnityEngine.Object.DestroyImmediate(opticDefinition);
            }
        }

        private static bool SightAnchorMatchesCameraEyeRelief(Transform cameraTransform, Transform sightAnchor, float expectedEyeRelief)
        {
            if (cameraTransform == null || sightAnchor == null)
            {
                return false;
            }

            var expectedPosition = cameraTransform.position - (cameraTransform.forward * expectedEyeRelief);
            return Vector3.Distance(sightAnchor.position, expectedPosition) <= 0.0001f
                && Quaternion.Angle(sightAnchor.rotation, cameraTransform.rotation) <= 0.05f;
        }

        private static Type ResolveType(string typeName)
        {
            return Type.GetType(typeName) ?? AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, false))
                .FirstOrDefault(type => type != null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on '{target.GetType().FullName}'.");
            field!.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on '{target.GetType().FullName}'.");
            return method!.Invoke(target, parameters);
        }
    }
}
