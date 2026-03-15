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
    public class PlayerWeaponControllerPlayModeTests
    {
        private IGameEventsRuntimeHub _runtimeEventsBeforeEachTest;
        private readonly HashSet<int> _baselineRootInstanceIds = new();

        [SetUp]
        public void SetUp()
        {
            _runtimeEventsBeforeEachTest = RuntimeKernelBootstrapper.Events;
            _baselineRootInstanceIds.Clear();

            var activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                _baselineRootInstanceIds.Add(root.GetInstanceID());
            }
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Events = _runtimeEventsBeforeEachTest;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            RuntimeKernelBootstrapper.Events = _runtimeEventsBeforeEachTest;

            var activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null || _baselineRootInstanceIds.Contains(root.GetInstanceID()))
                {
                    continue;
                }

                Object.Destroy(root);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Configure_InjectedWeaponAndInventoryEvents_RaisesThroughInjectedPortsOnly()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var fallbackRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = fallbackRuntimeEvents;

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            var fallbackHandlersRegistered = false;

            var fallbackEquipRaised = 0;
            var fallbackFireRaised = 0;
            var fallbackReloadRaised = 0;
            var fallbackInventoryChangedRaised = 0;
            void HandleFallbackWeaponEquipped(string _) => fallbackEquipRaised++;
            void HandleFallbackWeaponFired(string _, Vector3 __, Vector3 ___) => fallbackFireRaised++;
            void HandleFallbackWeaponReloaded(string _, int __, int ___) => fallbackReloadRaised++;
            void HandleFallbackInventoryChanged() => fallbackInventoryChangedRaised++;

            try
            {
                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);
                runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
                registry.SetDefinitionsForTests(new[] { definition });

                var injectedEvents = new DefaultRuntimeEvents();
                var injectedEquipRaised = 0;
                var injectedFireRaised = 0;
                var injectedReloadRaised = 0;
                var injectedInventoryChangedRaised = 0;
                injectedEvents.OnWeaponEquipped += _ => injectedEquipRaised++;
                injectedEvents.OnWeaponFired += (_, _, _) => injectedFireRaised++;
                injectedEvents.OnWeaponReloaded += (_, _, _) => injectedReloadRaised++;
                injectedEvents.OnInventoryChanged += () => injectedInventoryChangedRaised++;

                fallbackRuntimeEvents.OnWeaponEquipped += HandleFallbackWeaponEquipped;
                fallbackRuntimeEvents.OnWeaponFired += HandleFallbackWeaponFired;
                fallbackRuntimeEvents.OnWeaponReloaded += HandleFallbackWeaponReloaded;
                fallbackRuntimeEvents.OnInventoryChanged += HandleFallbackInventoryChanged;
                fallbackHandlersRegistered = true;

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                controller.Configure(weaponEvents: injectedEvents, inventoryEvents: injectedEvents);
                yield return null;

                input.FirePressedThisFrame = true;
                yield return null;
                input.ReloadPressedThisFrame = true;
                yield return null;
                yield return new WaitForSeconds(0.36f);

                Assert.That(injectedEquipRaised, Is.GreaterThan(0));
                Assert.That(injectedFireRaised, Is.EqualTo(1));
                Assert.That(injectedReloadRaised, Is.EqualTo(1));
                Assert.That(injectedInventoryChangedRaised, Is.EqualTo(1));

                Assert.That(fallbackEquipRaised, Is.EqualTo(0));
                Assert.That(fallbackFireRaised, Is.EqualTo(0));
                Assert.That(fallbackReloadRaised, Is.EqualTo(0));
                Assert.That(fallbackInventoryChangedRaised, Is.EqualTo(0));
            }
            finally
            {
                if (fallbackHandlersRegistered)
                {
                    fallbackRuntimeEvents.OnWeaponEquipped -= HandleFallbackWeaponEquipped;
                    fallbackRuntimeEvents.OnWeaponFired -= HandleFallbackWeaponFired;
                    fallbackRuntimeEvents.OnWeaponReloaded -= HandleFallbackWeaponReloaded;
                    fallbackRuntimeEvents.OnInventoryChanged -= HandleFallbackInventoryChanged;
                }

                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
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
            }
        }

        [UnityTest]
        public IEnumerator Fire_RealStarterRifleContent_ReachesLongRangeTarget()
        {
#if UNITY_EDITOR
            GameObject root = null;
            GameObject registryGo = null;
            GameObject target = null;

            try
            {
                var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    "Assets/_Project/Weapons/Data/Weapons/StarterRifle.asset");
                Assert.That(definition, Is.Not.Null, "Expected the real StarterRifle weapon definition asset.");

                root = new GameObject("PlayerRoot");
                root.transform.position = new Vector3(0f, 1000f, 0f);
                var input = root.AddComponent<TestInputSource>();
                var resolver = root.AddComponent<TestPickupResolver>();
                var inventoryController = root.AddComponent<PlayerInventoryController>();
                var runtime = new PlayerInventoryRuntime();
                inventoryController.Configure(input, resolver, runtime);

                runtime.BeltSlotItemIds[0] = "weapon-kar98k";
                runtime.SelectBeltSlot(0);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = new Vector3(0f, 1000f, 300f);
                target.transform.localScale = new Vector3(60f, 120f, 1f);
                var receiver = target.AddComponent<TestDamageable>();

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                yield return null;

                var chamberRound = WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj");
                var magazineRounds = new[]
                {
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj"),
                    WeaponAmmoDefaults.BuildFactoryRound("ammo-factory-308-147-fmj")
                };
                Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 4, 0, true), Is.True);
                Assert.That(controller.ApplyRuntimeBallistics("weapon-kar98k", chamberRound, magazineRounds), Is.True);

                Random.InitState(1337);
                input.FirePressedThisFrame = true;
                yield return null;

                var elapsed = 0f;
                while (receiver.HitCount == 0 && elapsed < 2f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.That(
                    receiver.HitCount,
                    Is.EqualTo(1),
                    "The real Kar98k content should keep the projectile alive past the old 220m cutoff instead of despawning mid-flight.");
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

                if (target != null)
                {
                    Object.Destroy(target);
                }
            }
