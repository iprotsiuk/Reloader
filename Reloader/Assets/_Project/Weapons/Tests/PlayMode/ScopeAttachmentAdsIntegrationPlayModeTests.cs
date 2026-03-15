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
