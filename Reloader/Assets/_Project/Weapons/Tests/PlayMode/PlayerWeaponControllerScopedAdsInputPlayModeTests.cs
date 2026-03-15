using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Reloader.Weapons.Tests.PlayMode
{
    public partial class PlayerWeaponControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator ScopedPipAds_ZoomSensitivityBridgesIntoPlayerLookController_AndRestoresAuthoredBaseline()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            GameObject worldCameraGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object scopedOpticDefinition = null;
            GameObject scopedOpticPrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";
                worldCamera.fieldOfView = 80f;
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                viewmodelCameraGo.AddComponent<Camera>();

                var cameraPivot = new GameObject("CameraPivot").transform;
                cameraPivot.SetParent(root.transform, false);

                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, cameraPivot);
                look.LookSensitivity = Vector2.one;
                var authoredAdsSensitivityMultiplier = new Vector2(0.9f, 0.8f);
                look.AdsSensitivityMultiplier = authoredAdsSensitivityMultiplier;

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var adsPivot = new GameObject("AdsPivot").transform;
                adsPivot.SetParent(viewPrefab.transform, false);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(adsPivot, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(adsPivot, false);
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot: adsPivot, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                scopedOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                scopedOpticPrefab = new GameObject("OpticFourXPrefab");
                var sightAnchor = new GameObject("SightAnchor").transform;
                sightAnchor.SetParent(scopedOpticPrefab.transform, false);
                sightAnchor.localPosition = new Vector3(0f, 0f, 0.4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, scopedOpticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMax", 8f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_visualModePolicy", Enum.Parse(adsVisualModeType, "RenderTexturePiP"));
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticPrefab", scopedOpticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-optic-4x",
                        WeaponAttachmentSlotType.Scope,
                        scopedOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-4x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                input.AimHeldValue = true;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                Assert.That(adsBridge, Is.Not.Null);
                SetField(adsBridge.GetType(), adsBridge, "_magnificationLerpSpeed", 1000f);

                var frames = 0;
                while ((float)GetProperty(adsBridge, "AdsT") < 0.999f && frames < 60)
                {
                    frames++;
                    yield return null;
                }

                Invoke(adsBridge, "SetMagnification", 4f);
                var fourXScale = (float)GetProperty(adsBridge, "CurrentSensitivityScale");
                frames = 0;
                while (Mathf.Abs(look.RuntimeAdsSensitivityMultiplier.x - fourXScale) > 0.01f && frames < 30)
                {
                    frames++;
                    yield return null;
                }

                Assert.That(fourXScale, Is.GreaterThan(0f));
                Assert.That(look.AdsSensitivityMultiplier, Is.EqualTo(authoredAdsSensitivityMultiplier));
                Assert.That(look.RuntimeAdsSensitivityMultiplier.x, Is.EqualTo(fourXScale).Within(0.01f));
                Assert.That(look.RuntimeAdsSensitivityMultiplier.y, Is.EqualTo(fourXScale).Within(0.01f));

                root.transform.rotation = Quaternion.identity;
                cameraPivot.localRotation = Quaternion.identity;
                look.Configure(input, cameraPivot);
                look.LookSensitivity = Vector2.one;
                look.AdsSensitivityMultiplier = authoredAdsSensitivityMultiplier;
                input.LookInputValue = new Vector2(2f, 0f);
                look.Tick(1f);
                input.LookInputValue = Vector2.zero;
                var yawAtFourX = root.transform.eulerAngles.y;

                Invoke(adsBridge, "SetMagnification", 8f);
                frames = 0;
                while (Mathf.Abs((float)GetProperty(adsBridge, "CurrentMagnification") - 8f) > 0.01f && frames < 30)
                {
                    frames++;
                    yield return null;
                }

                var eightXScale = (float)GetProperty(adsBridge, "CurrentSensitivityScale");
                frames = 0;
                while (Mathf.Abs(look.RuntimeAdsSensitivityMultiplier.x - eightXScale) > 0.01f && frames < 30)
                {
                    frames++;
                    yield return null;
                }

                Assert.That(eightXScale, Is.LessThan(fourXScale - 0.01f));
                Assert.That(look.AdsSensitivityMultiplier, Is.EqualTo(authoredAdsSensitivityMultiplier));
                Assert.That(look.RuntimeAdsSensitivityMultiplier.x, Is.EqualTo(eightXScale).Within(0.01f));
                Assert.That(look.RuntimeAdsSensitivityMultiplier.y, Is.EqualTo(eightXScale).Within(0.01f));

                root.transform.rotation = Quaternion.identity;
                cameraPivot.localRotation = Quaternion.identity;
                look.Configure(input, cameraPivot);
                look.LookSensitivity = Vector2.one;
                input.LookInputValue = new Vector2(2f, 0f);
                look.Tick(1f);
                input.LookInputValue = Vector2.zero;
                var yawAtEightX = root.transform.eulerAngles.y;

                Assert.That(yawAtEightX, Is.LessThan(yawAtFourX - 0.05f),
                    "Higher PiP magnification should reduce effective ADS look sensitivity.");

                input.AimHeldValue = false;
                yield return null;
                yield return null;

                Assert.That(look.AdsSensitivityMultiplier, Is.EqualTo(authoredAdsSensitivityMultiplier));
                Assert.That(look.RuntimeAdsSensitivityMultiplier, Is.EqualTo(Vector2.one));
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (worldCameraGo != null)
                {
                    Object.Destroy(worldCameraGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (scopedOpticDefinition != null)
                {
                    Object.Destroy(scopedOpticDefinition);
                }

                if (scopedOpticPrefab != null)
                {
                    Object.Destroy(scopedOpticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipAds_HigherMagnification_ReducesPlayerLookRuntimeSensitivity()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            GameObject worldCameraGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object scopedOpticDefinition = null;
            GameObject scopedOpticPrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                var cameraPivot = new GameObject("CameraPivot");
                cameraPivot.transform.SetParent(root.transform, false);
                var look = root.AddComponent<PlayerLookController>();
                look.Configure(input, cameraPivot.transform);
                look.LookSensitivity = Vector2.one;
                look.AdsSensitivityMultiplier = Vector2.one;

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                viewmodelCameraGo.AddComponent<Camera>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.12f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-25x" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var adsPivot = new GameObject("AdsPivot").transform;
                adsPivot.SetParent(viewPrefab.transform, false);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(adsPivot, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(adsPivot, false);
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot: adsPivot, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                scopedOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                scopedOpticPrefab = new GameObject("OpticTwentyFiveXPrefab");
                new GameObject("SightAnchor").transform.SetParent(scopedOpticPrefab.transform, false);
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticId", "att-optic-25x");
                SetField(opticDefinitionType, scopedOpticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMin", 5f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMax", 25f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_visualModePolicy", Enum.Parse(adsVisualModeType, "RenderTexturePiP"));
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticPrefab", scopedOpticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-optic-25x",
                        WeaponAttachmentSlotType.Scope,
                        scopedOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-25x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                Assert.That(adsBridge, Is.Not.Null);
                SetField(adsBridge.GetType(), adsBridge, "_magnificationLerpSpeed", 1000f);

                input.AimHeldValue = true;
                yield return null;

                var setMagnificationMethod = adsBridge.GetType().GetMethod("SetMagnification", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(setMagnificationMethod, Is.Not.Null);

                setMagnificationMethod!.Invoke(adsBridge, new object[] { 5f });
                yield return null;
                var lowZoomRuntimeScale = look.RuntimeAdsSensitivityMultiplier;

                setMagnificationMethod.Invoke(adsBridge, new object[] { 25f });
                yield return null;
                var highZoomRuntimeScale = look.RuntimeAdsSensitivityMultiplier;

                Assert.That(lowZoomRuntimeScale.x, Is.LessThan(1f), "Scoped PiP ADS should reduce look sensitivity below the hip-fire baseline.");
                Assert.That(lowZoomRuntimeScale.y, Is.LessThan(1f), "Scoped PiP ADS should reduce look sensitivity below the hip-fire baseline.");
                Assert.That(highZoomRuntimeScale.x, Is.LessThan(lowZoomRuntimeScale.x), "Higher magnification should further reduce runtime look sensitivity.");
                Assert.That(highZoomRuntimeScale.y, Is.LessThan(lowZoomRuntimeScale.y), "Higher magnification should further reduce runtime look sensitivity.");
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (worldCameraGo != null)
                {
                    Object.Destroy(worldCameraGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (scopedOpticDefinition != null)
                {
                    Object.Destroy(scopedOpticDefinition);
                }

                if (scopedOpticPrefab != null)
                {
                    Object.Destroy(scopedOpticPrefab);
                }
            }
        }

        [Test]
        public void ScopedPipZoomInput_NormalizesRawWheelDeltaBeforeApplyingMagnification()
        {
            var normalizationType = typeof(PlayerInputReader).Assembly.GetType("Reloader.Player.ZoomInputNormalization");
            Assert.That(normalizationType, Is.Not.Null);

            var method = normalizationType!.GetMethod(
                "NormalizeScrollDelta",
                BindingFlags.Static | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);

            Assert.That((float)method!.Invoke(null, new object[] { 120f }), Is.EqualTo(1f).Within(0.001f));
            Assert.That((float)method.Invoke(null, new object[] { 12f }), Is.EqualTo(1f).Within(0.001f));
            Assert.That((float)method.Invoke(null, new object[] { 0.25f }), Is.EqualTo(0.4f).Within(0.001f));
        }

        [Test]
        public void TickScopedAdsBridgeInput_DoesNotRenormalizeConsumedZoomInput()
        {
            var root = new GameObject("ScopedZoomInputRoot");
            var controller = root.AddComponent<PlayerWeaponController>();
            var input = root.AddComponent<TestInputSource>();
            var adsBridge = root.AddComponent<StubAdsStateRuntimeBridge>();

            try
            {
                input.AimHeldValue = true;
                input.ZoomQueued = 1f;
                adsBridge.SetMagnification(5f);

                SetControllerField(controller, "_inputSource", input);
                SetControllerField(controller, "_adsStateRuntimeBridge", adsBridge);

                Invoke(controller, "TickScopedAdsBridgeInput");

                Assert.That(adsBridge.LastSetMagnification, Is.EqualTo(6f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipAds_MagnificationDoesNotResetWhileSameScopeRemainsMounted()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            GameObject worldCameraGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object scopedOpticDefinition = null;
            GameObject scopedOpticPrefab = null;

            try
            {
                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";
                worldCamera.fieldOfView = 80f;
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                viewmodelCameraGo.AddComponent<Camera>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var adsPivot = new GameObject("AdsPivot").transform;
                adsPivot.SetParent(viewPrefab.transform, false);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(adsPivot, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(adsPivot, false);
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot: adsPivot, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                scopedOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                scopedOpticPrefab = new GameObject("OpticFourXPrefab");
                var sightAnchor = new GameObject("SightAnchor").transform;
                sightAnchor.SetParent(scopedOpticPrefab.transform, false);
                sightAnchor.localPosition = new Vector3(0f, 0f, 0.4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, scopedOpticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMax", 8f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_visualModePolicy", Enum.Parse(adsVisualModeType, "RenderTexturePiP"));
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticPrefab", scopedOpticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-optic-4x",
                        WeaponAttachmentSlotType.Scope,
                        scopedOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-4x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                input.AimHeldValue = true;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                Assert.That(adsBridge, Is.Not.Null);
                SetField(adsBridge.GetType(), adsBridge, "_magnificationLerpSpeed", 1000f);

                var frames = 0;
                while ((float)GetProperty(adsBridge, "AdsT") < 0.999f && frames < 60)
                {
                    frames++;
                    yield return null;
                }

                Invoke(adsBridge, "SetMagnification", 8f);
                frames = 0;
                while (Mathf.Abs((float)GetProperty(adsBridge, "CurrentMagnification") - 8f) > 0.01f && frames < 10)
                {
                    frames++;
                    yield return null;
                }

                Assert.That((float)GetProperty(adsBridge, "CurrentMagnification"), Is.EqualTo(8f).Within(0.01f));

                yield return null;
                yield return null;
                yield return null;

                Assert.That((float)GetProperty(adsBridge, "CurrentMagnification"), Is.EqualTo(8f).Within(0.01f));
            }
            finally
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (worldCameraGo != null)
                {
                    Object.Destroy(worldCameraGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (scopedOpticDefinition != null)
                {
                    Object.Destroy(scopedOpticDefinition);
                }

                if (scopedOpticPrefab != null)
                {
                    Object.Destroy(scopedOpticPrefab);
                }
            }
        }

    }
}