#else
            Assert.Ignore("Requires UnityEditor AssetDatabase.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator Fire_WhenMenuIsOpen_IsBlockedAndDoesNotCarryOverAfterClose()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var firedRaised = 0;
            runtimeEvents.OnWeaponFired += (_, _, _) => firedRaised++;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var cursorController = root.AddComponent<PlayerCursorLockController>();
            cursorController.LockCursor();

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            controller.Configure();
            yield return null;

            runtimeEvents.RaiseTabInventoryVisibilityChanged(true);
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(0), "Weapon fire should be blocked while a menu is open.");

            runtimeEvents.RaiseTabInventoryVisibilityChanged(false);
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(0), "Blocked fire input should be consumed instead of firing after the menu closes.");

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(1), "Fire should resume once the menu has closed.");

            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_WhenGameplayInputIsForcedBlocked_IsBlockedAndDoesNotCarryOverAfterRelease()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var firedRaised = 0;
            runtimeEvents.OnWeaponFired += (_, _, _) => firedRaised++;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var cursorController = root.AddComponent<PlayerCursorLockController>();
            cursorController.LockCursor();

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            controller.Configure();
            yield return null;

            cursorController.SetForcedCursorUnlock(true);
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(0), "Weapon fire should be blocked while dialogue-style forced input blocking is active.");

            cursorController.SetForcedCursorUnlock(false);
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(0), "Blocked fire input should be consumed instead of firing after forced blocking ends.");

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedRaised, Is.EqualTo(1), "Fire should resume once forced input blocking ends.");

            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Aiming_LerpsFieldOfViewToPackAdsFovAndBack()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                "weapon-kar98k",
                "Scoped Rifle",
                5,
                0.1f,
                80f,
                0f,
                20f,
                120f,
                1,
                10,
                true,
                0.7f,
                WeaponScopeConfiguration.Create(true, 4f, 20f, 8f, "ebr-7c", 100, 25));
            registry.SetDefinitionsForTests(new[] { definition });

            var adsCameraGo = new GameObject("AdsCamera");
            var adsCamera = adsCameraGo.AddComponent<Camera>();
            const float baseFieldOfView = 60f;
            adsCamera.fieldOfView = baseFieldOfView;
            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", adsCamera);
            yield return null;

            const float expectedAdsFieldOfView = 45f;
            input.AimHeldValue = true;
            yield return null;
            yield return new WaitForSeconds(0.45f);

            Assert.That(adsCamera.fieldOfView, Is.EqualTo(expectedAdsFieldOfView).Within(0.6f));

            input.AimHeldValue = false;
            yield return null;
            yield return new WaitForSeconds(0.45f);

            Assert.That(adsCamera.fieldOfView, Is.EqualTo(baseFieldOfView).Within(0.6f));

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(adsCameraGo);
        }

        [UnityTest]
        public IEnumerator UnequipWhileAiming_RestoresBaselineFieldOfView()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.BeltSlotItemIds[1] = null;
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                "weapon-kar98k",
                "Scoped Rifle",
                5,
                0.1f,
                80f,
                0f,
                20f,
                120f,
                1,
                10,
                true,
                0.7f,
                WeaponScopeConfiguration.Create(true, 4f, 20f, 8f, "ebr-7c", 100, 25));
            registry.SetDefinitionsForTests(new[] { definition });

            var adsCameraGo = new GameObject("AdsCamera");
            var adsCamera = adsCameraGo.AddComponent<Camera>();
            const float baseFieldOfView = 60f;
            adsCamera.fieldOfView = baseFieldOfView;

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", adsCamera);
            yield return null;

            input.AimHeldValue = true;
            yield return null;
            yield return new WaitForSeconds(0.25f);
            Assert.That(adsCamera.fieldOfView, Is.LessThan(baseFieldOfView - 1f), "Expected ADS to lower FOV before unequip.");

            runtime.SelectBeltSlot(1);
            yield return null;
            yield return new WaitForSeconds(0.15f);

            Assert.That(adsCamera.fieldOfView, Is.EqualTo(baseFieldOfView).Within(0.6f), "Unequipping while ADS should restore baseline FOV.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(adsCameraGo);
        }

        [UnityTest]
        public IEnumerator UnarmedState_PreservesExternalFieldOfViewChanges()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.BeltSlotItemIds[1] = null;
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                "weapon-kar98k",
                "Scoped Rifle",
                5,
                0.1f,
                80f,
                0f,
                20f,
                120f,
                1,
                10,
                true,
                0.7f,
                WeaponScopeConfiguration.Create(true, 4f, 20f, 8f, "ebr-7c", 100, 25));
            registry.SetDefinitionsForTests(new[] { definition });

            var adsCameraGo = new GameObject("AdsCamera");
            var adsCamera = adsCameraGo.AddComponent<Camera>();
            const float baseFieldOfView = 60f;
            adsCamera.fieldOfView = baseFieldOfView;

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_adsCamera", adsCamera);
            yield return null;

            input.AimHeldValue = true;
            yield return null;
            yield return new WaitForSeconds(0.25f);

            runtime.SelectBeltSlot(1);
            yield return null;
            yield return new WaitForSeconds(0.15f);
            Assert.That(adsCamera.fieldOfView, Is.EqualTo(baseFieldOfView).Within(0.6f));

            const float externalFov = 72f;
            adsCamera.fieldOfView = externalFov;
            yield return null;
            yield return null;

            Assert.That(adsCamera.fieldOfView, Is.EqualTo(externalFov).Within(0.2f), "Unarmed flow should not overwrite externally applied FOV.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(adsCameraGo);
        }

        [UnityTest]
        public IEnumerator BeltSelectedWeapon_EquipsAndFiresAndReloads()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);

            string equipped = null;
            string fired = null;
            var firedCount = 0;
            string reloaded = null;
            runtimeEvents.OnWeaponEquipped += OnEquipped;
            runtimeEvents.OnWeaponFired += OnFired;
            runtimeEvents.OnWeaponReloaded += OnReloaded;

            void OnEquipped(string itemId) => equipped = itemId;
            void OnFired(string itemId, Vector3 _, Vector3 __)
            {
                fired = itemId;
                firedCount++;
            }
            void OnReloaded(string itemId, int _, int __) => reloaded = itemId;

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(equipped, Is.EqualTo("weapon-kar98k"));
            Assert.That(fired, Is.EqualTo("weapon-kar98k"));
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterFire), Is.True);
            Assert.That(stateAfterFire.MagazineCount, Is.EqualTo(0));

            input.ReloadPressedThisFrame = true;
            yield return null;
            yield return new WaitForSeconds(0.36f);

            Assert.That(reloaded, Is.EqualTo("weapon-kar98k"));
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterReload), Is.True);
            Assert.That(stateAfterReload.MagazineCount, Is.EqualTo(5));
            Assert.That(stateAfterReload.ReserveCount, Is.EqualTo(runtime.GetItemQuantity("ammo-factory-308-147-fmj")));

            yield return new WaitForSeconds(0.11f);
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedCount, Is.EqualTo(2));
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterReloadFire), Is.True);
            Assert.That(stateAfterReloadFire.ChamberLoaded, Is.True);
            Assert.That(stateAfterReloadFire.MagazineCount, Is.EqualTo(4));

            runtimeEvents.OnWeaponEquipped -= OnEquipped;
            runtimeEvents.OnWeaponFired -= OnFired;
            runtimeEvents.OnWeaponReloaded -= OnReloaded;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MissingLocalInputSource_StillEquips_FromSceneInputProvider()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();

            var inputGo = new GameObject("InputSource");
            var input = inputGo.AddComponent<TestInputSource>();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);

            string equipped = null;
            runtimeEvents.OnWeaponEquipped += OnEquipped;
            void OnEquipped(string itemId) => equipped = itemId;

            yield return null;

            Assert.That(equipped, Is.EqualTo("weapon-kar98k"));

            runtimeEvents.OnWeaponEquipped -= OnEquipped;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(inputGo);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MultipleRegistries_WhenAssignedRegistryMissesSelectedItem_DoesNotRescueFromOtherSceneRegistry()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var staleRegistryGo = new GameObject("StaleRegistry");
            var staleRegistry = staleRegistryGo.AddComponent<WeaponRegistry>();
            var staleDefinition = ScriptableObject.CreateInstance<WeaponDefinition>();
            staleDefinition.SetRuntimeValuesForTests("weapon-other-99", "Other", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            staleRegistry.SetDefinitionsForTests(new[] { staleDefinition });

            var activeRegistryGo = new GameObject("ActiveRegistry");
            var activeRegistry = activeRegistryGo.AddComponent<WeaponRegistry>();
            var activeDefinition = ScriptableObject.CreateInstance<WeaponDefinition>();
            activeDefinition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            activeRegistry.SetDefinitionsForTests(new[] { activeDefinition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", staleRegistry);

            var firedCount = 0;
            runtimeEvents.OnWeaponFired += HandleWeaponFired;
            void HandleWeaponFired(string _, Vector3 __, Vector3 ___) => firedCount++;

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedCount, Is.EqualTo(0));
            Assert.That(controller.EquippedItemId, Is.Empty);
            Assert.That(GetControllerField<WeaponRegistry>(controller, "_weaponRegistry"), Is.SameAs(staleRegistry));

            runtimeEvents.OnWeaponFired -= HandleWeaponFired;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(staleRegistryGo);
            Object.Destroy(activeRegistryGo);
            Object.Destroy(staleDefinition);
            Object.Destroy(activeDefinition);
        }

        [UnityTest]
        public IEnumerator ReloadCompletion_FromRuntimeAppliedLoadout_ConsumesAmmoAndRaisesEvents()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 0, false);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            var inventoryChangedCount = 0;
            var reloadEventCount = 0;
            string reloadedItemId = null;
            runtimeEvents.OnInventoryChanged += OnInventoryChanged;
            runtimeEvents.OnWeaponReloaded += OnWeaponReloaded;
            void OnInventoryChanged() => inventoryChangedCount++;
            void OnWeaponReloaded(string itemId, int _, int __)
            {
                reloadEventCount++;
                reloadedItemId = itemId;
            }

            yield return null;

            Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 0, 0, false), Is.True);
            Assert.That(
                controller.ApplyRuntimeBallistics(
                    "weapon-kar98k",
                    null,
                    new[]
                    {
                        new AmmoBallisticSnapshot(
                            AmmoSourceType.Factory,
                            2780f,
                            55f,
                            147f,
                            0.398f,
                            4.5f,
                            "Factory .308 147gr FMJ",
                            "runtime-pack-round",
                            "ammo-factory-308-147-fmj")
                    }),
                Is.True);

            var quantityBeforeReload = runtime.GetItemQuantity("ammo-factory-308-147-fmj");
            input.ReloadPressedThisFrame = true;
            yield return null;
            yield return new WaitForSeconds(0.36f);

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var stateAfterReload), Is.True);
            Assert.That(stateAfterReload.ChamberLoaded, Is.True);
            Assert.That(stateAfterReload.MagazineCount, Is.GreaterThan(0));
            Assert.That(runtime.GetItemQuantity("ammo-factory-308-147-fmj"), Is.LessThan(quantityBeforeReload));
            Assert.That(inventoryChangedCount, Is.EqualTo(1));
            Assert.That(reloadEventCount, Is.EqualTo(1));
            Assert.That(reloadedItemId, Is.EqualTo("weapon-kar98k"));

            runtimeEvents.OnInventoryChanged -= OnInventoryChanged;
            runtimeEvents.OnWeaponReloaded -= OnWeaponReloaded;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MissingCoreDependencies_LogsErrorOnce_AndSkipsTick()
        {
            var root = new GameObject("PlayerRoot");
            root.AddComponent<PlayerWeaponController>();

            LogAssert.Expect(LogType.Error, "PlayerWeaponController requires PlayerInventoryController and WeaponRegistry references.");
            yield return null;
            yield return null;

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ApplyRuntimePayload_InvalidCountsAndLoadout_AreNormalized()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            inventoryController.Configure(input, resolver, new PlayerInventoryRuntime());

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 0, false);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            Assert.That(controller.ApplyRuntimeState("weapon-kar98k", 999, -5, true), Is.True);
            Assert.That(
                controller.ApplyRuntimeBallistics(
                    "weapon-kar98k",
                    null,
                    new[]
                    {
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c1", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c2", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c3", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c4", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c5", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c6", "ammo-factory-308-147-fmj"),
                        new AmmoBallisticSnapshot(AmmoSourceType.Factory, 2780f, 55f, 147f, 0.398f, 4.5f, "Factory .308 147gr FMJ", "c7", "ammo-factory-308-147-fmj")
                    }),
                Is.True);

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            Assert.That(state.MagazineCount, Is.EqualTo(5));
            Assert.That(state.GetMagazineRoundsSnapshot().Count, Is.EqualTo(5));
            Assert.That(state.ReserveCount, Is.EqualTo(0));
            Assert.That(state.ChamberLoaded, Is.True);
            Assert.That(state.ChamberRound.HasValue, Is.True);

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator MissingProjectilePrefab_StillSpawnsRuntimeProjectile()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            yield return null;

            var before = Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None).Length;
            input.FirePressedThisFrame = true;
            yield return null;

            var after = Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None).Length;
            Assert.That(after, Is.GreaterThan(before));

            foreach (var projectile in Object.FindObjectsByType<WeaponProjectile>(FindObjectsSortMode.None))
            {
                Object.Destroy(projectile.gameObject);
            }

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_UsesAmmoSnapshotVelocityAndProjectileMass_NotWeaponDefaults()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 10f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            var chamberRound = new AmmoBallisticSnapshot(AmmoSourceType.Factory, 3000f, 0f, 168f, 0.46f, 0f);
            state.SetAmmoLoadoutForTests(chamberRound, System.Array.Empty<AmmoBallisticSnapshot>());

            input.FirePressedThisFrame = true;
            yield return null;

            var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.InitialSpeedMetersPerSecond, Is.EqualTo(914.4f).Within(1f));
            Assert.That(projectile.ProjectileMassGrains, Is.EqualTo(168f).Within(0.001f));

            if (projectile != null)
            {
                Object.Destroy(projectile.gameObject);
            }

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_RaisesWeaponFiredWithActualDispersedDirection()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 10f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            var chamberRound = new AmmoBallisticSnapshot(AmmoSourceType.Factory, 3000f, 0f, 168f, 0.46f, 12f);
            state.SetAmmoLoadoutForTests(chamberRound, System.Array.Empty<AmmoBallisticSnapshot>());

            var firedDirection = Vector3.zero;
            var firedDirectionCaptured = false;
            runtimeEvents.OnWeaponFired += HandleWeaponFired;
            void HandleWeaponFired(string _, Vector3 __, Vector3 direction)
            {
                firedDirection = direction;
                firedDirectionCaptured = true;
            }

            Random.InitState(1337);
            input.FirePressedThisFrame = true;
            yield return null;

            var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
            Assert.That(projectile, Is.Not.Null);
            Assert.That(firedDirectionCaptured, Is.True);
            Assert.That(Vector3.Dot(firedDirection.normalized, projectile.transform.forward), Is.GreaterThan(0.9999f));

            runtimeEvents.OnWeaponFired -= HandleWeaponFired;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            if (projectile != null)
            {
                Object.Destroy(projectile.gameObject);
            }

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator ReloadStart_ThenSprint_CancelsReload()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 10, false);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string startedItemId = null;
            string cancelledItemId = null;
            var cancelledReason = WeaponReloadCancelReason.DryStateInvalidated;
            runtimeEvents.OnWeaponReloadStarted += HandleStarted;
            runtimeEvents.OnWeaponReloadCancelled += HandleCancelled;
            void HandleStarted(string itemId) => startedItemId = itemId;
            void HandleCancelled(string itemId, WeaponReloadCancelReason reason)
            {
                cancelledItemId = itemId;
                cancelledReason = reason;
            }

            yield return null;

            input.ReloadPressedThisFrame = true;
            yield return null;

            input.SprintHeldValue = true;
            yield return null;

            Assert.That(startedItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(cancelledItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(cancelledReason, Is.EqualTo(WeaponReloadCancelReason.Sprint));

            runtimeEvents.OnWeaponReloadStarted -= HandleStarted;
            runtimeEvents.OnWeaponReloadCancelled -= HandleCancelled;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator AimingStateChange_RaisesWeaponAimChangedEvent()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string eventItemId = null;
            var isAiming = false;
            var raised = false;
            runtimeEvents.OnWeaponAimChanged += HandleAimChanged;
            void HandleAimChanged(string itemId, bool value)
            {
                eventItemId = itemId;
                isAiming = value;
                raised = true;
            }

            yield return null;

            input.AimHeldValue = true;
            yield return null;

            Assert.That(raised, Is.True);
            Assert.That(eventItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(isAiming, Is.True);

            runtimeEvents.OnWeaponAimChanged -= HandleAimChanged;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator ReloadingThenUnequip_CancelsReloadWithUnequipReason()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("ammo-factory-308-147-fmj", 10, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 0, 10, false);
            registry.SetDefinitionsForTests(new[] { definition });

            root.AddComponent<PlayerWeaponController>();

            string cancelledItemId = null;
            var cancelledReason = WeaponReloadCancelReason.DryStateInvalidated;
            runtimeEvents.OnWeaponReloadCancelled += HandleCancelled;
            void HandleCancelled(string itemId, WeaponReloadCancelReason reason)
            {
                cancelledItemId = itemId;
                cancelledReason = reason;
            }

            yield return null;

            input.ReloadPressedThisFrame = true;
            yield return null;

            runtime.SelectBeltSlot(1);
            yield return null;

            Assert.That(cancelledItemId, Is.EqualTo("weapon-kar98k"));
            Assert.That(cancelledReason, Is.EqualTo(WeaponReloadCancelReason.Unequip));

            runtimeEvents.OnWeaponReloadCancelled -= HandleCancelled;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Equip_WithoutExplicitViewBindings_DoesNotSpawnWeaponViewFallback()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var viewPrefab = new GameObject("FallbackWeaponViewPrefab");
            GameObject registryGo = null;
            WeaponDefinition definition = null;

            try
            {
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
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_weaponViewParent", root.transform);
                yield return null;

                var equippedView = root.transform.Find("EquippedView_weapon-kar98k");
                Assert.That(equippedView, Is.Null, "Weapon view spawn should fail loudly when explicit bindings are missing.");
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator SameItemSelection_WhenViewSpawnFails_DoesNotRestartEquipCycle()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var equipStartedCount = 0;
            var unequipStartedCount = 0;
            runtimeEvents.OnWeaponEquipStarted += HandleEquipStarted;
            runtimeEvents.OnWeaponUnequipStarted += HandleUnequipStarted;
            void HandleEquipStarted(string _) => equipStartedCount++;
            void HandleUnequipStarted(string _) => unequipStartedCount++;

            root.AddComponent<PlayerWeaponController>();
            yield return null;
            yield return new WaitForSeconds(0.2f);

            Assert.That(equipStartedCount, Is.EqualTo(1), "Same-item selection with missing view should not restart equip flow.");
            Assert.That(unequipStartedCount, Is.EqualTo(0), "Same-item selection with missing view should not trigger holster churn.");

            runtimeEvents.OnWeaponEquipStarted -= HandleEquipStarted;
            runtimeEvents.OnWeaponUnequipStarted -= HandleUnequipStarted;
            RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator SameItemSelection_AfterDisableEnable_RespawnsMissingViewWithoutRestartingEquipCycle()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var runtimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = runtimeEvents;

            var root = new GameObject("PlayerRoot");
            var viewPrefab = new GameObject("ReenableWeaponViewPrefab");
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            var equipStartedCount = 0;
            var unequipStartedCount = 0;
            var handlersRegistered = false;
            void HandleEquipStarted(string _) => equipStartedCount++;
            void HandleUnequipStarted(string _) => unequipStartedCount++;

            try
            {
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
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                runtimeEvents.OnWeaponEquipStarted += HandleEquipStarted;
                runtimeEvents.OnWeaponUnequipStarted += HandleUnequipStarted;
                handlersRegistered = true;

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_weaponViewParent", root.transform);
                SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);
                yield return null;

                var viewName = "EquippedView_weapon-kar98k";
                Assert.That(root.transform.Find(viewName), Is.Not.Null, "Initial equip should spawn the bound weapon view.");
                Assert.That(equipStartedCount, Is.EqualTo(1));
                Assert.That(unequipStartedCount, Is.EqualTo(0));

                controller.enabled = false;
                yield return null;
                Assert.That(root.transform.Find(viewName), Is.Null, "Disabling should destroy the equipped view instance.");

                controller.enabled = true;
                yield return null;

                Assert.That(root.transform.Find(viewName), Is.Not.Null, "Re-enabling with the same selected item should respawn a missing equipped view.");
                Assert.That(equipStartedCount, Is.EqualTo(1), "View respawn should not restart equip flow for same item.");
                Assert.That(unequipStartedCount, Is.EqualTo(0), "View respawn should not trigger unequip flow.");

            }
            finally
            {
                if (handlersRegistered)
                {
                    runtimeEvents.OnWeaponEquipStarted -= HandleEquipStarted;
                    runtimeEvents.OnWeaponUnequipStarted -= HandleUnequipStarted;
                }

                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;

                if (root != null)
                {
                    Object.Destroy(root);
                }

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (registryGo != null)
                {
                    Object.Destroy(registryGo);
                }

                if (definition != null)
                {
                    Object.Destroy(definition);
                }
            }
        }

        [UnityTest]
        public IEnumerator Equip_WithConfiguredWeaponViewParent_UsesConfiguredParentBeforeFallback()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.SetParent(root.transform, false);
            var playerArms = new GameObject("PlayerArms").transform;
            playerArms.SetParent(cameraPivot, false);
            var playerArmsVisual = new GameObject("PlayerArmsVisual").transform;
            playerArmsVisual.SetParent(playerArms, false);
            playerArmsVisual.gameObject.AddComponent<Animator>();
            var fallbackParent = new GameObject("ik_hand_gun").transform;
            fallbackParent.SetParent(playerArmsVisual, false);

            var configuredParent = new GameObject("ConfiguredWeaponViewParent").transform;
            configuredParent.SetParent(root.transform, false);

            var viewPrefab = new GameObject("ConfiguredParentViewPrefab");
            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(iconPrefabField, Is.Not.Null);
            iconPrefabField.SetValue(definition, viewPrefab);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_weaponViewParent", configuredParent);
            SetControllerWeaponViewBinding(controller, "weapon-kar98k", viewPrefab);
            yield return null;

            Assert.That(configuredParent.Find("EquippedView_weapon-kar98k"), Is.Not.Null);
            Assert.That(fallbackParent.Find("EquippedView_weapon-kar98k"), Is.Null);

            Object.Destroy(root);
            Object.Destroy(viewPrefab);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator BeltSelectionChange_DuringHolsterDelay_UpdatesPendingEquipToLatestSelection()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.BeltSlotItemIds[1] = "weapon-rifle-02";
            runtime.BeltSlotItemIds[2] = "weapon-rifle-03";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definitionA = ScriptableObject.CreateInstance<WeaponDefinition>();
            definitionA.SetRuntimeValuesForTests("weapon-kar98k", "Rifle 01", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            var definitionB = ScriptableObject.CreateInstance<WeaponDefinition>();
            definitionB.SetRuntimeValuesForTests("weapon-rifle-02", "Rifle 02", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            var definitionC = ScriptableObject.CreateInstance<WeaponDefinition>();
            definitionC.SetRuntimeValuesForTests("weapon-rifle-03", "Rifle 03", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
            registry.SetDefinitionsForTests(new[] { definitionA, definitionB, definitionC });

            var controller = root.AddComponent<PlayerWeaponController>();
            yield return null;

            runtime.SelectBeltSlot(1);
            yield return null;
            Assert.That(GetControllerField<string>(controller, "_pendingEquipItemId"), Is.EqualTo("weapon-rifle-02"));

            runtime.SelectBeltSlot(2);
            yield return null;
            Assert.That(GetControllerField<string>(controller, "_pendingEquipItemId"), Is.EqualTo("weapon-rifle-03"));

            yield return new WaitForSeconds(0.12f);
            Assert.That(controller.EquippedItemId, Is.EqualTo("weapon-rifle-03"));

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definitionA);
            Object.Destroy(definitionB);
            Object.Destroy(definitionC);
        }

        [UnityTest]
        public IEnumerator Fire_WithoutSerializedCombatEmitter_AutoResolvesRuntimeEmitter()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_combatAudioEmitter", null);

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            var emitterField = typeof(PlayerWeaponController).GetField("_combatAudioEmitter", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(emitterField, Is.Not.Null);
            var resolvedEmitter = emitterField.GetValue(controller) as WeaponCombatAudioEmitter;
            Assert.That(resolvedEmitter, Is.Not.Null);
            Assert.That(root.GetComponentInChildren<WeaponCombatAudioEmitter>(true), Is.SameAs(resolvedEmitter));
            var catalogField = typeof(WeaponCombatAudioEmitter).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(catalogField, Is.Not.Null);
            Assert.That(catalogField.GetValue(resolvedEmitter), Is.Not.Null, "Auto-resolved emitter should bind a default combat audio catalog.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator Fire_DiscoveredEmitterWithCustomCatalog_PreservesConfiguredCatalog()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            registry.SetDefinitionsForTests(new[] { definition });

            var customCatalog = ScriptableObject.CreateInstance<CombatAudioCatalog>();
            var emitterHost = new GameObject("EmitterHost");
            emitterHost.transform.SetParent(root.transform, false);
            var emitter = emitterHost.AddComponent<WeaponCombatAudioEmitter>();
            emitter.SetCatalog(customCatalog);

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_combatAudioEmitter", null);

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            var emitterField = typeof(PlayerWeaponController).GetField("_combatAudioEmitter", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(emitterField, Is.Not.Null);
            var resolvedEmitter = emitterField.GetValue(controller) as WeaponCombatAudioEmitter;
            Assert.That(resolvedEmitter, Is.SameAs(emitter));

            var catalogField = typeof(WeaponCombatAudioEmitter).GetField("_catalog", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(catalogField, Is.Not.Null);
            Assert.That(catalogField.GetValue(resolvedEmitter), Is.SameAs(customCatalog));

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
            Object.Destroy(customCatalog);
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

        private static void SetField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(instance, value);
        }

        private static object GetField(Type type, object instance, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return field.GetValue(instance);
        }

        private static void ConfigureTestWeaponViewMounts(
            GameObject viewPrefab,
            Transform adsPivot = null,
            Transform muzzleFirePoint = null,
            Transform ironSightAnchor = null,
            Transform magazineSocket = null,
            Transform magazineDropSocket = null,
            Transform scopeSlot = null,
            Transform muzzleSlot = null)
        {
            Assert.That(viewPrefab, Is.Not.Null);

            var mountsType = ResolveType("Reloader.Weapons.Runtime.WeaponViewAttachmentMounts");
            Assert.That(mountsType, Is.Not.Null, "WeaponViewAttachmentMounts should exist so runtime views declare slots explicitly.");

            var mounts = viewPrefab.GetComponent(mountsType) ?? viewPrefab.AddComponent(mountsType);
            SetField(mountsType, mounts, "_adsPivot", adsPivot != null ? adsPivot : viewPrefab.transform);
            SetField(mountsType, mounts, "_muzzleTransform", muzzleFirePoint);
            SetField(mountsType, mounts, "_ironSightAnchor", ironSightAnchor);
            SetField(mountsType, mounts, "_magazineSocket", magazineSocket);
            SetField(mountsType, mounts, "_magazineDropSocket", magazineDropSocket);

            var slotEntryType = mountsType.GetNestedType("AttachmentSlotMount", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(slotEntryType, Is.Not.Null);

            var entries = Array.CreateInstance(slotEntryType, scopeSlot != null && muzzleSlot != null ? 2 : scopeSlot != null || muzzleSlot != null ? 1 : 0);
            var index = 0;
            if (scopeSlot != null)
            {
                var entry = Activator.CreateInstance(slotEntryType);
                SetField(slotEntryType, entry, "_slotType", WeaponAttachmentSlotType.Scope);
                SetField(slotEntryType, entry, "_slotTransform", scopeSlot);
                entries.SetValue(entry, index++);
            }

            if (muzzleSlot != null)
            {
                var entry = Activator.CreateInstance(slotEntryType);
                SetField(slotEntryType, entry, "_slotType", WeaponAttachmentSlotType.Muzzle);
                SetField(slotEntryType, entry, "_slotTransform", muzzleSlot);
                entries.SetValue(entry, index);
            }

            SetField(mountsType, mounts, "_attachmentSlots", entries);
        }

        private static MonoBehaviour FindFirstCinemachineCamera()
        {
            var allCameras = FindAllCinemachineCameras();
            return allCameras.Count > 0 ? allCameras[0] : null;
        }

        private static Camera FindShotRenderCamera()
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera != null && camera.name == "ShotCameraRuntime_Camera")
                {
                    return camera;
                }
            }

            return null;
        }

        private static UIDocument CreateRuntimeHudDocument(string screenId, Transform parent)
        {
            var screenGo = new GameObject(screenId);
            screenGo.transform.SetParent(parent, false);
            return screenGo.AddComponent<UIDocument>();
        }

        private static bool IsDocumentVisible(UIDocument document)
        {
            if (document == null || !document.enabled)
            {
                return false;
            }

            var root = document.rootVisualElement;
            return root != null && root.visible;
        }

        private static List<MonoBehaviour> FindAllCinemachineCameras()
        {
            var matches = new List<MonoBehaviour>();
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour != null && behaviour.GetType().FullName == "Unity.Cinemachine.CinemachineCamera")
                {
                    matches.Add(behaviour);
                }
            }

            return matches;
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource, IShotCameraInputSource
        {
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public bool ShotCameraCancelPressedThisFrame;
            public bool SprintHeldValue;
            public bool AimHeldValue;
            public bool ShotCameraSpeedUpHeldValue;
            public Vector2 LookInputValue;
            public float ZoomQueued;
            public int ZeroAdjustQueued;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => LookInputValue;
            public bool SprintHeld => SprintHeldValue;
            public bool AimHeld => AimHeldValue;
            public bool ShotCameraSpeedUpHeld => ShotCameraSpeedUpHeldValue;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumePickupPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeShotCameraCancelPressed()
            {
                if (!ShotCameraCancelPressedThisFrame)
                {
                    return false;
                }

                ShotCameraCancelPressedThisFrame = false;
                return true;
            }

            public float ConsumeZoomInput()
            {
                if (Mathf.Approximately(ZoomQueued, 0f))
                {
                    return 0f;
                }

                var queued = ZoomQueued;
                ZoomQueued = 0f;
                return queued;
            }

            public int ConsumeZeroAdjustStep()
            {
                if (ZeroAdjustQueued == 0)
                {
                    return 0;
                }

                var queued = ZeroAdjustQueued;
                ZeroAdjustQueued = 0;
                return queued;
            }

            public bool ConsumeFirePressed()
            {
                if (!FirePressedThisFrame)
                {
                    return false;
                }

                FirePressedThisFrame = false;
                return true;
            }

            public bool ConsumeReloadPressed()
            {
                if (!ReloadPressedThisFrame)
                {
                    return false;
                }

                ReloadPressedThisFrame = false;
                return true;
            }
        }

        private sealed class MinimalBridgeMarker : MonoBehaviour
        {
        }

        private sealed class StubAdsStateRuntimeBridge : MonoBehaviour
        {
            public float CurrentMagnification { get; private set; } = 1f;
            public float LastSetMagnification { get; private set; } = float.NaN;

            public void SetAdsHeld(bool held)
            {
            }

            public void SetMagnification(float magnification)
            {
                LastSetMagnification = magnification;
                CurrentMagnification = magnification;
            }

            public bool ApplyScopeAdjustmentInput(int windageClicks, int elevationClicks)
            {
                return false;
            }
        }

        private sealed class Vector3EqualityComparer : System.Collections.IEqualityComparer
        {
            public static readonly Vector3EqualityComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                if (x is not Vector3 lhs || y is not Vector3 rhs)
                {
                    return false;
                }

                return Vector3.Distance(lhs, rhs) <= 0.0001f;
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }

        private sealed class QuaternionEqualityComparer : System.Collections.IEqualityComparer
        {
            public static readonly QuaternionEqualityComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                if (x is not Quaternion lhs || y is not Quaternion rhs)
                {
                    return false;
                }

                return Quaternion.Angle(lhs, rhs) <= 0.1f;
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public int HitCount { get; private set; }
            public float LastDamage { get; private set; }

            public void ApplyDamage(ProjectileImpactPayload payload)
            {
                HitCount++;
                LastDamage = payload.Damage;
            }
        }

        private sealed class TestPickupResolver : MonoBehaviour, IInventoryPickupTargetResolver
        {
            public bool TryResolvePickupTarget(out IInventoryPickupTarget target)
            {
                target = null;
                return false;
            }
        }

        private sealed class ShotCameraRegistrationSpy : MonoBehaviour
        {
            public int RequestCount { get; private set; }
            public WeaponProjectile LastProjectile { get; private set; }

            public void RegisterShotCameraRequest(WeaponProjectile projectile)
            {
                RequestCount++;
                LastProjectile = projectile;
            }
        }

        private static float MagnificationToFieldOfView(float referenceFieldOfView, float magnification)
        {
            var safeReferenceFov = Mathf.Clamp(referenceFieldOfView, 1f, 179f);
            var safeMagnification = Mathf.Max(1f, magnification);
            var referenceHalfAngle = safeReferenceFov * 0.5f * Mathf.Deg2Rad;
            var zoomedHalfAngle = Mathf.Atan(Mathf.Tan(referenceHalfAngle) / safeMagnification);
            return Mathf.Clamp(zoomedHalfAngle * 2f * Mathf.Rad2Deg, 5f, safeReferenceFov);
        }

        private static void SetControllerField(PlayerWeaponController controller, string fieldName, object value)
        {
            var field = typeof(PlayerWeaponController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(controller, value);
        }

        private static void SetPrivateField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on type '{type.FullName}'.");
            field!.SetValue(instance, value);
        }

        private static T GetControllerField<T>(PlayerWeaponController controller, string fieldName)
        {
            var field = typeof(PlayerWeaponController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return (T)field.GetValue(controller);
        }

        private static void SetControllerWeaponViewBinding(PlayerWeaponController controller, string itemId, GameObject viewPrefab)
        {
            Assert.That(controller, Is.Not.Null);
            Assert.That(viewPrefab, Is.Not.Null);

            var weaponViewParentField = typeof(PlayerWeaponController).GetField("_weaponViewParent", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(weaponViewParentField, Is.Not.Null, "Field '_weaponViewParent' was not found.");
            if (weaponViewParentField.GetValue(controller) == null)
            {
                weaponViewParentField.SetValue(controller, controller.transform);
            }

            var bindingType = typeof(WeaponViewPrefabBinding);
            var binding = Activator.CreateInstance(bindingType);
            SetField(bindingType, binding, "_itemId", itemId);
            SetField(bindingType, binding, "_viewPrefab", viewPrefab);

            var bindings = new[] { (WeaponViewPrefabBinding)binding };
            SetControllerField(controller, "_weaponViewPrefabs", bindings);
            Invoke(controller, "UpdateEquipFromSelection");
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

        private sealed class ClipCaptureWeaponCombatAudioEmitter : WeaponCombatAudioEmitter
        {
            public AudioClip LastFireOverrideClip { get; private set; }

            public override void EmitWeaponFire(string weaponId, Vector3 muzzlePosition, AudioClip overrideClip = null)
            {
                LastFireOverrideClip = overrideClip;
            }
        }
    }
}
