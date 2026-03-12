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
    public class ScopeAttachmentAdsIntegrationPlayModeTests
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
            Assert.That((int)minusUnshiftedArgs[4], Is.EqualTo(1), "Expected unshifted '-' to raise elevation clicks.");

            var equalsUnshiftedArgs = new object[] { false, false, true, 0, 0 };
            Assert.That((bool)resolveMethod.Invoke(null, equalsUnshiftedArgs), Is.True);
            Assert.That((int)equalsUnshiftedArgs[3], Is.EqualTo(0));
            Assert.That((int)equalsUnshiftedArgs[4], Is.EqualTo(-1), "Expected unshifted '=' to lower elevation clicks.");

            var minusShiftedArgs = new object[] { true, true, false, 0, 0 };
            Assert.That((bool)resolveMethod.Invoke(null, minusShiftedArgs), Is.True);
            Assert.That((int)minusShiftedArgs[3], Is.EqualTo(1), "Expected shifted '-' to raise windage clicks.");
            Assert.That((int)minusShiftedArgs[4], Is.EqualTo(0));

            var equalsShiftedArgs = new object[] { true, false, true, 0, 0 };
            Assert.That((bool)resolveMethod.Invoke(null, equalsShiftedArgs), Is.True);
            Assert.That((int)equalsShiftedArgs[3], Is.EqualTo(-1), "Expected shifted '=' to lower windage clicks.");
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
                yield return null;
                yield return null;

                var adjustmentController = GetProperty(manager, "ActiveScopeAdjustmentController");
                Assert.That(adjustmentController, Is.Not.Null);

                var effectiveAtZero = (Vector2)GetProperty(scopeController, "CurrentEffectiveAdjustmentMrad");
                Assert.That(effectiveAtZero.x, Is.EqualTo(0.25f).Within(0.0001f));
                Assert.That(effectiveAtZero.y, Is.EqualTo(-0.5f).Within(0.0001f));

                Invoke(adjustmentController, "ApplyWindageClicks", 10);
                Invoke(adjustmentController, "ApplyElevationClicks", -10);
                yield return null;

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

        [Test]
        public void RealKar98kOpticAsset_UsesRenderTexturePipVisualMode()
        {
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");
            Assert.That(adsVisualModeType, Is.Not.Null);

            var visualModePolicyProperty = opticDefinition.GetType().GetProperty("VisualModePolicy", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(visualModePolicyProperty, Is.Not.Null);

            var visualMode = visualModePolicyProperty.GetValue(opticDefinition);
            Assert.That(visualMode, Is.EqualTo(Enum.Parse(adsVisualModeType, "RenderTexturePiP")));
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_LensDisplay_UsesProxySurfaceWhenTextureApplied()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.white);
            scopeTexture.SetPixel(1, 0, Color.black);
            scopeTexture.SetPixel(0, 1, Color.black);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            try
            {
                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                Assert.That((bool)GetProperty(lensDisplay, "IsUsingProxySurface"), Is.True,
                    "Real Kar98k optic should use a runtime proxy display surface when its authored lens mesh cannot present PiP imagery correctly.");
                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null, "Real Kar98k proxy display surface should be instantiated under the optic root.");

                var sightAnchor = opticInstance.transform.Find("SightAnchor");
                Assert.That(sightAnchor, Is.Not.Null);
                var lensTransform = GetComponentInChildren(opticInstance, scopeLensDisplayType).transform;

                var distanceToSightAnchor = Vector3.Distance(proxySurface.position, sightAnchor.position);
                var distanceToFrontLens = Vector3.Distance(proxySurface.position, lensTransform.position);
                Assert.That(distanceToSightAnchor, Is.LessThan(distanceToFrontLens),
                    "Proxy display surface should sit near the authored sight anchor on the eyepiece side, not on the front lens.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_UsesLensShapedMeshInsteadOfQuad()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                var internalLensMeshFilter = lensDisplay.GetComponent<MeshFilter>();
                Assert.That(internalLensMeshFilter, Is.Not.Null);
                Assert.That(internalLensMeshFilter.sharedMesh, Is.Not.Null);
                Assert.That(internalLensMeshFilter.sharedMesh.isReadable, Is.True,
                    "Real Kar98k optic lens mesh must stay readable so proxy fallback can preserve the authored lens silhouette.");

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyMeshFilter = proxySurface.GetComponent<MeshFilter>();
                Assert.That(proxyMeshFilter, Is.Not.Null);
                Assert.That(proxyMeshFilter.sharedMesh, Is.Not.Null);
                Assert.That(proxyMeshFilter.sharedMesh.vertexCount, Is.EqualTo(internalLensMeshFilter.sharedMesh.vertexCount),
                    "Proxy fallback should reuse the authored lens silhouette instead of reverting to a rectangular quad.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_FillsMeaningfulViewportAreaFromSightAnchor()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var previewCameraGo = new GameObject("ScopePreviewCamera");

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null, "Real Kar98k proxy display surface should be instantiated under the optic root.");

                var proxyRenderer = proxySurface.GetComponent<Renderer>();
                Assert.That(proxyRenderer, Is.Not.Null);

                var sightAnchor = opticInstance.transform.Find("SightAnchor");
                Assert.That(sightAnchor, Is.Not.Null);

                var previewCamera = previewCameraGo.AddComponent<Camera>();
                previewCamera.transform.SetPositionAndRotation(sightAnchor.position, sightAnchor.rotation);
                previewCamera.nearClipPlane = 0.001f;
                previewCamera.farClipPlane = 10f;
                previewCamera.fieldOfView = 60f;

                var viewportRect = CalculateViewportRect(previewCamera, proxyRenderer.bounds);
                Assert.That(viewportRect.width, Is.GreaterThan(0.12f),
                    $"Proxy display surface should occupy a meaningful amount of screen width from the eye box. Actual width={viewportRect.width:0.000}.");
                Assert.That(viewportRect.height, Is.GreaterThan(0.12f),
                    $"Proxy display surface should occupy a meaningful amount of screen height from the eye box. Actual height={viewportRect.height:0.000}.");
            }
            finally
            {
                Cleanup(previewCameraGo, opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_IsLargerThanInternalLensSurface()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                var internalLensRenderer = lensDisplay.GetComponent<Renderer>();
                Assert.That(internalLensRenderer, Is.Not.Null);

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyRenderer = proxySurface.GetComponent<Renderer>();
                Assert.That(proxyRenderer, Is.Not.Null);

                Assert.That(proxyRenderer.bounds.size.x, Is.GreaterThan(internalLensRenderer.bounds.size.x * 1.2f),
                    $"Proxy display surface should be larger than the internal lens aperture so the PiP image does not look buried inside the scope. Proxy width={proxyRenderer.bounds.size.x:0.0000}, internal width={internalLensRenderer.bounds.size.x:0.0000}.");
                Assert.That(proxyRenderer.bounds.size.y, Is.GreaterThan(internalLensRenderer.bounds.size.y * 1.2f),
                    $"Proxy display surface should be larger than the internal lens aperture so the PiP image does not look buried inside the scope. Proxy height={proxyRenderer.bounds.size.y:0.0000}, internal height={internalLensRenderer.bounds.size.y:0.0000}.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_PreservesAuthoredLensAspectRatio()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                var internalLensRenderer = lensDisplay.GetComponent<Renderer>();
                Assert.That(internalLensRenderer, Is.Not.Null);

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyRenderer = proxySurface.GetComponent<Renderer>();
                Assert.That(proxyRenderer, Is.Not.Null);

                var sourceAspect = internalLensRenderer.bounds.size.x / Mathf.Max(0.0001f, internalLensRenderer.bounds.size.y);
                var proxyAspect = proxyRenderer.bounds.size.x / Mathf.Max(0.0001f, proxyRenderer.bounds.size.y);
                Assert.That(proxyAspect, Is.EqualTo(sourceAspect).Within(0.05f),
                    $"Proxy fallback should preserve the authored lens aspect ratio instead of stretching vertically. Proxy aspect={proxyAspect:0.000}, source aspect={sourceAspect:0.000}.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_CentersOnAuthoredLensInLensPlane()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var lensPlaneOffset = lensDisplay.transform.InverseTransformPoint(proxySurface.position);
                Assert.That(Mathf.Abs(lensPlaneOffset.x), Is.LessThan(0.0025f),
                    $"Proxy display surface should stay centered on the authored lens horizontally. Local x offset={lensPlaneOffset.x:0.0000}.");
                Assert.That(Mathf.Abs(lensPlaneOffset.y), Is.LessThan(0.0025f),
                    $"Proxy display surface should stay centered on the authored lens vertically. Local y offset={lensPlaneOffset.y:0.0000}.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_ProxyDisplayLocalOffset_ShiftsProxyAlongLensAxes()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var unoffsetRoot = CreateProxyOffsetFixture(scopeLensDisplayType, Vector3.zero, 1f, out var unoffsetLensDisplay, out var unoffsetScopeTexture);
            var offsetRoot = CreateProxyOffsetFixture(scopeLensDisplayType, new Vector3(0.004f, -0.006f, 0.002f), 1f, out var offsetLensDisplay, out var offsetScopeTexture);

            try
            {
                yield return null;

                Assert.That((bool)Invoke(unoffsetLensDisplay, "TrySetTexture", new object[] { unoffsetScopeTexture }), Is.True);
                Assert.That((bool)Invoke(offsetLensDisplay, "TrySetTexture", new object[] { offsetScopeTexture }), Is.True);

                var unoffsetProxy = unoffsetRoot.transform.Find("ScopeDisplayProxy");
                var offsetProxy = offsetRoot.transform.Find("ScopeDisplayProxy");
                Assert.That(unoffsetProxy, Is.Not.Null);
                Assert.That(offsetProxy, Is.Not.Null);

                var expectedWorldDelta =
                    offsetLensDisplay.transform.right * 0.004f +
                    offsetLensDisplay.transform.up * -0.006f +
                    offsetLensDisplay.transform.forward * 0.002f;
                var actualWorldDelta = offsetProxy.position - unoffsetProxy.position;

                Assert.That(Vector3.Distance(actualWorldDelta, expectedWorldDelta), Is.LessThan(0.0015f),
                    $"Proxy display local offset should move the proxy along the authored lens axes. Expected delta={expectedWorldDelta}, actual delta={actualWorldDelta}.");
            }
            finally
            {
                Cleanup(unoffsetRoot, unoffsetScopeTexture, offsetRoot, offsetScopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_ProxySurface_LeavesVisibleEyepieceMargin()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = CreateProxyOffsetFixture(scopeLensDisplayType, Vector3.zero, 1f, out var lensDisplay, out var scopeTexture);

            try
            {
                lensDisplay.transform.localScale = Vector3.one * 0.2f;

                yield return null;

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = root.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyRenderer = proxySurface.GetComponent<Renderer>();
                var rootRenderer = root.GetComponent<Renderer>();
                Assert.That(proxyRenderer, Is.Not.Null);
                Assert.That(rootRenderer, Is.Not.Null);

                Assert.That(proxyRenderer.bounds.size.x, Is.LessThan(rootRenderer.bounds.size.x * 0.98f),
                    $"Proxy PiP should leave a visible horizontal eyepiece frame margin. Proxy width={proxyRenderer.bounds.size.x:0.0000}, body width={rootRenderer.bounds.size.x:0.0000}.");
                Assert.That(proxyRenderer.bounds.size.y, Is.LessThan(rootRenderer.bounds.size.y * 0.98f),
                    $"Proxy PiP should leave a visible vertical eyepiece frame margin. Proxy height={proxyRenderer.bounds.size.y:0.0000}, body height={rootRenderer.bounds.size.y:0.0000}.");
            }
            finally
            {
                Cleanup(root, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_ProxyDisplayScaleMultiplier_ScalesProxyAtRuntime()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var defaultRoot = CreateProxyOffsetFixture(scopeLensDisplayType, Vector3.zero, 1f, out var defaultLensDisplay, out var defaultScopeTexture);
            var scaledRoot = CreateProxyOffsetFixture(scopeLensDisplayType, Vector3.zero, 0.95f, out var scaledLensDisplay, out var scaledScopeTexture);

            try
            {
                yield return null;

                Assert.That((bool)Invoke(defaultLensDisplay, "TrySetTexture", new object[] { defaultScopeTexture }), Is.True);
                Assert.That((bool)Invoke(scaledLensDisplay, "TrySetTexture", new object[] { scaledScopeTexture }), Is.True);

                var defaultProxy = defaultRoot.transform.Find("ScopeDisplayProxy");
                var scaledProxy = scaledRoot.transform.Find("ScopeDisplayProxy");
                Assert.That(defaultProxy, Is.Not.Null);
                Assert.That(scaledProxy, Is.Not.Null);

                var defaultRenderer = defaultProxy.GetComponent<Renderer>();
                var scaledRenderer = scaledProxy.GetComponent<Renderer>();
                Assert.That(defaultRenderer, Is.Not.Null);
                Assert.That(scaledRenderer, Is.Not.Null);

                Assert.That(scaledRenderer.bounds.size.x, Is.EqualTo(defaultRenderer.bounds.size.x * 0.95f).Within(0.01f),
                    $"Proxy display scale multiplier should shrink proxy width proportionally. Default width={defaultRenderer.bounds.size.x:0.0000}, scaled width={scaledRenderer.bounds.size.x:0.0000}.");
                Assert.That(scaledRenderer.bounds.size.y, Is.EqualTo(defaultRenderer.bounds.size.y * 0.95f).Within(0.01f),
                    $"Proxy display scale multiplier should shrink proxy height proportionally. Default height={defaultRenderer.bounds.size.y:0.0000}, scaled height={scaledRenderer.bounds.size.y:0.0000}.");
            }
            finally
            {
                Cleanup(defaultRoot, defaultScopeTexture, scaledRoot, scaledScopeTexture);
            }
        }

        [Test]
        public void ScopeAdjustmentController_ClampsWindageAndElevationToMechanicalLimits()
        {
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);

            var root = new GameObject("ScopeAdjustmentRoot");
            try
            {
                var controller = root.AddComponent(scopeAdjustmentControllerType);
                SetField(controller, "_minWindageClicks", -2);
                SetField(controller, "_maxWindageClicks", 2);
                SetField(controller, "_minElevationClicks", -3);
                SetField(controller, "_maxElevationClicks", 3);

                Invoke(controller, "AdjustWindageClicks", 100);
                Invoke(controller, "AdjustElevationClicks", -100);

                Assert.That((int)GetProperty(controller, "CurrentWindageClicks"), Is.EqualTo(2));
                Assert.That((int)GetProperty(controller, "CurrentElevationClicks"), Is.EqualTo(-3));
            }
            finally
            {
                Cleanup(root);
            }
        }

        [Test]
        public void EquipOptic_ScopeAdjustmentController_RestoresStateAcrossReequip()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            var opticDefinition = CreateOpticDefinition("scope-adjust-persist", 4f, 12f, true, "RenderTexturePiP");

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                var opticPrefab = GetProperty(opticDefinition, "OpticPrefab") as GameObject;
                Assert.That(opticPrefab, Is.Not.Null);
                opticPrefab.AddComponent(scopeAdjustmentControllerType);

                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var firstController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Invoke(firstController, "AdjustWindageClicks", 2);
                Invoke(firstController, "AdjustElevationClicks", -1);

                Invoke(manager, "UnequipOptic");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);

                var secondController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Assert.That((int)GetProperty(secondController, "CurrentWindageClicks"), Is.EqualTo(2));
                Assert.That((int)GetProperty(secondController, "CurrentElevationClicks"), Is.EqualTo(-1));
            }
            finally
            {
                Cleanup(root, opticDefinition);
            }
        }

        [Test]
        public void EquipOptic_ScopeAdjustmentController_UsesPendingStateKeyForDistinctScopeInstances()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            var opticDefinition = CreateOpticDefinition("scope-adjust-state-key", 4f, 12f, true, "RenderTexturePiP");

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                var opticPrefab = GetProperty(opticDefinition, "OpticPrefab") as GameObject;
                Assert.That(opticPrefab, Is.Not.Null);
                opticPrefab.AddComponent(scopeAdjustmentControllerType);

                Invoke(manager, "SetPendingOpticAdjustmentStateKey", "scope-a");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var firstController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Invoke(firstController, "AdjustWindageClicks", 3);
                Invoke(manager, "UnequipOptic");

                Invoke(manager, "SetPendingOpticAdjustmentStateKey", "scope-b");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var secondController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Assert.That((int)GetProperty(secondController, "CurrentWindageClicks"), Is.EqualTo(0));
                Invoke(secondController, "AdjustWindageClicks", -2);
                Invoke(manager, "UnequipOptic");

                Invoke(manager, "SetPendingOpticAdjustmentStateKey", "scope-a");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var restoredFirstController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Assert.That((int)GetProperty(restoredFirstController, "CurrentWindageClicks"), Is.EqualTo(3));

                Invoke(manager, "UnequipOptic");
                Invoke(manager, "SetPendingOpticAdjustmentStateKey", "scope-b");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var restoredSecondController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Assert.That((int)GetProperty(restoredSecondController, "CurrentWindageClicks"), Is.EqualTo(-2));
            }
            finally
            {
                Cleanup(root, opticDefinition);
            }
        }

        [UnityTest]
        public IEnumerator AdsStateController_ApplyScopeAdjustmentInput_RequiresScopedAds()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            var opticDefinition = CreateOpticDefinition("scope-adjust-gated", 4f, 12f, true, "RenderTexturePiP");

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                var opticPrefab = GetProperty(opticDefinition, "OpticPrefab") as GameObject;
                Assert.That(opticPrefab, Is.Not.Null);
                opticPrefab.AddComponent(scopeAdjustmentControllerType);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_useLegacyInput", false);

                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var activeController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);

                Assert.That((bool)Invoke(ads, "ApplyScopeAdjustmentInput", 1, -1), Is.False);
                Assert.That((int)GetProperty(activeController, "CurrentWindageClicks"), Is.EqualTo(0));
                Assert.That((int)GetProperty(activeController, "CurrentElevationClicks"), Is.EqualTo(0));

                Invoke(ads, "SetAdsHeld", true);
                yield return null;

                Assert.That((bool)Invoke(ads, "ApplyScopeAdjustmentInput", 1, -1), Is.True);
                Assert.That((int)GetProperty(activeController, "CurrentWindageClicks"), Is.EqualTo(1));
                Assert.That((int)GetProperty(activeController, "CurrentElevationClicks"), Is.EqualTo(-1));
            }
            finally
            {
                Cleanup(root, opticDefinition);
            }
        }

        [UnityTest]
        public IEnumerator AdsStateController_ApplyScopeAdjustmentInput_ShiftsScopeProjectionUnderFixedReticle()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            var scopeReticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);
            Assert.That(scopeReticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsProjectionShiftRoot");
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

                scopedOptic = CreateOpticDefinition("scope-adjust-projection", 4f, 12f, true, "RenderTexturePiP");
                var opticPrefab = GetProperty(scopedOptic, "OpticPrefab") as GameObject;
                Assert.That(opticPrefab, Is.Not.Null);

                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent<SpriteRenderer>();
                reticleGo.AddComponent(scopeReticleControllerType);
                reticleDefinition = CreateReticleDefinition("Ffp", 4f);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);

                opticPrefab.AddComponent(scopeAdjustmentControllerType);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);

                var liveReticle = GetComponentInChildren(activeOpticInstance, scopeReticleControllerType);
                Assert.That(liveReticle, Is.Not.Null);
                var baselineReticleLocalPosition = ((Component)liveReticle).transform.localPosition;

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 8f);
                yield return null;
                yield return null;

                var baselineProjection = scopeCamera.projectionMatrix;

                Assert.That((bool)Invoke(ads, "ApplyScopeAdjustmentInput", 2, -1), Is.True);
                yield return null;
                yield return null;

                var adjustedProjection = scopeCamera.projectionMatrix;
                Assert.That(((Component)liveReticle).transform.localPosition, Is.EqualTo(baselineReticleLocalPosition));
                Assert.That(
                    Mathf.Abs(adjustedProjection.m02 - baselineProjection.m02) + Mathf.Abs(adjustedProjection.m12 - baselineProjection.m12),
                    Is.GreaterThan(0.0001f),
                    "Expected scope adjustment input to shift the PiP projection under the fixed reticle.");
            }
            finally
            {
                Cleanup(root, scopedOptic, reticleDefinition, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_AdjustmentTooltip_ShowsOnlyWhileAdsAndReflectsActiveScopeValues()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            var tooltipType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentTooltipOverlay");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);
            Assert.That(tooltipType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            var opticDefinition = CreateOpticDefinition("scope-adjust-tooltip", 4f, 12f, true, "RenderTexturePiP");

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                var opticPrefab = GetProperty(opticDefinition, "OpticPrefab") as GameObject;
                Assert.That(opticPrefab, Is.Not.Null);
                opticPrefab.AddComponent(scopeAdjustmentControllerType);

                var tooltip = root.AddComponent(tooltipType);
                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_scopeAdjustmentTooltipOverlay", tooltip);

                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);

                Invoke(ads, "SetAdsHeld", true);
                yield return null;

                Assert.That((bool)Invoke(ads, "ApplyScopeAdjustmentInput", 2, -1), Is.True);
                yield return null;

                Assert.That((bool)GetProperty(tooltip, "IsVisible"), Is.True);
                var tooltipText = (string)GetProperty(tooltip, "CurrentText");
                Assert.That(tooltipText, Does.Contain("ELEV -1"));
                Assert.That(tooltipText, Does.Contain("WIND +2"));

                Invoke(ads, "SetAdsHeld", false);
                yield return null;

                Assert.That((bool)GetProperty(tooltip, "IsVisible"), Is.False);
            }
            finally
            {
                Cleanup(root, opticDefinition);
            }
        }

        [Test]
        public void AttachmentManager_DefaultVerboseOpticLogs_AreDisabled()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            Assert.That(attachmentManagerType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var verboseLogsField = attachmentManagerType.GetField("_verboseOpticLogs", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(verboseLogsField, Is.Not.Null);
                Assert.That((bool)verboseLogsField.GetValue(manager), Is.False);
            }
            finally
            {
                Cleanup(root);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_CollapsedUvMesh_UsesProxySurfaceInsteadOfSamplingSingleTexel()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("ScopeLensDisplayTestRoot");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(root.transform, false);
            sightAnchor.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            var lensDisplayGo = new GameObject("LensDisplay");
            lensDisplayGo.transform.SetParent(root.transform, false);
            lensDisplayGo.layer = 23;
            lensDisplayGo.transform.localPosition = new Vector3(0f, 0f, -0.006f);

            var meshFilter = lensDisplayGo.AddComponent<MeshFilter>();
            var meshRenderer = lensDisplayGo.AddComponent<MeshRenderer>();

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var sourceMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            var collapsedUvMesh = UnityEngine.Object.Instantiate(sourceMesh);
            collapsedUvMesh.name = "CollapsedUvQuad";
            var uv = collapsedUvMesh.uv;
            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(0.5f, 0.5f);
            }

            collapsedUvMesh.uv = uv;
            meshFilter.sharedMesh = collapsedUvMesh;

            var authoredMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial = authoredMaterial;

            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.red);
            scopeTexture.SetPixel(1, 0, Color.green);
            scopeTexture.SetPixel(0, 1, Color.blue);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            try
            {
                var lensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(lensDisplay, "_targetRenderer", meshRenderer);

                yield return null;

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                Assert.That((bool)GetProperty(lensDisplay, "IsUsingProxySurface"), Is.True,
                    "Collapsed lens UVs should trigger the runtime proxy surface so PiP does not collapse into a solid color.");
                Assert.That(meshRenderer.enabled, Is.False, "The broken authored renderer should be hidden while the proxy surface is active.");
                var proxySurface = root.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null, "Proxy display surface should be created when collapsed UVs require fallback rendering.");
                Assert.That(proxySurface.gameObject.layer, Is.EqualTo(lensDisplayGo.layer),
                    "Proxy display surface must inherit the lens layer so scope cameras that exclude the viewmodel layer do not recursively render it.");
                Assert.That(
                    Vector3.Distance(proxySurface.position, sightAnchor.transform.position),
                    Is.LessThan(Vector3.Distance(proxySurface.position, lensDisplayGo.transform.position)),
                    "Proxy display surface should be placed near the authored sight anchor on the eyepiece side.");
            }
            finally
            {
                Cleanup(root, primitive, collapsedUvMesh, authoredMaterial, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_PartialProxyState_RebuildsUsableProxyBeforeHidingAuthoredLens()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("ScopeLensDisplayPartialProxyRoot");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(root.transform, false);
            sightAnchor.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            var lensDisplayGo = new GameObject("LensDisplay");
            lensDisplayGo.transform.SetParent(root.transform, false);

            var meshFilter = lensDisplayGo.AddComponent<MeshFilter>();
            var meshRenderer = lensDisplayGo.AddComponent<MeshRenderer>();

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var collapsedUvMesh = UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            collapsedUvMesh.name = "CollapsedUvQuad";
            var uv = collapsedUvMesh.uv;
            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(0.5f, 0.5f);
            }

            collapsedUvMesh.uv = uv;
            meshFilter.sharedMesh = collapsedUvMesh;

            var authoredMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial = authoredMaterial;

            var staleProxy = new GameObject("ScopeDisplayProxy");
            staleProxy.transform.SetParent(root.transform, false);
            var staleRenderer = staleProxy.AddComponent<MeshRenderer>();

            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.red);
            scopeTexture.SetPixel(1, 0, Color.green);
            scopeTexture.SetPixel(0, 1, Color.blue);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            try
            {
                var lensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(lensDisplay, "_targetRenderer", meshRenderer);
                SetField(lensDisplay, "_proxySurface", staleProxy);
                SetField(lensDisplay, "_proxyRenderer", staleRenderer);

                yield return null;

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                Assert.That((bool)GetProperty(lensDisplay, "IsUsingProxySurface"), Is.True);

                var proxyMeshFilter = staleProxy.GetComponent<MeshFilter>();
                Assert.That(proxyMeshFilter, Is.Not.Null, "Proxy setup should rebuild a missing MeshFilter instead of hiding the authored lens behind an unusable proxy.");
                Assert.That(proxyMeshFilter!.sharedMesh, Is.Not.Null, "Proxy setup should only hide the authored lens once a usable proxy mesh exists.");
                Assert.That(meshRenderer.enabled, Is.False, "Authored lens should only be hidden after the rebuilt proxy becomes usable.");
            }
            finally
            {
                Cleanup(root, primitive, collapsedUvMesh, authoredMaterial, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_Destroy_ReleasesGeneratedProxyMesh()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("ScopeLensDisplayDestroyRoot");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(root.transform, false);
            sightAnchor.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            var lensDisplayGo = new GameObject("LensDisplay");
            lensDisplayGo.transform.SetParent(root.transform, false);

            var meshFilter = lensDisplayGo.AddComponent<MeshFilter>();
            var meshRenderer = lensDisplayGo.AddComponent<MeshRenderer>();

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var collapsedUvMesh = UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            collapsedUvMesh.name = "CollapsedUvQuad";
            var uv = collapsedUvMesh.uv;
            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(0.5f, 0.5f);
            }

            collapsedUvMesh.uv = uv;
            meshFilter.sharedMesh = collapsedUvMesh;

            var authoredMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial = authoredMaterial;

            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.red);
            scopeTexture.SetPixel(1, 0, Color.green);
            scopeTexture.SetPixel(0, 1, Color.blue);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            try
            {
                var lensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(lensDisplay, "_targetRenderer", meshRenderer);

                yield return null;

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                var proxySurface = root.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyMeshFilter = proxySurface!.GetComponent<MeshFilter>();
                Assert.That(proxyMeshFilter, Is.Not.Null);
                Assert.That(proxyMeshFilter!.sharedMesh, Is.Not.Null);
                var proxyMesh = proxyMeshFilter.sharedMesh;
                Assert.That(proxyMesh.name, Does.Contain("_ScopeProxy"), "Readable-collapsed UV meshes should allocate a flattened runtime proxy mesh.");

                UnityEngine.Object.DestroyImmediate(root);
                root = null;
                yield return null;

                Assert.That(proxyMesh == null, Is.True, "Destroying the lens display should release the generated runtime proxy mesh.");
            }
            finally
            {
                Cleanup(root, primitive, collapsedUvMesh, authoredMaterial, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator EquipOptic_HotSwap_UpdatesActiveSightAnchor()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            Assert.That(attachmentManagerType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            var manager = root.AddComponent(attachmentManagerType);
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(root.transform, false);
            var ironAnchor = new GameObject("IronSightAnchor").transform;
            ironAnchor.SetParent(root.transform, false);

            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironAnchor);

            var lpvo = CreateOpticDefinition("lpvo", 1f, 6f, true, "Auto");
            var highMag = CreateOpticDefinition("high", 6f, 12f, true, "Auto");

            Assert.That((bool)Invoke(manager, "EquipOptic", lpvo), Is.True);
            var firstAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
            Assert.That(firstAnchor, Is.Not.Null);
            Assert.That(firstAnchor.name, Is.EqualTo("SightAnchor"));
            var firstAnchorInstanceId = firstAnchor.GetInstanceID();
            Assert.That(GetProperty(manager, "ActiveOpticDefinition"), Is.EqualTo(lpvo));

            Assert.That((bool)Invoke(manager, "EquipOptic", highMag), Is.True);
            var secondAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
            Assert.That(secondAnchor, Is.Not.Null);
            Assert.That(secondAnchor.GetInstanceID(), Is.Not.EqualTo(firstAnchorInstanceId));
            Assert.That(GetProperty(manager, "ActiveOpticDefinition"), Is.EqualTo(highMag));

            Cleanup(root, lpvo, highMag);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EquipOptic_PipOpticWithoutSightAnchor_FailsWithoutSyntheticFallback()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            var manager = root.AddComponent(attachmentManagerType);
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(root.transform, false);
            var ironAnchor = new GameObject("IronSightAnchor").transform;
            ironAnchor.SetParent(root.transform, false);

            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironAnchor);

            var opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
            var opticPrefab = new GameObject("BrokenPipOptic");
            SetField(opticDefinition, "_opticId", "att-broken-pip");
            SetField(opticDefinition, "_magnificationMin", 4f);
            SetField(opticDefinition, "_magnificationMax", 4f);
            SetField(opticDefinition, "_magnificationStep", 1f);
            SetField(opticDefinition, "_visualModePolicy", Enum.Parse(adsVisualModeType, "RenderTexturePiP"));
            SetField(opticDefinition, "_opticPrefab", opticPrefab);

            try
            {
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.False, "PiP optics without authored SightAnchor should fail instead of synthesizing a fallback anchor.");
                Assert.That(scopeSlot.childCount, Is.EqualTo(0));
                Assert.That(Invoke(manager, "GetActiveSightAnchor"), Is.EqualTo(ironAnchor));
            }
            finally
            {
                Cleanup(root, opticDefinition, opticPrefab);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator AdsVisualMode_Auto_TogglesMaskAcrossOpticHotSwap()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var scopeMaskControllerType = ResolveType("Reloader.Game.Weapons.ScopeMaskController");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(scopeMaskControllerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);

            var root = new GameObject("AdsRoot");
            var manager = root.AddComponent(attachmentManagerType);
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(root.transform, false);
            var ironAnchor = new GameObject("IronSightAnchor").transform;
            ironAnchor.SetParent(root.transform, false);
            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironAnchor);

            var worldCamGo = new GameObject("WorldCam");
            var worldCamera = worldCamGo.AddComponent<Camera>();
            var viewmodelCamGo = new GameObject("ViewmodelCam");
            var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();

            var scopeMaskGo = new GameObject("ScopeMask");
            var scopeMask = scopeMaskGo.AddComponent(scopeMaskControllerType);

            var ads = root.AddComponent(adsStateControllerType);
            SetField(ads, "_worldCamera", worldCamera);
            SetField(ads, "_viewmodelCamera", viewmodelCamera);
            SetField(ads, "_attachmentManager", manager);
            SetField(ads, "_scopeMaskController", scopeMask);
            SetField(ads, "_useLegacyInput", false);

            var lowMag = CreateOpticDefinition("red-dot", 1f, 1f, false, "Auto");
            var highMag = CreateOpticDefinition("scope-high", 6f, 12f, true, "Auto");

            Assert.That((bool)Invoke(manager, "EquipOptic", highMag), Is.True);
            Invoke(ads, "SetAdsHeld", true);
            Invoke(ads, "SetMagnification", 8f);

            yield return null;
            yield return null;

            Assert.That((bool)GetProperty(scopeMask, "IsMaskVisible"), Is.True, "High magnification scope should activate mask in Auto mode.");

            Assert.That((bool)Invoke(manager, "EquipOptic", lowMag), Is.True);
            Invoke(ads, "SetMagnification", 1f);

            yield return null;
            yield return null;

            Assert.That((bool)GetProperty(scopeMask, "IsMaskVisible"), Is.False, "Low magnification optic should disable mask in Auto mode.");

            Cleanup(root, lowMag, highMag, worldCamGo, viewmodelCamGo, scopeMaskGo);
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_PreservesMainCameraFovDuringAds()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
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

                scopedOptic = CreateOpticDefinition("scope-pip", 4f, 12f, true, "RenderTexturePiP");
                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 8f);

                yield return null;
                yield return null;

                Assert.That(worldCamera.fieldOfView, Is.EqualTo(72f).Within(0.05f), "Scoped PiP optics should not zoom the main camera.");
            }
            finally
            {
                Cleanup(root, scopedOptic, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_UsesScopeCameraFovForMagnification()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
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

                scopedOptic = CreateOpticDefinition("scope-pip", 4f, 12f, true, "RenderTexturePiP");
                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);

                var firstExpectedFov = MagnificationToFieldOfView(72f, 4f);
                yield return WaitUntil(
                    () => Mathf.Abs(scopeCamera.fieldOfView - firstExpectedFov) <= 0.2f,
                    20,
                    "Scope camera did not converge to the expected 4x FOV.");
                var firstScopeFov = scopeCamera.fieldOfView;

                Invoke(ads, "SetMagnification", 8f);

                var secondExpectedFov = MagnificationToFieldOfView(72f, 8f);
                yield return WaitUntil(
                    () => Mathf.Abs(scopeCamera.fieldOfView - secondExpectedFov) <= 0.2f,
                    20,
                    "Scope camera did not converge to the expected 8x FOV.");

                var secondScopeFov = scopeCamera.fieldOfView;
                Assert.That(firstScopeFov, Is.EqualTo(firstExpectedFov).Within(0.2f));
                Assert.That(secondScopeFov, Is.EqualTo(secondExpectedFov).Within(0.2f));
                Assert.That(secondScopeFov, Is.LessThan(firstScopeFov), "Higher magnification should narrow the scope camera FOV.");
            }
            finally
            {
                Cleanup(root, scopedOptic, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_DoesNotAllocateRenderTextureWhileInactive()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
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

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);

                scopedOptic = CreateOpticDefinition("scope-pip-idle", 4f, 8f, true, "RenderTexturePiP");
                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);

                yield return null;

                var backingRenderTexture = GetPrivateField(scopeController, "_scopeRenderTexture") as RenderTexture;
                Assert.That(backingRenderTexture, Is.Null, "PiP scope render texture should not allocate before scoped ADS is active.");
                Assert.That(scopeCamera.targetTexture, Is.Null);
            }
            finally
            {
                Cleanup(root, scopedOptic, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_BindsRenderTextureToLensDisplay()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            GameObject opticPrefab = null;
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

                scopedOptic = CreateOpticDefinition("scope-pip-display", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-display");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var lensDisplayGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                lensDisplayGo.name = "LensDisplay";
                lensDisplayGo.transform.SetParent(opticPrefab.transform, false);
                var prefabLensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(prefabLensDisplay, "_targetRenderer", lensDisplayGo.GetComponent<Renderer>());
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                var liveLensDisplay = GetComponentInChildren(activeOpticInstance, scopeLensDisplayType);
                Assert.That(liveLensDisplay, Is.Not.Null);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 8f);

                yield return null;
                yield return null;

                var currentTexture = GetProperty(liveLensDisplay, "CurrentTexture") as Texture;
                Assert.That(scopeCamera.targetTexture, Is.Not.Null);
                Assert.That(currentTexture, Is.SameAs(scopeCamera.targetTexture), "Lens display should receive the live scope render texture.");
            }
            finally
            {
                Cleanup(root, scopedOptic, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_LensDisplaySwapsOffDarkSourceMaterialWhileActive()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            GameObject opticPrefab = null;
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

                scopedOptic = CreateOpticDefinition("scope-pip-dark-lens", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-dark-lens");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var lensDisplayGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                lensDisplayGo.name = "LensDisplay";
                lensDisplayGo.transform.SetParent(opticPrefab.transform, false);
                var lensRenderer = lensDisplayGo.GetComponent<Renderer>();
                Assert.That(lensRenderer, Is.Not.Null);
                var darkLensMaterial = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.1f, 0.1f, 0.1f, 0.25f)
                };
                lensRenderer.sharedMaterial = darkLensMaterial;

                var prefabLensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(prefabLensDisplay, "_targetRenderer", lensRenderer);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                var liveLensDisplay = GetComponentInChildren(activeOpticInstance, scopeLensDisplayType);
                Assert.That(liveLensDisplay, Is.Not.Null);

                var liveLensRenderer = activeOpticInstance.transform.Find("LensDisplay")?.GetComponent<Renderer>();
                Assert.That(liveLensRenderer, Is.Not.Null);
                var originalMaterial = liveLensRenderer.sharedMaterial;
                Assert.That(originalMaterial, Is.Not.Null);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 8f);
                yield return null;
                yield return null;

                Assert.That(liveLensRenderer.sharedMaterial, Is.Not.SameAs(originalMaterial),
                    "Scoped PiP lens should swap off the dark source material while actively displaying the scope image.");

                Assert.That((bool)Invoke(liveLensDisplay, "TrySetTexture", new object[] { null }), Is.True);

                Assert.That(liveLensRenderer.sharedMaterial, Is.SameAs(originalMaterial),
                    "Lens display should restore the authored lens material when the scope image is cleared.");
            }
            finally
            {
                Cleanup(root, scopedOptic, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_MissingReticleDefinition_LogsWarning()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            GameObject opticPrefab = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;

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

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);

                scopedOptic = CreateOpticDefinition("scope-pip-missing-reticle-definition", 4f, 8f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-missing-reticle-definition");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent(reticleControllerType);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return null;
                yield return null;

                Assert.That(
                    warnings.Exists(message => message.Contains("ScopeReticleDefinition", StringComparison.Ordinal)),
                    Is.True,
                    "Scoped PiP optics without a reticle definition should fail loudly.");
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
                Cleanup(root, scopedOptic, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_DestroyedPreviousReticleController_DoesNotThrowOnDeactivate()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            GameObject opticPrefab = null;
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

                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();
                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                scopedOptic = CreateOpticDefinition("scope-pip-destroyed-reticle", 4f, 8f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-destroyed-reticle");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent(reticleControllerType);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);

                Invoke(scopeController, "SetScopeActive", true, scopedOptic, activeOpticInstance, 72f, 8f, 0, 0);
                UnityEngine.Object.Destroy(activeOpticInstance);
                yield return null;

                Assert.DoesNotThrow(() => Invoke(scopeController, "SetScopeActive", false, scopedOptic, null, 72f, 1f, 0, 0));
            }
            finally
            {
                Cleanup(root, scopedOptic, opticPrefab, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_MissingLensDisplay_LogsWarning()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            var manager = root.AddComponent(attachmentManagerType);
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(root.transform, false);
            var ironAnchor = new GameObject("IronSightAnchor").transform;
            ironAnchor.SetParent(root.transform, false);
            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironAnchor);

            var worldCamGo = new GameObject("WorldCam");
            var worldCamera = worldCamGo.AddComponent<Camera>();
            worldCamera.fieldOfView = 72f;

            var viewmodelCamGo = new GameObject("ViewmodelCam");
            var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
            viewmodelCamera.fieldOfView = 55f;

            var scopeCameraGo = new GameObject("ScopeCam");
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

            var scopedOptic = CreateOpticDefinition("scope-pip-missing-display", 4f, 12f, true, "RenderTexturePiP");

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
                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 8f);

                yield return null;
                yield return null;
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
            }

            Assert.That(
                warnings.Exists(message => message.Contains("ScopeLensDisplay", StringComparison.Ordinal)),
                Is.True,
                "Scoped PiP optics without lens-display wiring should fail loudly.");

            Cleanup(root, scopedOptic, worldCamGo, viewmodelCamGo, scopeCameraGo);
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_ActivatesPeripheralEffectsOnlyWhileAds()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var peripheralEffectsType = ResolveType("Reloader.Game.Weapons.PeripheralScopeEffects");
            var peripheralScopeScreenMaskType = ResolveType("Reloader.Game.Weapons.PeripheralScopeScreenMask");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(peripheralEffectsType, Is.Not.Null);
            Assert.That(peripheralScopeScreenMaskType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
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

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);
                var peripheralEffects = root.AddComponent(peripheralEffectsType);
                var peripheralMask = root.AddComponent(peripheralScopeScreenMaskType) as Behaviour;
                Assert.That(peripheralMask, Is.Not.Null);
                SetField(peripheralEffectsType, peripheralEffects, "_scopedBehaviours", new[] { peripheralMask });

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_peripheralScopeEffects", peripheralEffects);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_fallbackAdsOutTime", 0.01f);

                scopedOptic = CreateOpticDefinition("scope-pip-effects", 4f, 12f, true, "RenderTexturePiP");
                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);

                yield return null;
                Assert.That((bool)GetProperty(peripheralEffects, "IsActive"), Is.False);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 6f);

                yield return WaitUntil(
                    () => (bool)GetProperty(peripheralEffects, "IsActive"),
                    60,
                    "Peripheral effects did not activate after entering ADS.");
                Assert.That((bool)GetProperty(peripheralEffects, "IsActive"), Is.True);
                Assert.That(peripheralMask.enabled, Is.True, "Scoped peripheral effect receiver should enable while ADS is active.");

                Invoke(ads, "SetAdsHeld", false);

                yield return WaitUntil(
                    () => !(bool)GetProperty(peripheralEffects, "IsActive"),
                    60,
                    "Peripheral effects did not disable after leaving ADS.");
                Assert.That((bool)GetProperty(peripheralEffects, "IsActive"), Is.False);
                Assert.That(peripheralMask.enabled, Is.False, "Scoped peripheral effect receiver should disable after leaving ADS.");
            }
            finally
            {
                Cleanup(root, scopedOptic, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedReticle_FfpScalesWithMagnification()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
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

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-ffp", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-ffp");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent(reticleControllerType);
                reticleDefinition = CreateReticleDefinition("Ffp", 4f);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(scopeController, "CurrentCompositeReticleScale") - 1f) <= 0.01f,
                    20,
                    "FFP reticle did not settle to 1x scale at reference magnification.");
                var firstScale = (float)GetProperty(scopeController, "CurrentCompositeReticleScale");

                Invoke(ads, "SetMagnification", 8f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(scopeController, "CurrentCompositeReticleScale") - 2f) <= 0.05f,
                    20,
                    "FFP reticle did not scale with magnification.");
                var secondScale = (float)GetProperty(scopeController, "CurrentCompositeReticleScale");

                Assert.That(firstScale, Is.EqualTo(1f).Within(0.01f));
                Assert.That(secondScale, Is.EqualTo(2f).Within(0.05f));
            }
            finally
            {
                Cleanup(root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedReticle_FfpUpdatesWithMagnificationWhenUsingFixedScopeCameraFov()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
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

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-fixed-profile-ffp", 4f, 12f, true, "RenderTexturePiP");
                SetField(scopedOptic, "_hasScopeRenderProfile", true);
                SetField(scopedOptic, "_scopeRenderProfile", CreateScopeRenderProfile(1024, 18f));

                opticPrefab = new GameObject("Optic_scope-pip-fixed-profile-ffp");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent(reticleControllerType);
                reticleDefinition = CreateReticleDefinition("Ffp", 4f);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(scopeController, "CurrentCompositeReticleScale") - 1f) <= 0.01f,
                    20,
                    "FFP reticle did not settle to 1x scale at reference magnification with fixed scope FOV.");

                Invoke(ads, "SetMagnification", 8f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(scopeController, "CurrentCompositeReticleScale") - 2f) <= 0.05f,
                    20,
                    "FFP reticle did not update when magnification changed under a fixed scope camera FOV.");

                Assert.That((float)GetProperty(scopeController, "CurrentCompositeReticleScale"), Is.EqualTo(2f).Within(0.05f));
            }
            finally
            {
                Cleanup(root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedReticle_AssignsSpriteRendererSpriteFromDefinition()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;
            Texture2D reticleTexture = null;
            Sprite reticleSprite = null;

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

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);

                scopedOptic = CreateOpticDefinition("scope-pip-reticle-sprite", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-reticle-sprite");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent<SpriteRenderer>();
                reticleGo.AddComponent(reticleControllerType);
                reticleGo.transform.localScale = new Vector3(0.01f, 0.02f, 0.03f);

                reticleTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                reticleTexture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                reticleTexture.Apply();
                reticleSprite = Sprite.Create(reticleTexture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);

                reticleDefinition = CreateReticleDefinition("Sfp", 4f);
                SetField(reticleDefinition, "_reticleSprite", reticleSprite);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                var liveReticleController = GetComponentInChildren(activeOpticInstance, reticleControllerType);
                var liveSpriteRenderer = liveReticleController.GetComponent<SpriteRenderer>();
                Assert.That(liveSpriteRenderer, Is.Not.Null);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return null;
                yield return null;

                Assert.That(GetProperty(scopeController, "CurrentCompositeReticleSprite"), Is.SameAs(reticleSprite));
                Assert.That((bool)GetProperty(scopeController, "IsCompositeReticleActive"), Is.True);
                Assert.That(liveSpriteRenderer.sprite, Is.Null);
                Assert.That(liveSpriteRenderer.enabled, Is.False);
                Assert.That(liveReticleController.transform.localScale, Is.EqualTo(new Vector3(0.01f, 0.02f, 0.03f)));

                Invoke(ads, "SetAdsHeld", false);
                yield return WaitUntil(
                    () =>
                        liveSpriteRenderer.sprite == null
                        && !liveSpriteRenderer.enabled
                        && !(bool)GetProperty(scopeController, "IsCompositeReticleActive")
                        && GetProperty(scopeController, "CurrentCompositeReticleSprite") == null,
                    60,
                    "Reticle sprite renderer did not clear after leaving ADS.");

                Assert.That(liveSpriteRenderer.sprite, Is.Null);
                Assert.That(liveSpriteRenderer.enabled, Is.False);
                Assert.That(GetProperty(scopeController, "CurrentCompositeReticleSprite"), Is.Null);
                Assert.That(liveReticleController.transform.localScale, Is.EqualTo(new Vector3(0.01f, 0.02f, 0.03f)));
            }
            finally
            {
                Cleanup(reticleSprite, reticleTexture, root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_CompositeReticle_PreservesSpriteAspectRatio()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedPipReticleAspectRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;
            Texture2D reticleTexture = null;
            Sprite reticleSprite = null;

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

                scopedOptic = CreateOpticDefinition("scope-pip-reticle-aspect", 4f, 12f, true, "RenderTexturePiP");
                SetField(scopedOptic, "_compositeReticleScale", 0.5f);

                opticPrefab = new GameObject("Optic_scope-pip-reticle-aspect");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent<SpriteRenderer>();
                reticleGo.AddComponent(reticleControllerType);

                reticleTexture = new Texture2D(4, 2, TextureFormat.RGBA32, false);
                reticleTexture.SetPixels(new[]
                {
                    Color.white, Color.white, Color.white, Color.white,
                    Color.white, Color.white, Color.white, Color.white
                });
                reticleTexture.Apply();
                reticleSprite = Sprite.Create(reticleTexture, new Rect(0f, 0f, 4f, 2f), new Vector2(0.5f, 0.5f), 100f);

                reticleDefinition = CreateReticleDefinition("Sfp", 4f);
                SetField(reticleDefinition, "_reticleSprite", reticleSprite);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);

                yield return WaitUntil(
                    () =>
                    {
                        var drawScale = (Vector2)GetProperty(scopeController, "CurrentCompositeReticleDrawScale");
                        return (bool)GetProperty(scopeController, "IsCompositeReticleActive")
                               && Vector2.Distance(drawScale, new Vector2(0.5f, 0.25f)) <= 0.0001f;
                    },
                    60,
                    "PiP composite reticle did not preserve the authored sprite aspect ratio.");

                Assert.That(
                    Vector2.Distance((Vector2)GetProperty(scopeController, "CurrentCompositeReticleDrawScale"), new Vector2(0.5f, 0.25f)),
                    Is.LessThanOrEqualTo(0.0001f));
            }
            finally
            {
                Cleanup(reticleSprite, reticleTexture, root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedReticle_SfpRemainsStableAcrossMagnification()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
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

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-sfp", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-sfp");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent(reticleControllerType);
                reticleDefinition = CreateReticleDefinition("Sfp", 4f);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                var liveReticleController = GetComponentInChildren(activeOpticInstance, reticleControllerType);
                Assert.That(liveReticleController, Is.Not.Null);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return null;
                yield return null;
                var firstScale = (float)GetProperty(liveReticleController, "CurrentScale");

                Invoke(ads, "SetMagnification", 8f);
                yield return null;
                yield return null;
                var secondScale = (float)GetProperty(liveReticleController, "CurrentScale");

                Assert.That(firstScale, Is.EqualTo(1f).Within(0.01f));
                Assert.That(secondScale, Is.EqualTo(1f).Within(0.01f));
            }
            finally
            {
                Cleanup(root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        private static ScriptableObject CreateOpticDefinition(string id, float minMag, float maxMag, bool variableZoom, string visualMode)
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            var definition = ScriptableObject.CreateInstance(opticDefinitionType);
            SetField(definition, "_opticId", id);
            SetField(definition, "_isVariableZoom", variableZoom);
            SetField(definition, "_magnificationMin", minMag);
            SetField(definition, "_magnificationMax", maxMag);
            SetField(definition, "_magnificationStep", 1f);
            SetField(definition, "_visualModePolicy", Enum.Parse(adsVisualModeType, visualMode));

            var prefab = new GameObject($"Optic_{id}");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(prefab.transform, false);
            SetField(definition, "_opticPrefab", prefab);

            return definition;
        }

        private static ScriptableObject CreateReticleDefinition(string mode, float referenceMagnification)
        {
            var reticleDefinitionType = ResolveType("Reloader.Game.Weapons.ScopeReticleDefinition");
            var reticleModeType = ResolveType("Reloader.Game.Weapons.ScopeReticleMode");
            Assert.That(reticleDefinitionType, Is.Not.Null);
            Assert.That(reticleModeType, Is.Not.Null);

            var definition = ScriptableObject.CreateInstance(reticleDefinitionType);
            SetField(definition, "_mode", Enum.Parse(reticleModeType, mode, true));
            SetField(definition, "_referenceMagnification", referenceMagnification);
            return definition;
        }

        private static object CreateScopeRenderProfile(int renderTextureResolution, float scopeCameraFov)
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            var renderProfileType = opticDefinitionType.GetNestedType("ScopeRenderProfile", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(renderProfileType, Is.Not.Null);

            var profile = Activator.CreateInstance(renderProfileType);
            SetField(profile, "_renderTextureResolution", renderTextureResolution);
            SetField(profile, "_scopeCameraFov", scopeCameraFov);
            return profile;
        }

        private static float MagnificationToFieldOfView(float referenceFieldOfView, float magnification)
        {
            var safeReferenceFov = Mathf.Clamp(referenceFieldOfView, 1f, 179f);
            var safeMagnification = Mathf.Max(1f, magnification);
            var referenceHalfAngle = safeReferenceFov * 0.5f * Mathf.Deg2Rad;
            var zoomedHalfAngle = Mathf.Atan(Mathf.Tan(referenceHalfAngle) / safeMagnification);
            return Mathf.Clamp(zoomedHalfAngle * 2f * Mathf.Rad2Deg, 1f, safeReferenceFov);
        }

        private static ScriptableObject ResolveOpticDefinitionById(string opticId)
        {
#if UNITY_EDITOR
            if (string.Equals(opticId, "att-kar98k-scope-remote-a", StringComparison.Ordinal))
            {
                var opticAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    "Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset");
                if (opticAsset != null)
                {
                    return opticAsset;
                }
            }
#endif

            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            var opticIdProperty = opticDefinitionType.GetProperty("OpticId", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticIdProperty, Is.Not.Null);

            var candidates = Resources.FindObjectsOfTypeAll(opticDefinitionType);
            for (var i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is not ScriptableObject candidate)
                {
                    continue;
                }

                if (opticIdProperty.GetValue(candidate) is string candidateId
                    && string.Equals(candidateId, opticId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Type ResolveType(string fullName)
        {
            var direct = Type.GetType(fullName);
            if (direct != null)
            {
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var resolved = assemblies[i].GetType(fullName);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        private static object Invoke(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found on {instance.GetType().Name}.");
            return method.Invoke(instance, args);
        }

        private static object GetProperty(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Property '{propertyName}' was not found on {instance.GetType().Name}.");
            return property.GetValue(instance);
        }

        private static object GetPrivateField(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {instance.GetType().Name}.");
            return field.GetValue(instance);
        }

        private static Component GetComponentInChildren(GameObject gameObject, Type componentType)
        {
            Assert.That(gameObject, Is.Not.Null);
            Assert.That(componentType, Is.Not.Null);
            var component = gameObject.GetComponentInChildren(componentType, true);
            Assert.That(component, Is.Not.Null, $"Component '{componentType.Name}' was not found under '{gameObject.name}'.");
            return component;
        }

        private static IEnumerator WaitUntil(Func<bool> predicate, int maxFrames, string failureMessage)
        {
            Assert.That(predicate, Is.Not.Null);
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (predicate())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(failureMessage);
        }

        private static Rect CalculateViewportRect(Camera camera, Bounds bounds)
        {
            Assert.That(camera, Is.Not.Null);

            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;

            var center = bounds.center;
            var extents = bounds.extents;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        var viewport = camera.WorldToViewportPoint(corner);
                        minX = Mathf.Min(minX, viewport.x);
                        maxX = Mathf.Max(maxX, viewport.x);
                        minY = Mathf.Min(minY, viewport.y);
                        maxY = Mathf.Max(maxY, viewport.y);
                    }
                }
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static GameObject CreateProxyOffsetFixture(Type scopeLensDisplayType, Vector3 proxyOffset, float proxyScaleMultiplier, out Component lensDisplay, out Texture2D scopeTexture)
        {
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "ProxyOffsetRoot";

            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(root.transform, false);
            sightAnchor.transform.localPosition = new Vector3(0f, 0.02f, -0.03f);

            var lensDisplayGo = new GameObject("LensDisplay");
            lensDisplayGo.transform.SetParent(root.transform, false);
            lensDisplayGo.transform.localPosition = new Vector3(0f, 0.008f, -0.006f);

            var meshFilter = lensDisplayGo.AddComponent<MeshFilter>();
            var meshRenderer = lensDisplayGo.AddComponent<MeshRenderer>();
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var sourceMesh = UnityEngine.Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            sourceMesh.name = "CollapsedUvQuad";
            var uv = sourceMesh.uv;
            for (var i = 0; i < uv.Length; i++)
            {
                uv[i] = new Vector2(0.5f, 0.5f);
            }

            sourceMesh.uv = uv;
            meshFilter.sharedMesh = sourceMesh;
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            UnityEngine.Object.DestroyImmediate(primitive);

            lensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
            SetField(lensDisplay, "_targetRenderer", meshRenderer);
            SetField(lensDisplay, "_proxyDisplayLocalOffset", proxyOffset);
            SetField(lensDisplay, "_proxyDisplayScaleMultiplier", proxyScaleMultiplier);

            scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.red);
            scopeTexture.SetPixel(1, 0, Color.green);
            scopeTexture.SetPixel(0, 1, Color.blue);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            return root;
        }

        private static void Cleanup(params UnityEngine.Object[] objects)
        {
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {instance.GetType().Name}.");
            field.SetValue(instance, value);
        }

        private static void SetField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {type.Name}.");
            field.SetValue(instance, value);
        }
    }
}
