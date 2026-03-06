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

                Invoke(scopeController, "SetScopeActive", true, scopedOptic, activeOpticInstance, 72f, 8f);
                UnityEngine.Object.Destroy(activeOpticInstance);
                yield return null;

                Assert.DoesNotThrow(() => Invoke(scopeController, "SetScopeActive", false, scopedOptic, null, 72f, 1f));
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
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(peripheralEffectsType, Is.Not.Null);

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

                Invoke(ads, "SetAdsHeld", false);

                yield return WaitUntil(
                    () => !(bool)GetProperty(peripheralEffects, "IsActive"),
                    60,
                    "Peripheral effects did not disable after leaving ADS.");
                Assert.That((bool)GetProperty(peripheralEffects, "IsActive"), Is.False);
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
                var liveReticleController = GetComponentInChildren(activeOpticInstance, reticleControllerType);
                Assert.That(liveReticleController, Is.Not.Null);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(liveReticleController, "CurrentScale") - 1f) <= 0.01f,
                    20,
                    "FFP reticle did not settle to 1x scale at reference magnification.");
                var firstScale = (float)GetProperty(liveReticleController, "CurrentScale");

                Invoke(ads, "SetMagnification", 8f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(liveReticleController, "CurrentScale") - 2f) <= 0.05f,
                    20,
                    "FFP reticle did not scale with magnification.");
                var secondScale = (float)GetProperty(liveReticleController, "CurrentScale");

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
                var liveReticleController = GetComponentInChildren(activeOpticInstance, reticleControllerType);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(liveReticleController, "CurrentScale") - 1f) <= 0.01f,
                    20,
                    "FFP reticle did not settle to 1x scale at reference magnification with fixed scope FOV.");

                Invoke(ads, "SetMagnification", 8f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(liveReticleController, "CurrentScale") - 2f) <= 0.05f,
                    20,
                    "FFP reticle did not update when magnification changed under a fixed scope camera FOV.");

                Assert.That((float)GetProperty(liveReticleController, "CurrentScale"), Is.EqualTo(2f).Within(0.05f));
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

                Assert.That(liveSpriteRenderer.sprite, Is.SameAs(reticleSprite));
                Assert.That(liveSpriteRenderer.enabled, Is.True);
                Assert.That(liveReticleController.transform.localScale, Is.EqualTo(new Vector3(0.01f, 0.02f, 0.03f)));

                Invoke(ads, "SetAdsHeld", false);
                yield return WaitUntil(
                    () => liveSpriteRenderer.sprite == null && !liveSpriteRenderer.enabled,
                    60,
                    "Reticle sprite renderer did not clear after leaving ADS.");

                Assert.That(liveSpriteRenderer.sprite, Is.Null);
                Assert.That(liveSpriteRenderer.enabled, Is.False);
                Assert.That(liveReticleController.transform.localScale, Is.EqualTo(new Vector3(0.01f, 0.02f, 0.03f)));
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
