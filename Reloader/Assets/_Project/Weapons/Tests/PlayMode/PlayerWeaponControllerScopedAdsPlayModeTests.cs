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
        public void RifleViewPrefab_ExposesExplicitAttachmentMountContract()
        {
#if UNITY_EDITOR
            var mountsType = ResolveType("Reloader.Weapons.Runtime.WeaponViewAttachmentMounts");
            Assert.That(mountsType, Is.Not.Null, "Refactor contract requires an explicit weapon-view attachment mount component.");

            var rifleViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Weapons/Prefabs/RifleView.prefab");
            Assert.That(rifleViewPrefab, Is.Not.Null);

            var mounts = rifleViewPrefab.GetComponent(mountsType);
            Assert.That(mounts, Is.Not.Null, "Rifle runtime view should expose attachment slots explicitly instead of relying on name heuristics.");

            var adsPivot = GetProperty(mounts, "AdsPivot") as Transform;
            Assert.That(adsPivot, Is.Not.Null, "Rifle runtime view should expose an authored AdsPivot for camera-authoritative ADS alignment.");

            var ironSightAnchor = GetProperty(mounts, "IronSightAnchor") as Transform;
            Assert.That(ironSightAnchor, Is.Not.Null);

            var tryGetSlot = mountsType.GetMethod("TryGetAttachmentSlot", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(tryGetSlot, Is.Not.Null);

            var scopeArgs = new object[] { WeaponAttachmentSlotType.Scope, null };
            var muzzleArgs = new object[] { WeaponAttachmentSlotType.Muzzle, null };
            Assert.That((bool)tryGetSlot.Invoke(mounts, scopeArgs), Is.True);
            Assert.That(scopeArgs[1] as Transform, Is.Not.Null);
            Assert.That((bool)tryGetSlot.Invoke(mounts, muzzleArgs), Is.True);
            Assert.That(muzzleArgs[1] as Transform, Is.Not.Null);
#else
            Assert.Ignore("Requires UnityEditor AssetDatabase.");
#endif
        }

        [UnityTest]
        public IEnumerator EquipWeapon_DoesNotSeedScopeAttachmentStateFromAuthoredViewVisual()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
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

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-seeded" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("OpticSeededPrefab");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-seeded");
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);

                var authoredVisual = new GameObject("OpticSeededPrefab_AuthedViewMesh");
                authoredVisual.transform.SetParent(viewPrefab.transform, false);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-optic-seeded",
                        WeaponAttachmentSlotType.Scope,
                        opticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                yield return null;

                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.Empty);

                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                Assert.That(equippedView, Is.Not.Null);
                var runtimeScopeSlot = equippedView.transform.Find("ScopeSlot");
                Assert.That(runtimeScopeSlot, Is.Not.Null);
                Assert.That(runtimeScopeSlot.childCount, Is.EqualTo(0), "Spawned runtime view should stay empty until the player explicitly equips an attachment.");
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
        public IEnumerator WeaponViewPoseTuningHelper_WithZeroAuthoredPose_DoesNotInjectKar98kDefaultsAtRuntime()
        {
            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;

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

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);

                viewPrefab = new GameObject("Kar98kView");
                ConfigureTestWeaponViewMounts(viewPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                var poseHelper = root.AddComponent<WeaponViewPoseTuningHelper>();
                var helperType = typeof(WeaponViewPoseTuningHelper);
                SetField(helperType, poseHelper, "_weaponController", controller);
                SetField(helperType, poseHelper, "_targetWeaponItemId", "weapon-kar98k");
                SetField(helperType, poseHelper, "_hipLocalPosition", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalPosition", Vector3.zero);
                SetField(helperType, poseHelper, "_hipLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(helperType, poseHelper, "_blendSpeed", 24f);

                yield return null;

                var baseValues = poseHelper.GetBasePoseValues();
                Assert.That(baseValues.HipLocalPosition, Is.EqualTo(Vector3.zero));
                Assert.That(baseValues.AdsLocalPosition, Is.EqualTo(Vector3.zero));

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                Assert.That(equippedView.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(equippedView.localRotation, Is.EqualTo(Quaternion.identity));
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
        }

        [UnityTest]
        public IEnumerator WeaponViewPoseTuningHelper_AimingWithOneXOptic_StillAppliesAdsPose()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object oneXOpticDefinition = null;
            GameObject oneXOpticPrefab = null;

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

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-1x" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                oneXOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                oneXOpticPrefab = new GameObject("OpticOneXPrefab");
                new GameObject("SightAnchor").transform.SetParent(oneXOpticPrefab.transform, false);
                SetField(opticDefinitionType, oneXOpticDefinition, "_opticId", "att-optic-1x");
                SetField(opticDefinitionType, oneXOpticDefinition, "_isVariableZoom", false);
                SetField(opticDefinitionType, oneXOpticDefinition, "_magnificationMin", 1f);
                SetField(opticDefinitionType, oneXOpticDefinition, "_magnificationMax", 1f);
                SetField(opticDefinitionType, oneXOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, oneXOpticDefinition, "_opticPrefab", oneXOpticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-optic-1x",
                        WeaponAttachmentSlotType.Scope,
                        oneXOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                var poseHelper = root.AddComponent<WeaponViewPoseTuningHelper>();
                SetField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_weaponController", controller);
                SetField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_targetWeaponItemId", "weapon-kar98k");
                SetField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_hipLocalPosition", Vector3.zero);
                SetField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_adsLocalPosition", new Vector3(0.25f, 0f, 0f));
                SetField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_hipLocalEuler", Vector3.zero);
                SetField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_adsLocalEuler", Vector3.zero);
                SetField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(typeof(WeaponViewPoseTuningHelper), poseHelper, "_blendSpeed", 1000f);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-1x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                equippedView.localPosition = Vector3.zero;
                input.AimHeldValue = true;

                var aimElapsed = 0f;
                while (equippedView.localPosition.x <= 0.1f && aimElapsed < 0.5f)
                {
                    aimElapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.That(equippedView.localPosition.x, Is.GreaterThan(0.1f), "1x optics should still use tuned ADS pose instead of bypassing pose updates.");
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

                if (oneXOpticDefinition != null)
                {
                    Object.Destroy(oneXOpticDefinition);
                }

                if (oneXOpticPrefab != null)
                {
                    Object.Destroy(oneXOpticPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator WeaponViewPoseTuningHelper_AimingWithMagnifiedScope_UsesAttachmentSpecificEyeReliefOffset()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
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

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                scopedOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                scopedOpticPrefab = new GameObject("OpticFourXPrefab");
                new GameObject("SightAnchor").transform.SetParent(scopedOpticPrefab.transform, false);
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, scopedOpticDefinition, "_isVariableZoom", false);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMax", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticPrefab", scopedOpticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-optic-4x",
                        WeaponAttachmentSlotType.Scope,
                        scopedOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                var poseHelper = root.AddComponent<WeaponViewPoseTuningHelper>();
                var helperType = typeof(WeaponViewPoseTuningHelper);
                SetField(helperType, poseHelper, "_weaponController", controller);
                SetField(helperType, poseHelper, "_targetWeaponItemId", "weapon-kar98k");
                SetField(helperType, poseHelper, "_hipLocalPosition", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalPosition", new Vector3(0.1f, 0f, 0f));
                SetField(helperType, poseHelper, "_hipLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(helperType, poseHelper, "_blendSpeed", 1000f);

                var overrideType = helperType.GetNestedType("AttachmentPoseOverride", BindingFlags.NonPublic);
                Assert.That(overrideType, Is.Not.Null, "Attachment-specific pose overrides should be supported for scoped ADS tuning.");
                var overrides = Array.CreateInstance(overrideType, 1);
                var entry = Activator.CreateInstance(overrideType);
                SetField(overrideType, entry, "_slotType", WeaponAttachmentSlotType.Scope);
                SetField(overrideType, entry, "_attachmentItemId", "att-optic-4x");
                SetField(overrideType, entry, "_hipLocalPosition", Vector3.zero);
                SetField(overrideType, entry, "_hipLocalEuler", Vector3.zero);
                SetField(overrideType, entry, "_adsLocalPosition", new Vector3(0.4f, 0f, 0f));
                SetField(overrideType, entry, "_adsLocalEuler", Vector3.zero);
                SetField(overrideType, entry, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(overrideType, entry, "_blendSpeed", 1000f);
                SetField(overrideType, entry, "_scopedAdsEyeReliefBackOffset", 0.18f);
                overrides.SetValue(entry, 0);
                SetField(helperType, poseHelper, "_attachmentPoseOverrides", overrides);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-4x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                equippedView.localPosition = Vector3.zero;
                input.AimHeldValue = true;

                yield return null;

                Assert.That(
                    controller.ScopedAdsPresentationEyeReliefOffset,
                    Is.EqualTo(0.18f).Within(0.001f),
                    "Magnified scoped optics should drive their attachment-specific full-ADS presentation through the scoped aligner eye-relief offset.");
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
        public IEnumerator WeaponViewPoseTuningHelper_AimingWithMagnifiedScope_DoesNotDoubleSmoothAdsBlend()
        {
            var adsControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(adsControllerType, Is.Not.Null);
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
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot: adsPivot, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                scopedOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                scopedOpticPrefab = new GameObject("OpticFourXPrefab");
                new GameObject("SightAnchor").transform.SetParent(scopedOpticPrefab.transform, false);
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

                var poseHelper = root.AddComponent<WeaponViewPoseTuningHelper>();
                var helperType = typeof(WeaponViewPoseTuningHelper);
                SetField(helperType, poseHelper, "_weaponController", controller);
                SetField(helperType, poseHelper, "_targetWeaponItemId", "weapon-kar98k");
                SetField(helperType, poseHelper, "_hipLocalPosition", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalPosition", new Vector3(0f, 0.2f, 0.05f));
                SetField(helperType, poseHelper, "_hipLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(helperType, poseHelper, "_blendSpeed", 24f);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-4x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var bridgeFrames = 0;
                while (!controller.HasActiveScopedAdsAlignment && bridgeFrames < 60)
                {
                    bridgeFrames++;
                    yield return null;
                }

                Assert.That(controller.HasActiveScopedAdsAlignment, Is.True);
                input.AimHeldValue = true;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                Assert.That(adsBridge, Is.Not.Null);

                var syncFrames = 0;
                while (syncFrames < 60)
                {
                    syncFrames++;
                    yield return null;
                    if (!Application.isBatchMode)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    var adsBlendSample = (float)GetProperty(adsBridge, "AdsT");
                    var helperBlendSample = (float)GetField(helperType, poseHelper, "_blendT");
                    if (Mathf.Abs(helperBlendSample - adsBlendSample) <= 0.001f)
                    {
                        break;
                    }
                }

                var adsBlendT = (float)GetProperty(adsBridge, "AdsT");
                var helperBlendT = (float)GetField(helperType, poseHelper, "_blendT");

                Assert.That(
                    helperBlendT,
                    Is.EqualTo(adsBlendT).Within(0.001f),
                    "Magnified scoped pose tuning should track AdsStateController.AdsT directly instead of smoothing it a second time.");
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
        public IEnumerator EquipScopedWeapon_WhenAdsBlendReachesOne_SightAnchorIsAlreadyAligned()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsControllerType, Is.Not.Null);
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
                ConfigureTestWeaponViewMounts(viewPrefab, adsPivot: adsPivot, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                scopedOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                scopedOpticPrefab = new GameObject("OpticFourXPrefab");
                var sightAnchor = new GameObject("SightAnchor").transform;
                sightAnchor.SetParent(scopedOpticPrefab.transform, false);
                sightAnchor.localPosition = new Vector3(0.35f, 0.18f, 1.6f);
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
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-4x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var bridgeFrames = 0;
                while (!controller.HasActiveScopedAdsAlignment && bridgeFrames < 60)
                {
                    bridgeFrames++;
                    yield return null;
                }

                Assert.That(controller.HasActiveScopedAdsAlignment, Is.True);
                input.AimHeldValue = true;

                var adsBridge = GetControllerField<Component>(controller, "_adsStateRuntimeBridge");
                Assert.That(adsBridge, Is.Not.Null);

                var frames = 0;
                while ((float)GetProperty(adsBridge, "AdsT") < 0.999f && frames < 60)
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

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                var manager = equippedView.GetComponent(attachmentManagerType);
                Assert.That(manager, Is.Not.Null);
                var activeSightAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
                Assert.That(activeSightAnchor, Is.Not.Null);

                var initialAlignmentDistance = Vector3.Distance(activeSightAnchor.position, worldCamera.transform.position);

                for (var i = 0; i < 10; i++)
                {
                    yield return null;
                    if (!Application.isBatchMode)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }

                var settledAlignmentDistance = Vector3.Distance(activeSightAnchor.position, worldCamera.transform.position);
                Assert.That(
                    initialAlignmentDistance,
                    Is.EqualTo(settledAlignmentDistance).Within(0.002f),
                    "Once scoped ADS blend reaches 1, the sight anchor should already be aligned instead of continuing to drift into place across later frames.");
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
        public IEnumerator Diagnostic_RealKar98kScopeAlignment_LogsDistanceAtAdsCompletion()
        {
#if UNITY_EDITOR
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var aimAlignerType = ResolveType("Reloader.Game.Weapons.WeaponAimAligner");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsControllerType, Is.Not.Null);
            Assert.That(aimAlignerType, Is.Not.Null);

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
                runtime.TryAddStackItem("att-kar98k-scope-remote-a", 1, out _, out _, out _);

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
                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-kar98k-scope-remote-a"), Is.True);
                yield return null;

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

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                var manager = equippedView.GetComponent(attachmentManagerType);
                var aligner = root.GetComponent(aimAlignerType);
                var activeSightAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
                var distanceAtAdsOne = Vector3.Distance(activeSightAnchor.position, worldCamera.transform.position);
                var alignerDistanceAtAdsOne = (float)GetProperty(aligner, "DebugAlignmentErrorDistance");

                for (var i = 0; i < 10; i++)
                {
                    yield return null;
                    if (!Application.isBatchMode)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }

                var distanceAfterSettle = Vector3.Distance(activeSightAnchor.position, worldCamera.transform.position);
                var alignerDistanceAfterSettle = (float)GetProperty(aligner, "DebugAlignmentErrorDistance");
                Debug.Log(
                    $"[DIAG] RealKar98kScope distance@ads1={distanceAtAdsOne:F4} aligner={alignerDistanceAtAdsOne:F4} distance@settled={distanceAfterSettle:F4} alignerSettled={alignerDistanceAfterSettle:F4}",
                    root);

                Assert.Pass();
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
        public IEnumerator TrySwapEquippedWeaponAttachment_RealKar98kScopeAsset_AdsCycleRestoresViewAndAdsPivotPose()
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
                runtime.TryAddStackItem("att-kar98k-scope-remote-a", 1, out _, out _, out _);

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
                overrides.SetValue(entry, 0);
                SetField(helperType, poseHelper, "_attachmentPoseOverrides", overrides);

                yield return null;

                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-kar98k-scope-remote-a"), Is.True);
                yield return null;
                if (!Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                var runtimeAdsPivot = equippedView.Find("AdsPivot");
                Assert.That(runtimeAdsPivot, Is.Not.Null, "Expected the real Kar98k view to expose an authored AdsPivot.");

                var restViewPosition = equippedView.localPosition;
                var restViewRotation = equippedView.localRotation;
                var restAdsPivotPosition = runtimeAdsPivot.localPosition;
                var restAdsPivotRotation = runtimeAdsPivot.localRotation;

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

                var manager = equippedView.GetComponent(attachmentManagerType);
                Assert.That(manager, Is.Not.Null);
                var activeSightAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
                Assert.That(activeSightAnchor, Is.Not.Null);
                var scopedAlignmentDistance = Vector3.Distance(activeSightAnchor.position, worldCamera.transform.position);
                Assert.That(
                    scopedAlignmentDistance,
                    Is.LessThan(0.04f),
                    "Live Kar98k scope swaps should align the active sight anchor to the camera at full ADS instead of aiming through the stock.");

                input.AimHeldValue = false;
                frames = 0;
                while ((float)GetProperty(adsBridge, "AdsT") > 0.001f && frames < 90)
                {
                    frames++;
                    yield return null;
                    if (!Application.isBatchMode)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }

                for (var i = 0; i < 4; i++)
                {
                    yield return null;
                    if (!Application.isBatchMode)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }

                Assert.That(
                    Vector3.Distance(equippedView.localPosition, restViewPosition),
                    Is.LessThan(0.01f),
                    "Scoped ADS should return the real Kar98k view root to its pre-aim pose after RMB is released.");
                Assert.That(
                    Quaternion.Angle(equippedView.localRotation, restViewRotation),
                    Is.LessThan(0.5f),
                    "Scoped ADS should restore the real Kar98k view root rotation after RMB is released.");
                Assert.That(
                    Vector3.Distance(runtimeAdsPivot.localPosition, restAdsPivotPosition),
                    Is.LessThan(0.01f),
                    "Scoped ADS should restore the real Kar98k AdsPivot to its pre-aim rest pose after RMB is released.");
                Assert.That(
                    Quaternion.Angle(runtimeAdsPivot.localRotation, restAdsPivotRotation),
                    Is.LessThan(0.5f),
                    "Scoped ADS should restore the real Kar98k AdsPivot rotation after RMB is released.");
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
        public IEnumerator WeaponViewPoseTuningHelper_RuntimeScopeOverrideApi_CanInspectAndRetuneActiveOverride()
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            Assert.That(opticDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
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

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Kar98k", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
                });

                viewPrefab = new GameObject("Kar98kView");
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ConfigureTestWeaponViewMounts(viewPrefab, scopeSlot: scopeSlot, ironSightAnchor: ironSightAnchor);

                scopedOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                scopedOpticPrefab = new GameObject("OpticFourXPrefab");
                new GameObject("SightAnchor").transform.SetParent(scopedOpticPrefab.transform, false);
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, scopedOpticDefinition, "_isVariableZoom", false);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMax", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_opticPrefab", scopedOpticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-optic-4x",
                        WeaponAttachmentSlotType.Scope,
                        scopedOpticDefinition)
                });
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);

                var poseHelper = root.AddComponent<WeaponViewPoseTuningHelper>();
                var helperType = typeof(WeaponViewPoseTuningHelper);
                SetField(helperType, poseHelper, "_weaponController", controller);
                SetField(helperType, poseHelper, "_targetWeaponItemId", "weapon-kar98k");
                SetField(helperType, poseHelper, "_hipLocalPosition", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalPosition", new Vector3(0.1f, 0f, 0f));
                SetField(helperType, poseHelper, "_hipLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(helperType, poseHelper, "_blendSpeed", 1000f);

                var overrideType = helperType.GetNestedType("AttachmentPoseOverride", BindingFlags.NonPublic);
                Assert.That(overrideType, Is.Not.Null);
                var overrides = Array.CreateInstance(overrideType, 1);
                var entry = Activator.CreateInstance(overrideType);
                SetField(overrideType, entry, "_slotType", WeaponAttachmentSlotType.Scope);
                SetField(overrideType, entry, "_attachmentItemId", "att-optic-4x");
                SetField(overrideType, entry, "_hipLocalPosition", Vector3.zero);
                SetField(overrideType, entry, "_hipLocalEuler", Vector3.zero);
                SetField(overrideType, entry, "_adsLocalPosition", new Vector3(0.4f, 0f, 0f));
                SetField(overrideType, entry, "_adsLocalEuler", Vector3.zero);
                SetField(overrideType, entry, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(overrideType, entry, "_blendSpeed", 1000f);
                SetField(overrideType, entry, "_scopedAdsEyeReliefBackOffset", 0.16f);
                overrides.SetValue(entry, 0);
                SetField(helperType, poseHelper, "_attachmentPoseOverrides", overrides);

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-4x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                input.AimHeldValue = true;
                yield return null;

                Assert.That(
                    poseHelper.TryGetActiveAttachmentPoseOverride(out var activeSlot, out var activeAttachmentItemId, out var activeValues),
                    Is.True,
                    "Play-mode tuning needs to inspect the active scoped override without relying on fragile inspector serialization.");
                Assert.That(activeSlot, Is.EqualTo(WeaponAttachmentSlotType.Scope));
                Assert.That(activeAttachmentItemId, Is.EqualTo("att-optic-4x"));
                Assert.That(activeValues.AdsLocalPosition.x, Is.EqualTo(0.4f).Within(0.001f));
                Assert.That(activeValues.ScopedAdsEyeReliefBackOffset, Is.EqualTo(0.16f).Within(0.001f));
                Assert.That(poseHelper.TryGetRuntimeTuningContext(out var context), Is.True);
                Assert.That(context.HasActiveAttachmentOverride, Is.True);
                Assert.That(context.UsesMagnifiedScopedAlignment, Is.True);
                Assert.That(context.AttachmentItemId, Is.EqualTo("att-optic-4x"));

                activeValues.AdsLocalPosition = new Vector3(0.6f, 0f, 0f);
                activeValues.ScopedAdsEyeReliefBackOffset = 0.33f;
                Assert.That(
                    poseHelper.TrySetAttachmentPoseOverride(activeSlot, activeAttachmentItemId, activeValues),
                    Is.True,
                    "Play-mode tuning needs to push edited values back into the active scoped override.");

                equippedView.localPosition = Vector3.zero;
                yield return null;

                Assert.That(controller.ScopedAdsPresentationEyeReliefOffset, Is.EqualTo(0.33f).Within(0.001f));
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
        public IEnumerator WeaponViewPoseTuningHelper_RuntimeScopeOverrideApi_RetuneMovesScopedAdsPivot()
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
                SetField(opticDefinitionType, scopedOpticDefinition, "_isVariableZoom", false);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, scopedOpticDefinition, "_magnificationMax", 4f);
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

                var poseHelper = root.AddComponent<WeaponViewPoseTuningHelper>();
                var helperType = typeof(WeaponViewPoseTuningHelper);
                SetField(helperType, poseHelper, "_weaponController", controller);
                SetField(helperType, poseHelper, "_targetWeaponItemId", "weapon-kar98k");
                SetField(helperType, poseHelper, "_hipLocalPosition", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalPosition", Vector3.zero);
                SetField(helperType, poseHelper, "_hipLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_adsLocalEuler", Vector3.zero);
                SetField(helperType, poseHelper, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(helperType, poseHelper, "_blendSpeed", 1000f);

                var overrideType = helperType.GetNestedType("AttachmentPoseOverride", BindingFlags.NonPublic);
                Assert.That(overrideType, Is.Not.Null);
                var overrides = Array.CreateInstance(overrideType, 1);
                var entry = Activator.CreateInstance(overrideType);
                SetField(overrideType, entry, "_slotType", WeaponAttachmentSlotType.Scope);
                SetField(overrideType, entry, "_attachmentItemId", "att-optic-4x");
                SetField(overrideType, entry, "_hipLocalPosition", Vector3.zero);
                SetField(overrideType, entry, "_hipLocalEuler", Vector3.zero);
                SetField(overrideType, entry, "_adsLocalPosition", Vector3.zero);
                SetField(overrideType, entry, "_adsLocalEuler", Vector3.zero);
                SetField(overrideType, entry, "_rifleLocalEulerOffset", Vector3.zero);
                SetField(overrideType, entry, "_blendSpeed", 1000f);
                SetField(overrideType, entry, "_scopedAdsEyeReliefBackOffset", 0f);
                overrides.SetValue(entry, 0);
                SetField(helperType, poseHelper, "_attachmentPoseOverrides", overrides);

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

                var frames = 0;
                while ((float)GetProperty(adsBridge, "AdsT") < 0.999f && frames < 60)
                {
                    frames++;
                    yield return null;
                    if (!Application.isBatchMode)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }

                var equippedView = controller.EquippedWeaponViewTransform;
                Assert.That(equippedView, Is.Not.Null);
                var runtimeAdsPivot = equippedView.Find("AdsPivot");
                Assert.That(runtimeAdsPivot, Is.Not.Null);
                var beforeRetune = runtimeAdsPivot.localPosition;

                Assert.That(
                    poseHelper.TryGetActiveAttachmentPoseOverride(out var activeSlot, out var activeAttachmentItemId, out var activeValues),
                    Is.True);
                activeValues.ScopedAdsEyeReliefBackOffset = 0.33f;
                Assert.That(poseHelper.TrySetAttachmentPoseOverride(activeSlot, activeAttachmentItemId, activeValues), Is.True);

                yield return null;
                if (!Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }

                var afterRetune = runtimeAdsPivot.localPosition;
                Assert.That(
                    Vector3.Distance(beforeRetune, afterRetune),
                    Is.GreaterThan(0.05f),
                    "Changing the active scoped eye-relief offset in play mode should move the live AdsPivot, not just update cached numbers.");
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
