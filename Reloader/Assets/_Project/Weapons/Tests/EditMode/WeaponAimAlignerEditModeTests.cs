using System;
using System.Linq;
using System.Reflection;
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
                var screenMask = root.AddComponent(screenMaskType) as UnityEngine.Behaviour;
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

                Assert.That(screenMask, Is.Not.Null);
                Assert.That(screenMask!.enabled, Is.False, "PeripheralScopeEffects should no longer enable a sibling screen-mask fallback.");
                Assert.That((bool)isActiveProperty.GetValue(null), Is.True, "PeripheralScopeEffects should still publish blur runtime state.");
                Assert.That((float)blurAmountProperty.GetValue(null), Is.GreaterThan(0f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PeripheralScopeEffects_EmptyScopedBehaviours_PreserveExistingBlurAperture()
        {
            var effectsType = ResolveType("Reloader.Game.Weapons.PeripheralScopeEffects");
            var runtimeStateType = ResolveType("Reloader.Game.Weapons.Rendering.PeripheralScopeBlurRuntimeState");

            Assert.That(effectsType, Is.Not.Null);
            Assert.That(runtimeStateType, Is.Not.Null);

            var updateApertureMethod = runtimeStateType!.GetMethod("UpdateAperture", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var centerXProperty = runtimeStateType.GetProperty("CenterXNormalized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var centerYProperty = runtimeStateType.GetProperty("CenterYNormalized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var widthProperty = runtimeStateType.GetProperty("CenterWidthNormalized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var heightProperty = runtimeStateType.GetProperty("CenterHeightNormalized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var resetMethod = runtimeStateType.GetMethod("Reset", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            Assert.That(updateApertureMethod, Is.Not.Null, "Expected scoped blur runtime state to expose authored aperture updates.");
            Assert.That(centerXProperty, Is.Not.Null, "Expected scoped blur runtime state to publish the authored aperture center X.");
            Assert.That(centerYProperty, Is.Not.Null, "Expected scoped blur runtime state to publish the authored aperture center Y.");
            Assert.That(widthProperty, Is.Not.Null);
            Assert.That(heightProperty, Is.Not.Null);
            Assert.That(resetMethod, Is.Not.Null);

            var root = new GameObject("PeripheralScopeEffectsRoot_PreserveAperture");

            try
            {
                var effects = root.AddComponent(effectsType);
                SetField(effects, "_scopedBehaviours", Array.Empty<Behaviour>());

                resetMethod!.Invoke(null, null);
                updateApertureMethod!.Invoke(null, new object[] { 0.73f, 0.41f, 0.18f, 0.12f, 0.02f });

                Invoke(effects, "SetState", true, 1f, 1f);

                Assert.That((float)centerXProperty!.GetValue(null), Is.EqualTo(0.73f).Within(0.0001f),
                    "PeripheralScopeEffects should preserve the authored lens aperture center instead of restoring a centered square fallback.");
                Assert.That((float)centerYProperty!.GetValue(null), Is.EqualTo(0.41f).Within(0.0001f));
                Assert.That((float)widthProperty!.GetValue(null), Is.EqualTo(0.18f).Within(0.0001f));
                Assert.That((float)heightProperty!.GetValue(null), Is.EqualTo(0.12f).Within(0.0001f));
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

        [Test]
        public void WeaponAimAligner_AlignNow_AppliesAdditiveScopedEyeReliefAuthoring()
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

            var root = new GameObject("WeaponAimAlignerRoot_AdditiveEyeRelief");
            var viewRoot = new GameObject("ViewRoot");
            viewRoot.transform.SetParent(root.transform, false);
            var adsPivot = new GameObject("AdsPivot").transform;
            adsPivot.SetParent(viewRoot.transform, false);

            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(viewRoot.transform, false);
            var ironSightAnchor = new GameObject("IronSightAnchor").transform;
            ironSightAnchor.SetParent(viewRoot.transform, false);

            var mounts = viewRoot.AddComponent(viewMountsType);
            SetField(mounts, "_adsPivot", adsPivot);
            SetField(mounts, "_ironSightAnchor", ironSightAnchor);
            SetField(mounts, "_scopedAdsEyeReliefBackOffset", 0.015f);

            var manager = viewRoot.AddComponent(attachmentManagerType);
            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironSightAnchor);

            var worldCameraGo = new GameObject("WorldCamera");
            worldCameraGo.tag = "MainCamera";
            var worldCamera = worldCameraGo.AddComponent<Camera>();
            worldCamera.transform.position = new Vector3(4f, 2f, -3f);
            worldCamera.transform.rotation = Quaternion.Euler(8f, 24f, 0f);

            var viewmodelCameraGo = new GameObject("ViewmodelCamera");
            var viewmodelCamera = viewmodelCameraGo.AddComponent<Camera>();

            var adsState = root.AddComponent(adsStateControllerType);
            SetField(adsState, "_worldCamera", worldCamera);
            SetField(adsState, "_viewmodelCamera", viewmodelCamera);
            SetField(adsState, "_attachmentManager", manager);
            SetField(adsState, "_useLegacyInput", false);
            SetField(adsState, "_allowExternalAdsControl", true);

            var aligner = root.AddComponent(alignerType);

            var opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
            SetField(opticDefinition, "_opticId", "scope-aligner-additive-eye-relief");
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
                Assert.That(SightAnchorMatchesCameraEyeRelief(worldCamera.transform, activeSightAnchor, 0.052f), Is.True,
                    "WeaponAimAligner should add the view-authored scoped eye-relief correction on top of the optic baseline eye relief.");
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

        [Test]
        public void WeaponAimAligner_AlignNow_WhenAdsInactive_DoesNotRewriteHipBaseline()
        {
            var alignerType = ResolveType("Reloader.Weapons.Runtime.WeaponAimAligner");
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var viewMountsType = ResolveType("Reloader.Weapons.Runtime.WeaponViewAttachmentMounts");

            Assert.That(alignerType, Is.Not.Null);
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(viewMountsType, Is.Not.Null);

            var root = new GameObject("WeaponAimAlignerHipRoot");
            var viewRoot = new GameObject("ViewRoot");
            viewRoot.transform.SetParent(root.transform, false);
            var adsPivot = new GameObject("AdsPivot").transform;
            adsPivot.SetParent(viewRoot.transform, false);
            adsPivot.localPosition = Vector3.zero;
            adsPivot.localRotation = Quaternion.identity;

            var ironSightAnchor = new GameObject("IronSightAnchor").transform;
            ironSightAnchor.SetParent(viewRoot.transform, false);

            var mounts = viewRoot.AddComponent(viewMountsType);
            SetField(mounts, "_adsPivot", adsPivot);
            SetField(mounts, "_ironSightAnchor", ironSightAnchor);
            SetField(mounts, "_hasScopedPoseAuthoring", true);
            SetField(mounts, "_scopedHipLocalPosition", new Vector3(0.015f, 0.13f, 0.005f));
            SetField(mounts, "_scopedAdsLocalPosition", new Vector3(0f, 0.2f, 0.05f));

            var manager = viewRoot.AddComponent(attachmentManagerType);
            SetField(manager, "_ironSightAnchor", ironSightAnchor);

            var worldCameraGo = new GameObject("WorldCamera");
            var worldCamera = worldCameraGo.AddComponent<Camera>();
            var adsState = root.AddComponent(adsStateControllerType);
            var aligner = root.AddComponent(alignerType);

            try
            {
                Invoke(aligner, "BindRuntimeReferences", worldCamera, manager, adsState, mounts);

                adsPivot.localPosition = new Vector3(-0.12f, 0.04f, 0.27f);
                adsPivot.localRotation = Quaternion.Euler(8f, -14f, 3f);

                Invoke(aligner, "AlignNow");

                Assert.That(adsPivot.localPosition, Is.EqualTo(new Vector3(-0.12f, 0.04f, 0.27f)),
                    "HIP contract should stay on the spawned mount space owner, not be rewritten by WeaponAimAligner.");
                Assert.That(Quaternion.Angle(adsPivot.localRotation, Quaternion.Euler(8f, -14f, 3f)), Is.LessThan(0.0001f),
                    "WeaponAimAligner should not restore a cached HIP rotation while ADS is inactive.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(worldCameraGo);
            }
        }

        [Test]
        public void WeaponAimAligner_AlignNow_AfterAdsExit_RestoresCanonicalHipPoseOnce()
        {
            var alignerType = ResolveType("Reloader.Weapons.Runtime.WeaponAimAligner");
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var viewMountsType = ResolveType("Reloader.Weapons.Runtime.WeaponViewAttachmentMounts");

            Assert.That(alignerType, Is.Not.Null);
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(viewMountsType, Is.Not.Null);

            var root = new GameObject("WeaponAimAlignerAdsExitRoot");
            var viewRoot = new GameObject("ViewRoot");
            viewRoot.transform.SetParent(root.transform, false);
            var adsPivot = new GameObject("AdsPivot").transform;
            adsPivot.SetParent(viewRoot.transform, false);
            adsPivot.localPosition = Vector3.zero;
            adsPivot.localRotation = Quaternion.identity;

            var ironSightAnchor = new GameObject("IronSightAnchor").transform;
            ironSightAnchor.SetParent(adsPivot, false);

            var mounts = viewRoot.AddComponent(viewMountsType);
            SetField(mounts, "_adsPivot", adsPivot);
            SetField(mounts, "_ironSightAnchor", ironSightAnchor);
            SetField(mounts, "_hasScopedPoseAuthoring", false);

            var manager = viewRoot.AddComponent(attachmentManagerType);
            SetField(manager, "_ironSightAnchor", ironSightAnchor);
            Invoke(manager, "ConfigureMounts", null, ironSightAnchor, null, null);

            var worldCameraGo = new GameObject("WorldCamera");
            var worldCamera = worldCameraGo.AddComponent<Camera>();
            worldCamera.transform.position = new Vector3(1f, 2f, -3f);
            worldCamera.transform.rotation = Quaternion.Euler(4f, 16f, 0f);

            var adsState = root.AddComponent(adsStateControllerType);
            SetField(adsState, "_allowExternalAdsControl", true);
            var aligner = root.AddComponent(alignerType);

            try
            {
                Invoke(aligner, "BindRuntimeReferences", worldCamera, manager, adsState, mounts);

                Invoke(adsState, "SetAdsHeld", true);
                Invoke(aligner, "AlignNow");

                adsPivot.localPosition = new Vector3(0.41f, -0.22f, 0.18f);
                adsPivot.localRotation = Quaternion.Euler(-6f, 9f, 2f);

                Invoke(adsState, "SetAdsHeld", false);
                Invoke(aligner, "AlignNow");

                Assert.That(Vector3.Distance(adsPivot.localPosition, Vector3.zero), Is.LessThan(0.0001f),
                    "Leaving ADS should restore the canonical HIP baseline once.");
                Assert.That(Quaternion.Angle(adsPivot.localRotation, Quaternion.identity), Is.LessThan(0.0001f));

                adsPivot.localPosition = new Vector3(-0.12f, 0.04f, 0.27f);
                adsPivot.localRotation = Quaternion.Euler(8f, -14f, 3f);

                Invoke(aligner, "AlignNow");

                Assert.That(adsPivot.localPosition, Is.EqualTo(new Vector3(-0.12f, 0.04f, 0.27f)),
                    "WeaponAimAligner should stop writing HIP once the ADS exit handoff is complete.");
                Assert.That(Quaternion.Angle(adsPivot.localRotation, Quaternion.Euler(8f, -14f, 3f)), Is.LessThan(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(worldCameraGo);
            }
        }

        [Test]
        public void RifleViewPrefab_UsesIdentityMountSpaceRootAndScopedHipBaseline()
        {
            var viewMountsType = ResolveType("Reloader.Weapons.Runtime.WeaponViewAttachmentMounts");
            Assert.That(viewMountsType, Is.Not.Null);

            var rifleViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Weapons/Prefabs/RifleView.prefab");
            Assert.That(rifleViewPrefab, Is.Not.Null);

            Assert.That(Vector3.Distance(rifleViewPrefab!.transform.localPosition, Vector3.zero), Is.LessThan(0.0001f),
                "RifleView root should spawn at identity in mount space so there is only one HIP baseline.");
            Assert.That(Quaternion.Angle(rifleViewPrefab.transform.localRotation, Quaternion.identity), Is.LessThan(0.0001f),
                "RifleView root rotation should stay identity in mount space.");

            var mounts = rifleViewPrefab.GetComponent(viewMountsType);
            Assert.That(mounts, Is.Not.Null);

            var adsPivot = GetPropertyValue<Transform>(mounts, "AdsPivot");
            Assert.That(adsPivot, Is.Not.Null);
            Assert.That(Vector3.Distance(adsPivot!.localPosition, Vector3.zero), Is.LessThan(0.0001f),
                "AdsPivot should remain an identity seam under the prefab root.");
            Assert.That(Quaternion.Angle(adsPivot.localRotation, Quaternion.identity), Is.LessThan(0.0001f));

            Assert.That(GetPropertyValue<bool>(mounts, "HasScopedPoseAuthoring"), Is.True);
            Assert.That(Vector3.Distance((Vector3)GetFieldValue(mounts, "_scopedHipLocalPosition"), Vector3.zero), Is.LessThan(0.0001f),
                "Scoped HIP authoring should no longer duplicate the mount-space carry offset.");
            Assert.That(Vector3.Distance((Vector3)GetFieldValue(mounts, "_scopedAdsLocalPosition"), new Vector3(0f, 0.2f, 0.05f)), Is.LessThan(0.0001f),
                "Authored ADS alignment path should stay intact.");
            Assert.That((float)GetFieldValue(mounts, "_scopedAdsEyeReliefBackOffset"), Is.EqualTo(0.0425f).Within(0.0001f),
                "Scoped eye-relief authoring should remain on the live rifle view so the aligner can apply the additive correction at runtime.");
        }

        [Test]
        public void PlayerRootPrefab_AuthorsExplicitStaticWeaponPresentationMount()
        {
            var playerRootPrefab = PrefabUtility.LoadPrefabContents("Assets/_Project/Player/Prefabs/PlayerRoot.prefab");

            try
            {
                Assert.That(playerRootPrefab, Is.Not.Null);

                var weaponPresentationMount = playerRootPrefab!.transform.Find("CameraPivot/WeaponPresentationMount");
                Assert.That(weaponPresentationMount, Is.Not.Null,
                    "PlayerRoot should author an explicit static WeaponPresentationMount used by the runtime driver.");
                Assert.That(weaponPresentationMount!.parent, Is.SameAs(playerRootPrefab.transform.Find("CameraPivot")),
                    "The live PlayerRoot mount should be a direct authored child of CameraPivot instead of the animated armature chain.");
                Assert.That(weaponPresentationMount.localPosition, Is.EqualTo(new Vector3(0.02542634f, -0.08720852f, 0.17778906f)).Within(0.0001f),
                    "The static weapon presentation mount should preserve the measured hip pose offset instead of snapping to the camera pivot origin.");
                Assert.That(Quaternion.Angle(weaponPresentationMount.localRotation, new Quaternion(0.7087967f, 0.0026341488f, -0.0000001728f, -0.7054079f)), Is.LessThan(0.01f),
                    "The static weapon presentation mount should preserve the measured hip pose rotation so the rifle no longer hangs vertically.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRootPrefab);
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

        private static object GetFieldValue(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on '{target.GetType().FullName}'.");
            return field!.GetValue(target);
        }

        private static T GetPropertyValue<T>(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{target.GetType().FullName}'.");
            return (T)property!.GetValue(target);
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            var methods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(candidate => candidate.Name == methodName && ParametersMatch(candidate.GetParameters(), parameters))
                .ToArray();

            Assert.That(methods.Length, Is.EqualTo(1),
                $"Expected exactly one matching overload for method '{methodName}' on '{target.GetType().FullName}', but found {methods.Length}.");

            var method = methods[0];
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on '{target.GetType().FullName}'.");
            return method!.Invoke(target, parameters);
        }

        private static bool ParametersMatch(ParameterInfo[] methodParameters, object[] providedParameters)
        {
            if (methodParameters.Length != providedParameters.Length)
            {
                return false;
            }

            for (var i = 0; i < methodParameters.Length; i++)
            {
                var providedParameter = providedParameters[i];
                var expectedType = methodParameters[i].ParameterType;
                if (providedParameter == null)
                {
                    if (expectedType.IsValueType && Nullable.GetUnderlyingType(expectedType) == null)
                    {
                        return false;
                    }

                    continue;
                }

                var providedType = providedParameter.GetType();
                if (!expectedType.IsAssignableFrom(providedType))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
