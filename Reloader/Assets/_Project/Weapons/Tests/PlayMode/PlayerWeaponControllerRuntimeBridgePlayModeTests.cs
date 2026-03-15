using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Audio;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Cinematics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Weapons.Tests.PlayMode
{
    public partial class PlayerWeaponControllerPlayModeTests
    {
        [Test]
        public void HasScopedAdsBridgeActive_ReturnsFalse_WhenBridgeHasNoActiveOptic()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            Assert.That(attachmentManagerType, Is.Not.Null);

            var root = new GameObject("PlayerRoot");
            var bridgeHost = new GameObject("AdsBridgeHost");
            var managerHost = new GameObject("AttachmentManagerHost");
            try
            {
                var controller = root.AddComponent<PlayerWeaponController>();
                var adsBridge = bridgeHost.AddComponent<MinimalBridgeMarker>();
                var manager = managerHost.AddComponent(attachmentManagerType);

                SetControllerField(controller, "_adsStateRuntimeBridge", adsBridge);
                SetControllerField(controller, "_adsAttachmentManagerRuntimeBridge", manager);

                var hasScopedAdsBridgeActive = typeof(PlayerWeaponController).GetMethod(
                    "HasScopedAdsBridgeActive",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(hasScopedAdsBridgeActive, Is.Not.Null);

                var isActive = (bool)hasScopedAdsBridgeActive.Invoke(controller, null);
                Assert.That(isActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(bridgeHost);
                Object.DestroyImmediate(managerHost);
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_RealKar98kScopeAsset_MountsIntoRuntimeViewWithDedicatedSightAnchor()
        {
#if UNITY_EDITOR
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            Assert.That(attachmentManagerType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;

            try
            {
                var realOpticDefinition = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    "Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset");
                Assert.That(realOpticDefinition, Is.Not.Null);

                root = new GameObject("PlayerRoot");
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);
                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);
                runtime.TryAddStackItem("att-kar98k-scope-remote-a", 1, out _, out _, out _);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-kar98k-scope-remote-a" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-kar98k-scope-remote-a",
                        WeaponAttachmentSlotType.Scope,
                        realOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;

                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-kar98k-scope-remote-a"), Is.True);
                yield return null;

                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                Assert.That(equippedView, Is.Not.Null);

                var manager = equippedView.GetComponent(attachmentManagerType);
                Assert.That(manager, Is.Not.Null);

                var equippedScopeSlot = equippedView.transform.Find("ScopeSlot");
                Assert.That(equippedScopeSlot, Is.Not.Null);
                Assert.That(equippedScopeSlot.childCount, Is.EqualTo(1));

                var activeAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
                Assert.That(activeAnchor, Is.Not.Null);
                Assert.That(activeAnchor, Is.Not.EqualTo(equippedScopeSlot.GetChild(0)), "Real Kar98k scope should not fall back to optic root as the sight anchor.");
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

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }
            }
#else
            Assert.Ignore("Requires UnityEditor AssetDatabase.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator ApplyRuntimeAttachments_RealKar98kScopeState_ActivatesScopedAdsBridgeWithoutManualSwap()
        {
#if UNITY_EDITOR
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            Assert.That(attachmentManagerType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            GameObject worldCameraGo = null;
            WeaponDefinition definition = null;

            try
            {
                var realOpticDefinition = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    "Assets/_Project/Weapons/Data/Attachments/Kar98kScopeRemoteA.asset");
                var rifleViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Weapons/Prefabs/RifleView.prefab");
                Assert.That(realOpticDefinition, Is.Not.Null);
                Assert.That(rifleViewPrefab, Is.Not.Null);

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
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                viewmodelCameraGo.AddComponent<Camera>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-kar98k-scope-remote-a" })
                });

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, rifleViewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-kar98k-scope-remote-a",
                        WeaponAttachmentSlotType.Scope,
                        realOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", rifleViewPrefab);

                var poseHelper = root.AddComponent<WeaponViewPoseTuningHelper>();
                var helperType = typeof(WeaponViewPoseTuningHelper);
                SetField(helperType, poseHelper, "_weaponController", controller);
                SetField(helperType, poseHelper, "_targetWeaponItemId", "weapon-kar98k");
                SetField(helperType, poseHelper, "_hipLocalPosition", new Vector3(0.015f, 0.15f, 0.005f));
                SetField(helperType, poseHelper, "_adsLocalPosition", new Vector3(0f, 0.2f, 0.077f));
                SetField(helperType, poseHelper, "_hipLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_rifleLocalEulerOffset", new Vector3(90f, 0f, 0f));
                SetField(helperType, poseHelper, "_blendSpeed", 24f);

                var overrideType = helperType.GetNestedType("AttachmentPoseOverride", BindingFlags.NonPublic);
                var overrides = Array.CreateInstance(overrideType, 1);
                var entry = Activator.CreateInstance(overrideType);
                SetField(overrideType, entry, "_slotType", WeaponAttachmentSlotType.Scope);
                SetField(overrideType, entry, "_attachmentItemId", "att-kar98k-scope-remote-a");
                SetField(overrideType, entry, "_hipLocalPosition", new Vector3(0.015f, 0.15f, 0.005f));
                SetField(overrideType, entry, "_hipLocalEuler", Vector3.zero);
                SetField(overrideType, entry, "_adsLocalPosition", new Vector3(0f, 0.2f, 0.05f));
                SetField(overrideType, entry, "_adsLocalEuler", Vector3.zero);
                SetField(overrideType, entry, "_rifleLocalEulerOffset", new Vector3(90f, 0f, 0f));
                SetField(overrideType, entry, "_blendSpeed", 24f);
                SetField(overrideType, entry, "_scopedAdsEyeReliefBackOffset", 0f);
                overrides.SetValue(entry, 0);
                SetField(helperType, poseHelper, "_attachmentPoseOverrides", overrides);

                yield return null;

                Assert.That(controller.HasActiveScopedAdsAlignment, Is.False, "Expected irons to start without an active scoped ADS bridge.");

                var applied = controller.ApplyRuntimeAttachments(
                    "weapon-kar98k",
                    new Dictionary<WeaponAttachmentSlotType, string>
                    {
                        [WeaponAttachmentSlotType.Scope] = "att-kar98k-scope-remote-a"
                    });
                Assert.That(applied, Is.True);

                yield return null;
                if (!Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }

                yield return null;

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);

                var manager = equippedView.GetComponent(attachmentManagerType);
                Assert.That(manager, Is.Not.Null);

                var activeOpticProperty = attachmentManagerType.GetProperty("ActiveOpticDefinition", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(activeOpticProperty, Is.Not.Null);
                var activeOptic = activeOpticProperty.GetValue(manager);
                Assert.That(activeOptic, Is.Not.Null, "Expected runtime attachment restore to produce a live active optic before any manual swap.");

                Assert.That(controller.HasActiveScopedAdsAlignment, Is.True, "Expected scoped attachment restore to light up the live scoped ADS bridge.");

                input.AimHeldValue = true;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                Assert.That(adsBridge, Is.Not.Null);

                var frames = 0;
                while ((float)GetProperty(adsBridge, "AdsT") < 0.999f && frames < 90)
                {
                    frames++;
                    yield return null;
                    if (!Application.isBatchMode)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }

                yield return null;
                if (!Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }

                var activeSightAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
                Assert.That(activeSightAnchor, Is.Not.Null);
                var mounts = equippedView.GetComponent<WeaponViewAttachmentMounts>();
                Assert.That(mounts, Is.Not.Null);
                Assert.That(
                    activeSightAnchor,
                    Is.Not.SameAs(mounts!.IronSightAnchor),
                    "Runtime attachment restore should keep AttachmentManager bound to the optic's authored SightAnchor instead of silently falling back to irons.");

                var scopedAlignmentDistance = Vector3.Distance(activeSightAnchor.position, worldCamera.transform.position);
                Assert.That(
                    scopedAlignmentDistance,
                    Is.LessThan(0.04f),
                    "Runtime attachment restore should align the real Kar98k sight anchor to the camera at full ADS instead of leaving the optic buried inside the stock.");
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
            }
#else
            Assert.Ignore("Requires UnityEditor AssetDatabase.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_AttachingScopeWhileAiming_DoesNotRebaseAdsPivotRestPose()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

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
                runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);

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
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var adsPivot = new GameObject("AdsPivot").transform;
                adsPivot.SetParent(viewPrefab.transform, false);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(adsPivot, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(adsPivot, false);
                ironSightAnchor.localPosition = new Vector3(0.05f, 0.02f, 0.4f);
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot: adsPivot, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                scopedOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                scopedOpticPrefab = new GameObject("OpticFourXPrefab");
                var sightAnchor = new GameObject("SightAnchor").transform;
                sightAnchor.SetParent(scopedOpticPrefab.transform, false);
                sightAnchor.localPosition = new Vector3(-0.04f, -0.01f, 0.28f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, scopedOpticDefinition, "_isVariableZoom", false);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMax", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "RenderTexturePiP"));
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

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);

                var runtimeAdsPivot = equippedView.Find("AdsPivot");
                Assert.That(runtimeAdsPivot, Is.Not.Null);
                var authoredRestLocalPosition = runtimeAdsPivot.localPosition;

                runtimeAdsPivot.localPosition = authoredRestLocalPosition + new Vector3(0.08f, -0.03f, 0.05f);
                runtimeAdsPivot.localRotation = Quaternion.Euler(7f, -4f, 3f);

                input.AimHeldValue = true;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                Assert.That(adsBridge, Is.Not.Null);

                var frames = 0;
                while ((float)GetProperty(adsBridge, "AdsT") < 0.999f && frames < 90)
                {
                    frames++;
                    yield return null;
                }

                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.True);
                yield return null;

                input.AimHeldValue = false;

                frames = 0;
                while ((float)GetProperty(adsBridge, "AdsT") > 0.001f && frames < 90)
                {
                    frames++;
                    yield return null;
                }

                for (var i = 0; i < 4; i++)
                {
                    yield return null;
                }

                Assert.That(
                    Vector3.Distance(runtimeAdsPivot.localPosition, authoredRestLocalPosition),
                    Is.LessThan(0.001f),
                    "Attaching a scope while ADS is active must not rebase the runtime AdsPivot rest pose to the in-progress irons alignment.");
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
        public IEnumerator EquipScopedWeapon_RuntimeBridge_WiresPipScopeControllerAndScopeCamera()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var peripheralEffectsType = ResolveType("Reloader.Game.Weapons.PeripheralScopeEffects");
            var weaponAimAlignerType = ResolveType("Reloader.Game.Weapons.WeaponAimAligner");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(peripheralEffectsType, Is.Not.Null);
            Assert.That(weaponAimAlignerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            GameObject worldCameraGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object opticDefinition = null;
            GameObject opticPrefab = null;

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
                runtime.TryAddStackItem("att-optic-pip", 1, out _, out _, out _);

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                viewmodelCameraGo.AddComponent<Camera>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-pip" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("OpticPiPPrefab");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-pip");
                SetField(opticDefinitionType, opticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 8f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, opticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "RenderTexturePiP"));
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-pip", WeaponAttachmentSlotType.Scope, opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                Assert.That(GetControllerField<GameObject>(controller, "_equippedWeaponView"), Is.Not.Null);
                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-pip"), Is.True);
                yield return null;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                Assert.That(adsBridge, Is.Not.Null);
                Assert.That(root.GetComponent(adsControllerType), Is.SameAs(adsBridge));

                var renderTextureScopeController = root.GetComponent(renderTextureScopeControllerType);
                Assert.That(renderTextureScopeController, Is.Not.Null);
                Assert.That(GetField(adsControllerType, adsBridge, "_renderTextureScopeController"), Is.SameAs(renderTextureScopeController));

                var peripheralEffects = root.GetComponent(peripheralEffectsType);
                Assert.That(peripheralEffects, Is.Not.Null);
                Assert.That(GetField(adsControllerType, adsBridge, "_peripheralScopeEffects"), Is.SameAs(peripheralEffects));

                var weaponAimAligner = root.GetComponent(weaponAimAlignerType);
                Assert.That(weaponAimAligner, Is.Not.Null, "Scoped PiP weapons should wire WeaponAimAligner for final camera-authoritative eye alignment.");

                var scopeCamera = worldCamera.transform.Find("ScopeCamera");
                Assert.That(scopeCamera, Is.Not.Null);
                var scopeCameraComponent = scopeCamera.GetComponent<Camera>();
                Assert.That(scopeCameraComponent, Is.Not.Null);
                Assert.That(GetField(renderTextureScopeControllerType, renderTextureScopeController, "_scopeCamera"), Is.SameAs(scopeCameraComponent));

                var additionalCameraDataType = ResolveType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
                Assert.That(additionalCameraDataType, Is.Not.Null, "Expected URP additional camera data type to be available.");
                var scopeCameraData = scopeCameraComponent.GetComponent(additionalCameraDataType);
                Assert.That(scopeCameraData, Is.Not.Null, "Runtime-created PiP scope camera should carry URP additional camera data so it renders as a real URP camera.");
                Assert.That(GetProperty(scopeCameraData, "renderType")?.ToString(), Is.EqualTo("Base"), "PiP scope camera should render as a standalone URP base camera instead of an overlay camera.");
                Assert.That(GetProperty(scopeCameraData, "cameraStack") as System.Collections.ICollection, Is.Empty,
                    "PiP scope camera should not inherit any overlay stack while rendering into its own target texture.");

                var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
                Assert.That(viewmodelLayer, Is.GreaterThanOrEqualTo(0), "Project should define a dedicated Viewmodel layer.");
                Assert.That((scopeCameraComponent.cullingMask & (1 << viewmodelLayer)) == 0, Is.True, "Scope camera must exclude the Viewmodel layer.");

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                Assert.That(equippedView.gameObject.layer, Is.EqualTo(viewmodelLayer), "Spawned runtime weapon view should render on the Viewmodel layer.");

                var equippedScopeSlot = equippedView.Find("ScopeSlot");
                Assert.That(equippedScopeSlot, Is.Not.Null);
                Assert.That(equippedScopeSlot.childCount, Is.EqualTo(1));
                Assert.That(equippedScopeSlot.GetChild(0).gameObject.layer, Is.EqualTo(viewmodelLayer), "Mounted scoped optics should inherit the Viewmodel layer.");
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

                if (opticDefinition != null)
                {
                    Object.Destroy(opticDefinition);
                }

                if (opticPrefab != null)
                {
                    Object.Destroy(opticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator EquipScopedWeapon_RuntimeBridgeRefresh_DoesNotResetLiveScopeCamera()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            GameObject worldCameraGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object opticDefinition = null;
            GameObject opticPrefab = null;

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
                runtime.TryAddStackItem("att-optic-pip-refresh", 1, out _, out _, out _);

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                viewmodelCameraGo.AddComponent<Camera>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-pip-refresh" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var adsPivot = new GameObject("AdsPivot").transform;
                adsPivot.SetParent(viewPrefab.transform, false);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(adsPivot, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(adsPivot, false);
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot: adsPivot, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("OpticPiPRefreshPrefab");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var lensDisplayGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                lensDisplayGo.name = "LensDisplay";
                lensDisplayGo.transform.SetParent(opticPrefab.transform, false);
                var prefabLensDisplay = lensDisplayGo.AddComponent(scopeLensDisplayType);
                SetField(scopeLensDisplayType, prefabLensDisplay, "_targetRenderer", lensDisplayGo.GetComponent<Renderer>());
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-pip-refresh");
                SetField(opticDefinitionType, opticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 8f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, opticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "RenderTexturePiP"));
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-pip-refresh", WeaponAttachmentSlotType.Scope, opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-pip-refresh"), Is.True);
                yield return null;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                var attachmentManager = equippedView != null ? equippedView.GetComponent(attachmentManagerType) : null;
                var renderTextureScopeController = root.GetComponent(renderTextureScopeControllerType);
                var scopeCamera = worldCamera.transform.Find("ScopeCamera")?.GetComponent<Camera>();
                Assert.That(adsBridge, Is.Not.Null);
                Assert.That(equippedView, Is.Not.Null);
                Assert.That(attachmentManager, Is.Not.Null);
                Assert.That(renderTextureScopeController, Is.Not.Null);
                Assert.That(scopeCamera, Is.Not.Null);

                input.AimHeldValue = true;
                Invoke(adsBridge, "SetMagnification", 6f);
                var activationElapsed = 0f;
                while ((!scopeCamera.enabled || scopeCamera.targetTexture == null) && activationElapsed < 0.5f)
                {
                    activationElapsed += Time.deltaTime;
                    yield return null;
                }

                var originalTargetTexture = scopeCamera.targetTexture;
                Assert.That(scopeCamera.enabled, Is.True);
                Assert.That(originalTargetTexture, Is.Not.Null);

                Invoke(controller, "EnsureScopedAdsRuntimeBridge", equippedView, attachmentManager);
                var refreshElapsed = 0f;
                while ((!scopeCamera.enabled || !ReferenceEquals(scopeCamera.targetTexture, originalTargetTexture)) && refreshElapsed < 0.5f)
                {
                    refreshElapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.That(scopeCamera.enabled, Is.True, "Refreshing the scoped ADS bridge should not disable the live PiP scope camera.");
                Assert.That(scopeCamera.targetTexture, Is.SameAs(originalTargetTexture), "Refreshing the scoped ADS bridge should preserve the live PiP render texture binding.");
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

                if (opticDefinition != null)
                {
                    Object.Destroy(opticDefinition);
                }

                if (opticPrefab != null)
                {
                    Object.Destroy(opticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator EquipScopedWeapon_RuntimeBridge_WiresWeaponAimAlignerForPipOptics()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var aimAlignerType = ResolveType("Reloader.Game.Weapons.WeaponAimAligner");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsControllerType, Is.Not.Null);
            Assert.That(aimAlignerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            GameObject worldCameraGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object opticDefinition = null;
            GameObject opticPrefab = null;

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
                runtime.TryAddStackItem("att-optic-pip-aligner", 1, out _, out _, out _);

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                viewmodelCameraGo.AddComponent<Camera>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-pip-aligner" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var adsPivot = new GameObject("AdsPivot").transform;
                adsPivot.SetParent(viewPrefab.transform, false);
                adsPivot.localPosition = new Vector3(0.11f, -0.07f, 0.23f);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(adsPivot, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(adsPivot, false);
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot: adsPivot, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("OpticPiPAlignerPrefab");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-pip-aligner");
                SetField(opticDefinitionType, opticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 8f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, opticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "RenderTexturePiP"));
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-pip-aligner", WeaponAttachmentSlotType.Scope, opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;

                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-pip-aligner"), Is.True);
                yield return null;

                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                var attachmentManager = equippedView != null ? equippedView.GetComponent(attachmentManagerType) : null;
                var aimAligner = root.GetComponent(aimAlignerType);
                Assert.That(aimAligner, Is.Not.Null, "PiP optic runtime should add a WeaponAimAligner bridge.");
                Assert.That(GetField(aimAlignerType, aimAligner, "_attachmentManager"), Is.SameAs(attachmentManager));
                Assert.That(GetField(aimAlignerType, aimAligner, "_adsStateController"), Is.SameAs(adsBridge));
                Assert.That(GetField(aimAlignerType, aimAligner, "_cameraTransform"), Is.SameAs(worldCamera.transform));
                var runtimeAdsPivot = equippedView.transform.Find("AdsPivot");
                Assert.That(runtimeAdsPivot, Is.Not.Null);
                Assert.That(GetField(aimAlignerType, aimAligner, "_adsPivot"), Is.SameAs(runtimeAdsPivot));
                Assert.That(GetField(aimAlignerType, aimAligner, "_pivotParent"), Is.SameAs(equippedView.transform), "Runtime-wired aim aligner should refresh its cached pivot parent after AdsPivot assignment.");
                Assert.That((Vector3)GetField(aimAlignerType, aimAligner, "_restLocalPosition"), Is.EqualTo(new Vector3(0.11f, -0.07f, 0.23f)));

                controller.SetScopedAdsPresentationEyeReliefOffset(0.19f);
                yield return null;

                Assert.That(
                    GetField(aimAlignerType, aimAligner, "_runtimeEyeReliefBackOffset"),
                    Is.EqualTo(0.19f).Within(0.001f),
                    "Scoped pose tuning should forward attachment-specific eye-relief offsets into the live WeaponAimAligner bridge.");
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

                if (opticDefinition != null)
                {
                    Object.Destroy(opticDefinition);
                }

                if (opticPrefab != null)
                {
                    Object.Destroy(opticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator EquipScopedWeapon_RuntimeViewAndMountedOptic_UseViewmodelLayer()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            Assert.That(viewmodelLayer, Is.GreaterThanOrEqualTo(0), "Project should define a Viewmodel layer.");

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject weaponViewParent = null;
            GameObject viewPrefab = null;
            UnityEngine.Object opticDefinition = null;
            GameObject opticPrefab = null;

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
                runtime.TryAddStackItem("att-optic-viewmodel-layer", 1, out _, out _, out _);

                weaponViewParent = new GameObject("WeaponViewParent");
                weaponViewParent.transform.SetParent(root.transform, false);
                weaponViewParent.layer = viewmodelLayer;

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-viewmodel-layer" })
                });

                viewPrefab = new GameObject("Kar98kViewDefaultLayer");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                var decorativeChild = new GameObject("DecorativeChild");
                decorativeChild.transform.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("OpticDefaultLayerPrefab");
                var opticChild = new GameObject("OpticChild");
                opticChild.transform.SetParent(opticPrefab.transform, false);
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-viewmodel-layer");
                SetField(opticDefinitionType, opticDefinition, "_isVariableZoom", false);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, opticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "RenderTexturePiP"));
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_weaponViewParent", weaponViewParent.transform);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-viewmodel-layer", WeaponAttachmentSlotType.Scope, opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-viewmodel-layer"), Is.True);
                yield return null;

                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                Assert.That(equippedView, Is.Not.Null);
                Assert.That(equippedView.layer, Is.EqualTo(viewmodelLayer), "Spawned runtime weapon view should move onto the Viewmodel layer.");
                Assert.That(equippedView.transform.GetChild(0).gameObject.layer, Is.EqualTo(viewmodelLayer), "Spawned runtime weapon-view descendants should move onto the Viewmodel layer.");

                var mountedOpticRoot = equippedView.transform.Find("ScopeSlot")?.GetChild(0);
                Assert.That(mountedOpticRoot, Is.Not.Null, "Mounted optic should exist after a successful scope swap.");
                Assert.That(mountedOpticRoot.gameObject.layer, Is.EqualTo(viewmodelLayer), "Mounted optic root should inherit the Viewmodel layer.");
                Assert.That(mountedOpticRoot.GetChild(0).gameObject.layer, Is.EqualTo(viewmodelLayer), "Mounted optic descendants should inherit the Viewmodel layer.");
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

                if (definition != null)
                {
                    Object.Destroy(definition);
                }

                if (weaponViewParent != null)
                {
                    Object.Destroy(weaponViewParent);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (opticDefinition != null)
                {
                    Object.Destroy(opticDefinition);
                }

                if (opticPrefab != null)
                {
                    Object.Destroy(opticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator EquipScopedWeapon_ScopeCamera_ExcludesViewmodelLayer()
        {
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);

            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            Assert.That(viewmodelLayer, Is.GreaterThanOrEqualTo(0), "Project should define a Viewmodel layer.");

            GameObject root = null;
            GameObject registryGo = null;
            GameObject worldCameraGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object opticDefinition = null;
            GameObject opticPrefab = null;

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
                runtime.TryAddStackItem("att-optic-scope-camera", 1, out _, out _, out _);

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                worldCamera.tag = "MainCamera";
                worldCamera.cullingMask = (1 << 0) | (1 << viewmodelLayer);
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(worldCamera.transform, false);
                viewmodelCameraGo.AddComponent<Camera>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-scope-camera" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("OpticScopeCameraPrefab");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-scope-camera");
                SetField(opticDefinitionType, opticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 8f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, opticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "RenderTexturePiP"));
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_adsCamera", worldCamera);
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-scope-camera", WeaponAttachmentSlotType.Scope, opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;
                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-scope-camera"), Is.True);
                yield return null;

                var renderTextureScopeController = root.GetComponent(renderTextureScopeControllerType);
                Assert.That(renderTextureScopeController, Is.Not.Null);

                var scopeCamera = worldCamera.transform.Find("ScopeCamera")?.GetComponent<Camera>();
                Assert.That(scopeCamera, Is.Not.Null);
                Assert.That((worldCamera.cullingMask & (1 << viewmodelLayer)) != 0, Is.True, "Test setup must prove the world camera includes the Viewmodel layer.");
                Assert.That((scopeCamera.cullingMask & (1 << viewmodelLayer)) == 0, Is.True, "Scope camera should exclude the Viewmodel layer to avoid rendering weapon-view geometry inside the scope.");
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

                if (opticDefinition != null)
                {
                    Object.Destroy(opticDefinition);
                }

                if (opticPrefab != null)
                {
                    Object.Destroy(opticPrefab);
                }
            }
        }

    }
}
