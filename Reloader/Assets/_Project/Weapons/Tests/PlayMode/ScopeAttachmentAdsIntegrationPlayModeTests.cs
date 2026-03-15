using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Weapons.Tests.PlayMode
{
    public partial class ScopeAttachmentAdsIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator EquipOptic_RealKar98kAsset_MountsWithoutEditorFallbackAndResolvesSightAnchor()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var scopeReticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(scopeReticleControllerType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var root = new GameObject("AttachmentRoot");
            var manager = root.AddComponent(attachmentManagerType);
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(root.transform, false);
            var ironAnchor = new GameObject("IronSightAnchor").transform;
            ironAnchor.SetParent(root.transform, false);

            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironAnchor);

            var warnings = new List<string>();
            void CaptureLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Warning)
                {
                    warnings.Add(condition ?? string.Empty);
                }
            }

            Application.logMessageReceived += CaptureLog;
            try
            {
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);

                var activeAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeAnchor, Is.Not.Null);
                Assert.That(activeOpticInstance, Is.Not.Null);
                Assert.That(activeAnchor.name, Is.EqualTo("SightAnchor"), "Real Kar98k optic should resolve an authored sight anchor.");
                Assert.That(scopeSlot.childCount, Is.EqualTo(1));
                Assert.That(activeAnchor, Is.Not.EqualTo(scopeSlot.GetChild(0)), "Sight anchor should not fall back to optic root.");
                Assert.That(
                    activeAnchor.localPosition.sqrMagnitude,
                    Is.GreaterThan(0.0001f),
                    "Real Kar98k optic should author a specific eye-box sight anchor instead of relying on a synthetic root anchor.");
                Assert.That(GetComponentInChildren(activeOpticInstance, scopeLensDisplayType), Is.Not.Null, "Real Kar98k optic should expose a ScopeLensDisplay for PiP rendering.");
                Assert.That(GetComponentInChildren(activeOpticInstance, scopeReticleControllerType), Is.Not.Null, "Real Kar98k optic should expose a ScopeReticleController for PiP reticle rendering.");
                Assert.That(
                    warnings.Exists(message => message.Contains("Used editor fallback optic prefab", StringComparison.Ordinal)),
                    Is.False,
                    "Healthy real optic mount should not require editor fallback.");
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
                Cleanup(root);
            }

            yield return null;
        }

        [Test]
        public void RealKar98kOpticAsset_ReticleDefinition_IsFfpAndBindsAuthoredSprite()
        {
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var scopeReticleDefinition = GetProperty(opticDefinition, "ScopeReticleDefinition") as ScriptableObject;
            Assert.That(scopeReticleDefinition, Is.Not.Null, "Expected the real Kar98k optic to bind a ScopeReticleDefinition.");

            var reticleSprite = GetProperty(scopeReticleDefinition, "ReticleSprite") as Sprite;
            Assert.That(reticleSprite, Is.Not.Null, "Expected the real Kar98k reticle definition to bind an authored reticle sprite.");

            var reticleMode = GetProperty(scopeReticleDefinition, "Mode");
            Assert.That(reticleMode, Is.Not.Null);
            Assert.That(reticleMode!.ToString(), Is.EqualTo("Ffp"), "Expected the Kar98k reticle definition to be authored as FFP.");
        }

        [Test]
        public void RealKar98kOpticAsset_UsesCurrentTunedCalibrationDefaults()
        {
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            Assert.That((float)GetProperty(opticDefinition, "EyeReliefBackOffset"), Is.EqualTo(0.012f).Within(0.0001f));
            Assert.That((float)GetProperty(opticDefinition, "MagnificationMin"), Is.EqualTo(5f).Within(0.0001f));
            Assert.That((float)GetProperty(opticDefinition, "MagnificationMax"), Is.EqualTo(25f).Within(0.0001f));
            Assert.That((float)GetProperty(opticDefinition, "MradPerClick"), Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That((int)GetProperty(opticDefinition, "MinWindageClicks"), Is.EqualTo(-60));
            Assert.That((int)GetProperty(opticDefinition, "MaxWindageClicks"), Is.EqualTo(60));
            Assert.That((int)GetProperty(opticDefinition, "MinElevationClicks"), Is.EqualTo(-60));
            Assert.That((int)GetProperty(opticDefinition, "MaxElevationClicks"), Is.EqualTo(60));
            Assert.That((Vector2)GetProperty(opticDefinition, "MechanicalZeroOffsetMrad"), Is.EqualTo(Vector2.zero));
            Assert.That((float)GetProperty(opticDefinition, "ProjectionCalibrationMultiplier"), Is.EqualTo(6.3f).Within(0.0001f));
            Assert.That((float)GetProperty(opticDefinition, "CompositeReticleScale"), Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That((Vector2)GetProperty(opticDefinition, "CompositeReticleOffset"), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void PlayerWeaponController_ScopedAdjustmentKeyMapping_UsesExpectedClickDirections()
        {
            var controllerType = ResolveType("Reloader.Weapons.Controllers.PlayerWeaponController");
            Assert.That(controllerType, Is.Not.Null);

            var resolveMethod = controllerType!.GetMethod(
                "ResolveScopedAdjustmentClicks",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null, "Expected PlayerWeaponController to expose a scoped-adjustment mapping helper.");

            var minusUnshiftedArgs = new object[] { false, true, false, 0, 0 };
            Assert.That((bool)resolveMethod!.Invoke(null, minusUnshiftedArgs), Is.True);
            Assert.That((int)minusUnshiftedArgs[3], Is.EqualTo(0));
            Assert.That((int)minusUnshiftedArgs[4], Is.EqualTo(-1), "Expected unshifted '-' to lower elevation clicks.");

            var equalsUnshiftedArgs = new object[] { false, false, true, 0, 0 };
            Assert.That((bool)resolveMethod.Invoke(null, equalsUnshiftedArgs), Is.True);
            Assert.That((int)equalsUnshiftedArgs[3], Is.EqualTo(0));
            Assert.That((int)equalsUnshiftedArgs[4], Is.EqualTo(1), "Expected unshifted '=' to raise elevation clicks.");

            var minusShiftedArgs = new object[] { true, true, false, 0, 0 };
            Assert.That((bool)resolveMethod.Invoke(null, minusShiftedArgs), Is.True);
            Assert.That((int)minusShiftedArgs[3], Is.EqualTo(-1), "Expected shifted '-' to lower windage clicks.");
            Assert.That((int)minusShiftedArgs[4], Is.EqualTo(0));

            var equalsShiftedArgs = new object[] { true, false, true, 0, 0 };
            Assert.That((bool)resolveMethod.Invoke(null, equalsShiftedArgs), Is.True);
            Assert.That((int)equalsShiftedArgs[3], Is.EqualTo(1), "Expected shifted '=' to raise windage clicks.");
            Assert.That((int)equalsShiftedArgs[4], Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_PipReticle_CompositesIntoScopeRenderPath()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var scopeReticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(scopeReticleControllerType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null);

            var root = new GameObject("RealKar98kReticleRoot");
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                scopeSlot.gameObject.layer = 23;
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                worldCamGo = new GameObject("WorldCam");
                var worldCamera = worldCamGo.AddComponent<Camera>();
                worldCamera.fieldOfView = 72f;

                viewmodelCamGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
                viewmodelCamera.fieldOfView = 55f;

                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();
                scopeCamera.fieldOfView = 20f;

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return null;
                yield return null;

                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                var reticleController = GetComponentInChildren(activeOpticInstance, scopeReticleControllerType);
                Assert.That(reticleController, Is.Not.Null);
                var spriteRenderer = ((Component)reticleController).GetComponent<SpriteRenderer>();
                Assert.That(spriteRenderer, Is.Not.Null);
                var currentReticleDefinition = GetProperty(reticleController, "CurrentReticleDefinition") as UnityEngine.Object;
                var currentReticleName = currentReticleDefinition != null ? currentReticleDefinition.name : "<null>";
                var compositeActive = GetProperty(scopeController, "IsCompositeReticleActive");
                var compositeReticleSprite = GetProperty(scopeController, "CurrentCompositeReticleSprite") as Sprite;
                var compositeSpriteName = compositeReticleSprite != null ? compositeReticleSprite.name : "<null>";

                Assert.That(compositeActive, Is.Not.Null, "Expected PiP scope controller to expose composite-reticle state.");
                Assert.That((bool)compositeActive!, Is.True,
                    $"Expected Kar98k PiP reticle to composite through the scope render path. compositeSprite={compositeSpriteName}");
                Assert.That(compositeReticleSprite, Is.Not.Null,
                    "Expected Kar98k PiP reticle composition to bind an authored sprite.");
                Assert.That(spriteRenderer.enabled, Is.False,
                    $"Expected world-space reticle sprite renderer to be disabled while PiP composite reticle is active. worldSprite={spriteRenderer.sprite}, currentDefinition={currentReticleName}");
                Assert.That(spriteRenderer.sprite, Is.Null,
                    "Expected PiP optic to clear the authored world-space reticle sprite to avoid leaking outside the lens.");
                Assert.That(currentReticleDefinition, Is.Null,
                    "Expected prefab reticle controller to be cleared while PiP composite reticle is active.");
            }
            finally
            {
                Cleanup(root, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ReticleSitsInFrontOfLensFromEyeBox()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            var scopeReticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null);
            Assert.That(scopeReticleControllerType, Is.Not.Null);
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("RealKar98kReticleDepthRoot");
            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);

                var sightAnchor = activeOpticInstance.transform.Find("SightAnchor");
                Assert.That(sightAnchor, Is.Not.Null);

                var reticleController = GetComponentInChildren(activeOpticInstance, scopeReticleControllerType);
                var lensDisplay = GetComponentInChildren(activeOpticInstance, scopeLensDisplayType);
                Assert.That(reticleController, Is.Not.Null);
                Assert.That(lensDisplay, Is.Not.Null);

                var eyeForward = sightAnchor.forward;
                var reticleDepth = Vector3.Dot(((Component)reticleController).transform.position - sightAnchor.position, eyeForward);
                var lensDepth = Vector3.Dot(((Component)lensDisplay).transform.position - sightAnchor.position, eyeForward);

                Assert.That(reticleDepth, Is.LessThan(lensDepth),
                    $"Expected reticle plane to sit closer to the eye box than the lens plane. reticleDepth={reticleDepth:F6}, lensDepth={lensDepth:F6}");
            }
            finally
            {
                Cleanup(root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ScopeAdjustmentController_UsesSixtyClickTravel()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null);

            var root = new GameObject("Kar98kScopeAdjustmentRangeRoot");
            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var adjustmentController = GetProperty(manager, "ActiveScopeAdjustmentController");
                Assert.That(adjustmentController, Is.Not.Null);
                Assert.That(adjustmentController.GetType(), Is.EqualTo(scopeAdjustmentControllerType));

                Invoke(adjustmentController, "ApplyWindageClicks", 999);
                Invoke(adjustmentController, "ApplyElevationClicks", 999);
                Assert.That((int)GetProperty(adjustmentController, "CurrentWindageClicks"), Is.EqualTo(60));
                Assert.That((int)GetProperty(adjustmentController, "CurrentElevationClicks"), Is.EqualTo(60));

                Invoke(adjustmentController, "ApplyWindageClicks", -1998);
                Invoke(adjustmentController, "ApplyElevationClicks", -1998);
                Assert.That((int)GetProperty(adjustmentController, "CurrentWindageClicks"), Is.EqualTo(-60));
                Assert.That((int)GetProperty(adjustmentController, "CurrentElevationClicks"), Is.EqualTo(-60));
            }
            finally
            {
                Cleanup(root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_EffectiveAdjustmentMrad_UsesAuthoredMechanicalZeroAndClickValue()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);

            var root = new GameObject("ScopedMradCalibrationRoot");
            ScriptableObject scopedOptic = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                worldCamGo = new GameObject("WorldCam");
                var worldCamera = worldCamGo.AddComponent<Camera>();
                viewmodelCamGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();
                scopeCamera.aspect = 1f;

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-mrad-calibration", 4f, 8f, true, "RenderTexturePiP");
                SetField(scopedOptic, "_mradPerClick", 0.1f);
                SetField(scopedOptic, "_mechanicalZeroOffsetMrad", new Vector2(0.25f, -0.5f));
                SetField(scopedOptic, "_projectionCalibrationMultiplier", 1f);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return WaitUntil(
                    () =>
                    {
                        var effective = (Vector2)GetProperty(scopeController, "CurrentEffectiveAdjustmentMrad");
                        return Mathf.Abs(effective.x - 0.25f) <= 0.0001f
                            && Mathf.Abs(effective.y + 0.5f) <= 0.0001f;
                    },
                    10,
                    "Expected authored mechanical zero offset to propagate into the active PiP scope projection.");

                var adjustmentController = GetProperty(manager, "ActiveScopeAdjustmentController");
                Assert.That(adjustmentController, Is.Not.Null);

                var effectiveAtZero = (Vector2)GetProperty(scopeController, "CurrentEffectiveAdjustmentMrad");
                Assert.That(effectiveAtZero.x, Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(effectiveAtZero.y, Is.EqualTo(-0.5f).Within(0.0001f));

                Invoke(adjustmentController, "ApplyWindageClicks", 10);
                Invoke(adjustmentController, "ApplyElevationClicks", -10);
                yield return WaitUntil(
                    () =>
                    {
                        var effective = (Vector2)GetProperty(scopeController, "CurrentEffectiveAdjustmentMrad");
                        return Mathf.Abs(effective.x - 1.25f) <= 0.0001f
                            && Mathf.Abs(effective.y + 1.5f) <= 0.0001f;
                    },
                    10,
                    "Expected scope adjustment clicks to update the active PiP calibration state.");

                var effectiveAdjusted = (Vector2)GetProperty(scopeController, "CurrentEffectiveAdjustmentMrad");
                Assert.That(effectiveAdjusted.x, Is.EqualTo(1.25f).Within(0.0001f),
                    "Expected 10 windage clicks at 0.1 MRAD/click to add exactly 1.0 MRAD.");
                Assert.That(effectiveAdjusted.y, Is.EqualTo(-1.5f).Within(0.0001f),
                    "Expected -10 elevation clicks at 0.1 MRAD/click to subtract exactly 1.0 MRAD.");
            }
            finally
            {
                Cleanup(root, scopedOptic, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_InspectorCalibrationOverrides_ApplyLiveDuringAds()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);

            var root = new GameObject("ScopedInspectorCalibrationRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                worldCamGo = new GameObject("WorldCam");
                var worldCamera = worldCamGo.AddComponent<Camera>();
                viewmodelCamGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();
                scopeCamera.aspect = 1f;
                scopeCamera.fieldOfView = 20f;

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-inspector-calibration", 4f, 8f, true, "RenderTexturePiP");
                reticleDefinition = CreateReticleDefinition("Ffp", 4f);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_mradPerClick", 0.1f);
                SetField(scopedOptic, "_mechanicalZeroOffsetMrad", new Vector2(0.1f, -0.2f));
                SetField(scopedOptic, "_projectionCalibrationMultiplier", 1f);
                SetField(scopedOptic, "_compositeReticleScale", 1f);
                SetField(scopedOptic, "_compositeReticleOffset", Vector2.zero);

                var opticPrefab = GetProperty(scopedOptic, "OpticPrefab") as GameObject;
                Assert.That(opticPrefab, Is.Not.Null);
                opticPrefab.AddComponent(scopeAdjustmentControllerType);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 8f);
                yield return null;
                yield return null;

                Assert.That((Vector2)GetProperty(scopeController, "CurrentEffectiveAdjustmentMrad"), Is.EqualTo(new Vector2(0.1f, -0.2f)));
                Assert.That((float)GetProperty(scopeController, "CurrentProjectionCalibrationMultiplier"), Is.EqualTo(1f).Within(0.0001f));

                SetField(scopeController, "_useInspectorCalibrationOverrides", true);
                SetField(scopeController, "_inspectorMechanicalZeroOffsetMrad", new Vector2(0.75f, -1.25f));
                SetField(scopeController, "_inspectorProjectionCalibrationMultiplier", 0.85f);
                SetField(scopeController, "_inspectorCompositeReticleScale", 0.9f);
                SetField(scopeController, "_inspectorCompositeReticleOffset", new Vector2(0.02f, -0.03f));
                yield return WaitUntil(
                    () =>
                    {
                        var effective = (Vector2)GetProperty(scopeController, "CurrentEffectiveAdjustmentMrad");
                        var projectionMultiplier = (float)GetProperty(scopeController, "CurrentProjectionCalibrationMultiplier");
                        var reticleScale = (float)GetProperty(scopeController, "CurrentCompositeReticleScale");
                        var reticleOffset = (Vector2)GetProperty(scopeController, "CurrentCompositeReticleOffset");
                        return Mathf.Abs(effective.x - 0.75f) <= 0.0001f
                            && Mathf.Abs(effective.y + 1.25f) <= 0.0001f
                            && Mathf.Abs(projectionMultiplier - 0.85f) <= 0.0001f
                            && Mathf.Abs(reticleScale - 1.8f) <= 0.0001f
                            && Vector2.Distance(reticleOffset, new Vector2(0.02f, -0.03f)) <= 0.0001f;
                    },
                    20,
                    "Inspector calibration overrides did not apply live while ADS was active.");
            }
            finally
            {
                Cleanup(root, scopedOptic, reticleDefinition, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator EquipOptic_RenderTexturePipOpticWithoutAuthoredSightAnchor_FailsWithoutSyntheticFallback()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            Assert.That(attachmentManagerType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            ScriptableObject opticDefinition = null;
            GameObject opticPrefab = null;

            try
            {
                yield return null;

                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);

                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                opticDefinition = CreateOpticDefinition("scope-pip-missing-anchor", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("OpticMissingAuthoredSightAnchor");
                SetField(opticDefinition, "_opticPrefab", opticPrefab);

                Assert.That(
                    (bool)Invoke(manager, "EquipOptic", opticDefinition),
                    Is.False,
                    "PiP optics should reject prefabs without an authored SightAnchor instead of synthesizing one at runtime.");
                Assert.That(scopeSlot.childCount, Is.EqualTo(0), "Rejected optics should not remain mounted in the scope slot.");
                Assert.That(GetProperty(manager, "ActiveOpticInstance"), Is.Null);
                Assert.That(Invoke(manager, "GetActiveSightAnchor"), Is.SameAs(ironAnchor));
            }
            finally
            {
                Cleanup(root, opticDefinition, opticPrefab);
            }

            yield return null;
        }

    }
}
