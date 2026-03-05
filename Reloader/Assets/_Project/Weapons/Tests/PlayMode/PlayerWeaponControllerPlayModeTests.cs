using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Audio;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class PlayerWeaponControllerPlayModeTests
    {
        private IGameEventsRuntimeHub _runtimeEventsBeforeEachTest;

        [SetUp]
        public void SetUp()
        {
            _runtimeEventsBeforeEachTest = RuntimeKernelBootstrapper.Events;
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeKernelBootstrapper.Events = _runtimeEventsBeforeEachTest;
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
        public IEnumerator Configure_InjectedWeaponEvents_PropagatesInjectedChannelToSpawnedProjectile()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var fallbackRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = fallbackRuntimeEvents;

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;

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
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 10, true);
                registry.SetDefinitionsForTests(new[] { definition });

                var injectedEvents = new DefaultRuntimeEvents();

                root.AddComponent<PlayerWeaponController>().Configure(weaponEvents: injectedEvents, inventoryEvents: injectedEvents);

                yield return null;

                input.FirePressedThisFrame = true;
                yield return null;

                var projectile = Object.FindFirstObjectByType<WeaponProjectile>();
                Assert.That(projectile, Is.Not.Null);

                var useRuntimeField = typeof(WeaponProjectile).GetField("_useRuntimeKernelWeaponEvents", BindingFlags.Instance | BindingFlags.NonPublic);
                var projectileEventsField = typeof(WeaponProjectile).GetField("_weaponEvents", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(useRuntimeField, Is.Not.Null);
                Assert.That(projectileEventsField, Is.Not.Null);

                var useRuntimeKernelWeaponEvents = (bool)useRuntimeField.GetValue(projectile);
                var projectileEvents = projectileEventsField.GetValue(projectile);
                Assert.That(useRuntimeKernelWeaponEvents, Is.False);
                Assert.That(projectileEvents, Is.SameAs(injectedEvents));
            }
            finally
            {
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
        public IEnumerator Configure_InjectedWeaponEvents_ProjectileHitRaisesOnlyInjectedChannel()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var fallbackRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = fallbackRuntimeEvents;

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject target = null;
            WeaponProjectile projectile = null;
            var fallbackHandlersRegistered = false;

            var fallbackProjectileHitRaised = 0;
            void HandleFallbackProjectileHit(string _, Vector3 __, float ___) => fallbackProjectileHitRaised++;

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
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 120f, 0f, 20f, 10000f, 1, 0, true);
                registry.SetDefinitionsForTests(new[] { definition });

                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.position = new Vector3(0f, 0f, 2f);
                target.transform.localScale = new Vector3(8f, 8f, 0.5f);

                var injectedEvents = new DefaultRuntimeEvents();
                var injectedProjectileHitRaised = 0;
                injectedEvents.OnProjectileHit += (_, _, _) => injectedProjectileHitRaised++;

                fallbackRuntimeEvents.OnProjectileHit += HandleFallbackProjectileHit;
                fallbackHandlersRegistered = true;

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                controller.Configure(weaponEvents: injectedEvents, inventoryEvents: injectedEvents);
                yield return null;

                var injectedWeaponFiredRaised = 0;
                injectedEvents.OnWeaponFired += (_, _, _) => injectedWeaponFiredRaised++;

                Random.InitState(1337);
                input.FirePressedThisFrame = true;
                yield return null;

                var elapsed = 0f;
                while (injectedProjectileHitRaised == 0 && elapsed < 2f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                projectile = Object.FindFirstObjectByType<WeaponProjectile>();

                Assert.That(injectedWeaponFiredRaised, Is.EqualTo(1), "Injected weapon fire event was not raised.");
                Assert.That(injectedProjectileHitRaised, Is.EqualTo(1), "Injected projectile hit event was not raised.");
                Assert.That(fallbackProjectileHitRaised, Is.EqualTo(0), "Fallback runtime projectile hit channel should stay silent.");
            }
            finally
            {
                if (fallbackHandlersRegistered)
                {
                    fallbackRuntimeEvents.OnProjectileHit -= HandleFallbackProjectileHit;
                }

                RuntimeKernelBootstrapper.Events = runtimeEventsBefore;
                if (projectile != null)
                {
                    Object.Destroy(projectile.gameObject);
                }

                if (target != null)
                {
                    Object.Destroy(target);
                }

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
        public IEnumerator Configure_WithoutInjectedPorts_UsesCurrentRuntimeHubAfterSwap()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            var initialRuntimeEvents = new DefaultRuntimeEvents();
            var replacementRuntimeEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialRuntimeEvents;

            var initialFiredRaised = 0;
            var replacementFiredRaised = 0;
            initialRuntimeEvents.OnWeaponFired += (_, _, _) => initialFiredRaised++;
            replacementRuntimeEvents.OnWeaponFired += (_, _, _) => replacementFiredRaised++;

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
            controller.Configure();
            yield return null;

            RuntimeKernelBootstrapper.Events = replacementRuntimeEvents;
            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(initialFiredRaised, Is.EqualTo(0));
            Assert.That(replacementFiredRaised, Is.EqualTo(1));

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

            root.AddComponent<PlayerWeaponController>();

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
        public IEnumerator MultipleRegistries_WhenInitialRegistryMissesSelectedItem_ResolvesFallbackRegistryAndFires()
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

            var firedCount = 0;
            runtimeEvents.OnWeaponFired += HandleWeaponFired;
            void HandleWeaponFired(string _, Vector3 __, Vector3 ___) => firedCount++;

            yield return null;

            input.FirePressedThisFrame = true;
            yield return null;

            Assert.That(firedCount, Is.EqualTo(1));
            Assert.That(controller.EquippedItemId, Is.EqualTo("weapon-kar98k"));

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
        public IEnumerator Fire_UsesAmmoSnapshotVelocityFps_NotWeaponProjectileSpeed()
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
        public IEnumerator Equip_WithoutExplicitViewBindings_UsesWeaponDefinitionIconSourcePrefab()
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
                Assert.That(equippedView, Is.Not.Null, "Expected equipped view to spawn from WeaponDefinition icon prefab when explicit view bindings are missing.");
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
                yield return null;

                var viewName = "EquippedView_weapon-kar98k";
                Assert.That(root.transform.Find(viewName), Is.Not.Null);
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

        [UnityTest]
        public IEnumerator Fire_WithViewMuzzleDefaultAttachment_BridgesAndUsesOverrideClip()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            RuntimeKernelBootstrapper.Events = new DefaultRuntimeEvents();

            var runtimeType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentRuntime");
            var definitionType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
            Assert.That(runtimeType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object muzzleDefinition = null;
            AudioClip overrideClip = null;
            GameObject muzzlePrefab = null;

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

                var emitterSpy = root.AddComponent<ClipCaptureWeaponCombatAudioEmitter>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);

                viewPrefab = new GameObject("ViewPrefabWithMuzzleRuntime");
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(viewPrefab.transform, false);
                var slot = new GameObject("MuzzleAttachmentSlot").transform;
                slot.SetParent(viewPrefab.transform, false);
                var runtimeComponent = viewPrefab.AddComponent(runtimeType);
                var muzzleSocketField = runtimeType.GetField("_muzzleSocket", BindingFlags.Instance | BindingFlags.NonPublic);
                var attachmentSlotField = runtimeType.GetField("_attachmentSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                var defaultAttachmentField = runtimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(muzzleSocketField, Is.Not.Null);
                Assert.That(attachmentSlotField, Is.Not.Null);
                Assert.That(defaultAttachmentField, Is.Not.Null);

                muzzleDefinition = ScriptableObject.CreateInstance(definitionType);
                overrideClip = AudioClip.Create("muzzle-override", 128, 1, 44100, false);
                muzzlePrefab = new GameObject("MuzzleDevicePrefab");

                var definitionMuzzlePrefabField = definitionType.GetField("_muzzlePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                var definitionClipField = definitionType.GetField("_fireClipOverride", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(definitionMuzzlePrefabField, Is.Not.Null);
                Assert.That(definitionClipField, Is.Not.Null);
                definitionMuzzlePrefabField.SetValue(muzzleDefinition, muzzlePrefab);
                definitionClipField.SetValue(muzzleDefinition, overrideClip);

                muzzleSocketField.SetValue(runtimeComponent, muzzle);
                attachmentSlotField.SetValue(runtimeComponent, slot);
                defaultAttachmentField.SetValue(runtimeComponent, muzzleDefinition);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);

                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_combatAudioEmitter", emitterSpy);
                yield return null;

                input.FirePressedThisFrame = true;
                yield return null;

                Assert.That(emitterSpy.LastFireOverrideClip, Is.SameAs(overrideClip));
            }
            finally
            {
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

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (muzzleDefinition != null)
                {
                    Object.Destroy(muzzleDefinition);
                }

                if (overrideClip != null)
                {
                    Object.Destroy(overrideClip);
                }

                if (muzzlePrefab != null)
                {
                    Object.Destroy(muzzlePrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator EquipView_WithDetachableMagazineRuntimeDefaultAttachment_BridgesAndActivatesAttachment()
        {
            var runtimeType = ResolveType("Reloader.Game.Weapons.DetachableMagazineRuntime");
            var definitionType = ResolveType("Reloader.Game.Weapons.MagazineAttachmentDefinition");
            Assert.That(runtimeType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object magazineDefinition = null;
            GameObject magazineVisualPrefab = null;

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
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);

                viewPrefab = new GameObject("ViewPrefabWithMagRuntime");
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(viewPrefab.transform, false);
                var magazineSocket = new GameObject("MagazineSocket").transform;
                magazineSocket.SetParent(viewPrefab.transform, false);
                var dropSocket = new GameObject("MagazineDropSocket").transform;
                dropSocket.SetParent(viewPrefab.transform, false);

                var runtimeComponent = viewPrefab.AddComponent(runtimeType);
                var magazineSocketField = runtimeType.GetField("_magazineSocket", BindingFlags.Instance | BindingFlags.NonPublic);
                var dropSocketField = runtimeType.GetField("_magazineDropSocket", BindingFlags.Instance | BindingFlags.NonPublic);
                var defaultAttachmentField = runtimeType.GetField("_defaultAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(magazineSocketField, Is.Not.Null);
                Assert.That(dropSocketField, Is.Not.Null);
                Assert.That(defaultAttachmentField, Is.Not.Null);

                magazineDefinition = ScriptableObject.CreateInstance(definitionType);
                magazineVisualPrefab = new GameObject("MagazineVisualPrefab");
                var definitionVisualField = definitionType.GetField("_magazineVisualPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                var spawnDroppedField = definitionType.GetField("_spawnDroppedMagazine", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(definitionVisualField, Is.Not.Null);
                Assert.That(spawnDroppedField, Is.Not.Null);
                definitionVisualField.SetValue(magazineDefinition, magazineVisualPrefab);
                spawnDroppedField.SetValue(magazineDefinition, false);

                magazineSocketField.SetValue(runtimeComponent, magazineSocket);
                dropSocketField.SetValue(runtimeComponent, dropSocket);
                defaultAttachmentField.SetValue(runtimeComponent, magazineDefinition);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);

                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                yield return null;

                var equippedViewField = typeof(PlayerWeaponController).GetField("_equippedWeaponView", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(equippedViewField, Is.Not.Null);
                var equippedView = equippedViewField.GetValue(controller) as GameObject;
                Assert.That(equippedView, Is.Not.Null);

                var bridgedRuntime = equippedView.GetComponent(runtimeType);
                Assert.That(bridgedRuntime, Is.Not.Null);

                var activeAttachmentField = runtimeType.GetField("_activeAttachment", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(activeAttachmentField, Is.Not.Null);
                Assert.That(activeAttachmentField.GetValue(bridgedRuntime), Is.SameAs(magazineDefinition));

                var bridgedMagSocketField = runtimeType.GetField("_magazineSocket", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bridgedMagSocketField, Is.Not.Null);
                var bridgedMagazineSocket = bridgedMagSocketField.GetValue(bridgedRuntime) as Transform;
                Assert.That(bridgedMagazineSocket, Is.Not.Null);
                Assert.That(bridgedMagazineSocket.childCount, Is.EqualTo(1), "Bridged magazine runtime should mount configured visual.");
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

                if (magazineDefinition != null)
                {
                    Object.Destroy(magazineDefinition);
                }

                if (magazineVisualPrefab != null)
                {
                    Object.Destroy(magazineVisualPrefab);
                }
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_RejectsUnknownIncompatibleAndUnownedAttachment()
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
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" }),
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Muzzle, new[] { "att-muzzle-brake" })
            });
            registry.SetDefinitionsForTests(new[] { definition });

            runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);
            runtime.TryAddStackItem("att-muzzle-brake", 1, out _, out _, out _);

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_attachmentItemMetadata", new[]
            {
                WeaponAttachmentItemMetadata.CreateForTests("att-optic-4x", WeaponAttachmentSlotType.Scope),
                WeaponAttachmentItemMetadata.CreateForTests("att-muzzle-brake", WeaponAttachmentSlotType.Muzzle)
            });

            yield return null;

            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-unknown"), Is.False, "Unknown attachment should be rejected.");
            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-muzzle-brake"), Is.False, "Incompatible attachment should be rejected.");

            Assert.That(runtime.TryRemoveStackItem("att-optic-4x", 1), Is.True, "Test setup should remove the scope so swap is unowned.");
            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.False, "Unowned attachment should be rejected.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_WithOwnedCompatibleAttachment_PerformsAtomicSwapAndUpdatesRuntimeState()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x", "att-optic-8x" })
            });
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_attachmentItemMetadata", new[]
            {
                WeaponAttachmentItemMetadata.CreateForTests("att-optic-4x", WeaponAttachmentSlotType.Scope),
                WeaponAttachmentItemMetadata.CreateForTests("att-optic-8x", WeaponAttachmentSlotType.Scope)
            });

            yield return null;

            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-8x");

            Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(1));
            Assert.That(runtime.GetItemQuantity("att-optic-8x"), Is.EqualTo(0));

            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.True);
            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo("att-optic-4x"));
            Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(0), "New attachment should be consumed from inventory.");
            Assert.That(runtime.GetItemQuantity("att-optic-8x"), Is.EqualTo(1), "Previous attachment should be returned to inventory.");

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_AllowsCompatibilityBasedSwap_WhenMetadataLookupIsEmpty()
        {
            var root = new GameObject("PlayerRoot");
            var input = root.AddComponent<TestInputSource>();
            var resolver = root.AddComponent<TestPickupResolver>();
            var inventoryController = root.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            inventoryController.Configure(input, resolver, runtime);

            runtime.BeltSlotItemIds[0] = "weapon-kar98k";
            runtime.SelectBeltSlot(0);
            runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);

            var registryGo = new GameObject("Registry");
            var registry = registryGo.AddComponent<WeaponRegistry>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.1f, 80f, 0f, 20f, 120f, 1, 0, true);
            definition.SetAttachmentCompatibilitiesForTests(new[]
            {
                WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
            });
            registry.SetDefinitionsForTests(new[] { definition });

            var controller = root.AddComponent<PlayerWeaponController>();
            SetControllerField(controller, "_weaponRegistry", registry);
            SetControllerField(controller, "_attachmentItemMetadata", Array.Empty<WeaponAttachmentItemMetadata>());

            yield return null;

            Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.True);
            Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
            Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo("att-optic-4x"));

            Object.Destroy(root);
            Object.Destroy(registryGo);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_ScopeHotSwap_UpdatesAttachmentManagerAndAdsMask()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var scopeMaskControllerType = ResolveType("Reloader.Game.Weapons.ScopeMaskController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsControllerType, Is.Not.Null);
            Assert.That(scopeMaskControllerType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object highOpticDefinition = null;
            UnityEngine.Object lowOpticDefinition = null;
            GameObject highOpticPrefab = null;
            GameObject lowOpticPrefab = null;
            GameObject worldCameraGo = null;
            GameObject viewmodelCameraGo = null;
            GameObject scopeMaskGo = null;

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
                runtime.TryAddStackItem("att-optic-8x", 1, out _, out _, out _);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x", "att-optic-8x" })
                });

                viewPrefab = new GameObject("ViewPrefabWithAttachmentManager");
                viewPrefab.layer = 23;
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(viewPrefab.transform, false);
                scopeSlot.gameObject.layer = 23;
                var ironSightAnchor = new GameObject("IronSightAnchor").transform;
                ironSightAnchor.SetParent(viewPrefab.transform, false);
                ironSightAnchor.gameObject.layer = 23;

                var manager = viewPrefab.AddComponent(attachmentManagerType);
                var scopeSlotField = attachmentManagerType.GetField("_scopeSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                var ironSightField = attachmentManagerType.GetField("_ironSightAnchor", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(scopeSlotField, Is.Not.Null);
                Assert.That(ironSightField, Is.Not.Null);
                scopeSlotField.SetValue(manager, scopeSlot);
                ironSightField.SetValue(manager, ironSightAnchor);

                highOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                lowOpticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                highOpticPrefab = new GameObject("OpticHighPrefab");
                var highSightAnchor = new GameObject("SightAnchor").transform;
                highSightAnchor.SetParent(highOpticPrefab.transform, false);
                lowOpticPrefab = new GameObject("OpticLowPrefab");
                var lowSightAnchor = new GameObject("SightAnchor").transform;
                lowSightAnchor.SetParent(lowOpticPrefab.transform, false);

                SetField(opticDefinitionType, highOpticDefinition, "_opticId", "att-optic-8x");
                SetField(opticDefinitionType, highOpticDefinition, "_isVariableZoom", true);
                SetField(opticDefinitionType, highOpticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, highOpticDefinition, "_magnificationMax", 12f);
                SetField(opticDefinitionType, highOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, highOpticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "Auto"));
                SetField(opticDefinitionType, highOpticDefinition, "_opticPrefab", highOpticPrefab);

                SetField(opticDefinitionType, lowOpticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, lowOpticDefinition, "_isVariableZoom", false);
                SetField(opticDefinitionType, lowOpticDefinition, "_magnificationMin", 1f);
                SetField(opticDefinitionType, lowOpticDefinition, "_magnificationMax", 1f);
                SetField(opticDefinitionType, lowOpticDefinition, "_magnificationStep", 1f);
                SetField(opticDefinitionType, lowOpticDefinition, "_visualModePolicy", Enum.Parse(ResolveType("Reloader.Game.Weapons.AdsVisualMode"), "Auto"));
                SetField(opticDefinitionType, lowOpticDefinition, "_opticPrefab", lowOpticPrefab);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-4x", WeaponAttachmentSlotType.Scope),
                    WeaponAttachmentItemMetadata.CreateForTests("att-optic-8x", WeaponAttachmentSlotType.Scope)
                });

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-optic-8x");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                Assert.That(equippedView, Is.Not.Null);
                var bridgedManager = equippedView.GetComponent(attachmentManagerType);
                Assert.That(bridgedManager, Is.Not.Null);

                var activeOpticProperty = attachmentManagerType.GetProperty("ActiveOpticDefinition", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(activeOpticProperty, Is.Not.Null);
                var activeOptic = activeOpticProperty.GetValue(bridgedManager);
                Assert.That(activeOptic, Is.Not.Null);
                Assert.That(scopeSlot.childCount, Is.EqualTo(1), "Scope should be mounted into scope slot.");
                Assert.That(scopeSlot.GetChild(0).gameObject.layer, Is.EqualTo(scopeSlot.gameObject.layer), "Mounted scope visual must inherit slot layer so the viewmodel camera can render it.");
                var opticIdProperty = opticDefinitionType.GetProperty("OpticId", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(opticIdProperty, Is.Not.Null);
                Assert.That((string)opticIdProperty.GetValue(activeOptic), Is.EqualTo("att-optic-8x"));

                worldCameraGo = new GameObject("WorldCam");
                var worldCamera = worldCameraGo.AddComponent<Camera>();
                viewmodelCameraGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCameraGo.AddComponent<Camera>();
                scopeMaskGo = new GameObject("ScopeMask");
                var scopeMask = scopeMaskGo.AddComponent(scopeMaskControllerType);
                var ads = root.AddComponent(adsControllerType);
                SetField(adsControllerType, ads, "_worldCamera", worldCamera);
                SetField(adsControllerType, ads, "_viewmodelCamera", viewmodelCamera);
                SetField(adsControllerType, ads, "_attachmentManager", bridgedManager);
                SetField(adsControllerType, ads, "_scopeMaskController", scopeMask);
                SetField(adsControllerType, ads, "_useLegacyInput", false);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 8f);
                yield return null;
                yield return null;
                Assert.That((bool)GetProperty(scopeMask, "IsMaskVisible"), Is.True);

                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x"), Is.True);
                Invoke(ads, "SetMagnification", 1f);
                yield return null;
                yield return null;
                Assert.That((bool)GetProperty(scopeMask, "IsMaskVisible"), Is.False);
                Assert.That(scopeSlot.childCount, Is.EqualTo(1), "Scope should remain mounted after hot swap.");
                Assert.That(scopeSlot.GetChild(0).gameObject.layer, Is.EqualTo(scopeSlot.gameObject.layer), "Swapped scope visual must inherit slot layer.");

                activeOptic = activeOpticProperty.GetValue(bridgedManager);
                Assert.That((string)opticIdProperty.GetValue(activeOptic), Is.EqualTo("att-optic-4x"));
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

                if (highOpticDefinition != null)
                {
                    Object.Destroy(highOpticDefinition);
                }

                if (lowOpticDefinition != null)
                {
                    Object.Destroy(lowOpticDefinition);
                }

                if (highOpticPrefab != null)
                {
                    Object.Destroy(highOpticPrefab);
                }

                if (lowOpticPrefab != null)
                {
                    Object.Destroy(lowOpticPrefab);
                }

                if (worldCameraGo != null)
                {
                    Object.Destroy(worldCameraGo);
                }

                if (viewmodelCameraGo != null)
                {
                    Object.Destroy(viewmodelCameraGo);
                }

                if (scopeMaskGo != null)
                {
                    Object.Destroy(scopeMaskGo);
                }
            }
        }

        [UnityTest]
        public IEnumerator TrySwapEquippedWeaponAttachment_ReturnsFalse_WhenScopeCannotMount_AndRollsBackInventory()
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
                runtime.TryAddStackItem("att-optic-4x", 1, out _, out _, out _);

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.05f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-optic-4x" })
                });

                // No ScopeSlot on purpose: runtime mount should fail.
                viewPrefab = new GameObject("ViewPrefabWithoutScopeSlot");
                new GameObject("IronSightAnchor").transform.SetParent(viewPrefab.transform, false);

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("OpticPrefab");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-optic-4x");
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);

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
                        opticDefinition)
                });

                yield return null;

                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));
                Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(1));

                var swapped = controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, "att-optic-4x");
                Assert.That(swapped, Is.False, "Swap must fail when runtime cannot mount scope.");
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty), "State should roll back when mount fails.");
                Assert.That(runtime.GetItemQuantity("att-optic-4x"), Is.EqualTo(1), "Inventory should not consume attachment on failed mount.");
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
        public IEnumerator TrySwapEquippedWeaponAttachment_RemoveScope_DestroysAuthoredScopeVisual()
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
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Scope, new[] { "att-kar98k-optic" })
                });

                viewPrefab = new GameObject("Kar98kView");
                new GameObject("ScopeSlot").transform.SetParent(viewPrefab.transform, false);
                new GameObject("IronSightAnchor").transform.SetParent(viewPrefab.transform, false);
                var authoredScopeVisual = new GameObject("WWII_Optic_Remote_Range_A");
                authoredScopeVisual.AddComponent<MeshFilter>();
                authoredScopeVisual.AddComponent<MeshRenderer>();
                authoredScopeVisual.transform.SetParent(viewPrefab.transform, false);

                opticDefinition = ScriptableObject.CreateInstance(opticDefinitionType);
                opticPrefab = new GameObject("WWII_Optic_Remote_Range_A");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                SetField(opticDefinitionType, opticDefinition, "_opticId", "att-kar98k-optic");
                SetField(opticDefinitionType, opticDefinition, "_opticPrefab", opticPrefab);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMin", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationMax", 4f);
                SetField(opticDefinitionType, opticDefinition, "_magnificationStep", 1f);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests(
                        "att-kar98k-optic",
                        WeaponAttachmentSlotType.Scope,
                        opticDefinition)
                });

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope, "att-kar98k-optic");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                var equippedView = GetControllerField<GameObject>(controller, "_equippedWeaponView");
                Assert.That(equippedView, Is.Not.Null);

                var authoredScopePresentBeforeRemove = false;
                var transformsBefore = equippedView.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transformsBefore.Length; i++)
                {
                    if (string.Equals(transformsBefore[i].name, "WWII_Optic_Remote_Range_A", StringComparison.Ordinal))
                    {
                        authoredScopePresentBeforeRemove = true;
                        break;
                    }
                }

                Assert.That(authoredScopePresentBeforeRemove, Is.True, "Test setup should include authored scope visual.");
                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Scope, string.Empty), Is.True);
                yield return null;

                var authoredScopePresentAfterRemove = false;
                var transformsAfter = equippedView.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transformsAfter.Length; i++)
                {
                    if (string.Equals(transformsAfter[i].name, "WWII_Optic_Remote_Range_A", StringComparison.Ordinal))
                    {
                        authoredScopePresentAfterRemove = true;
                        break;
                    }
                }

                Assert.That(authoredScopePresentAfterRemove, Is.False, "Removing scope should remove authored scope visuals from equipped view.");
                Assert.That(state.GetEquippedAttachmentItemId(WeaponAttachmentSlotType.Scope), Is.EqualTo(string.Empty));
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
        public IEnumerator TrySwapEquippedWeaponAttachment_MuzzleHotSwap_UpdatesFireOverrideDeterministically()
        {
            var runtimeEventsBefore = RuntimeKernelBootstrapper.Events;
            RuntimeKernelBootstrapper.Events = new DefaultRuntimeEvents();

            var muzzleRuntimeType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentRuntime");
            var muzzleDefinitionType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
            Assert.That(muzzleRuntimeType, Is.Not.Null);
            Assert.That(muzzleDefinitionType, Is.Not.Null);

            GameObject root = null;
            GameObject registryGo = null;
            WeaponDefinition definition = null;
            GameObject viewPrefab = null;
            UnityEngine.Object muzzleA = null;
            UnityEngine.Object muzzleB = null;
            AudioClip clipA = null;
            AudioClip clipB = null;
            GameObject muzzlePrefabA = null;
            GameObject muzzlePrefabB = null;

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
                runtime.TryAddStackItem("att-muzzle-a", 1, out _, out _, out _);
                runtime.TryAddStackItem("att-muzzle-b", 1, out _, out _, out _);

                var emitterSpy = root.AddComponent<ClipCaptureWeaponCombatAudioEmitter>();

                registryGo = new GameObject("Registry");
                var registry = registryGo.AddComponent<WeaponRegistry>();
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.SetRuntimeValuesForTests("weapon-kar98k", "Rifle", 5, 0.01f, 80f, 0f, 20f, 120f, 1, 0, true);
                definition.SetAttachmentCompatibilitiesForTests(new[]
                {
                    WeaponAttachmentCompatibility.Create(WeaponAttachmentSlotType.Muzzle, new[] { "att-muzzle-a", "att-muzzle-b" })
                });

                viewPrefab = new GameObject("ViewPrefabWithMuzzleRuntime");
                var muzzleSocket = new GameObject("Muzzle").transform;
                muzzleSocket.SetParent(viewPrefab.transform, false);
                var attachmentSlot = new GameObject("MuzzleAttachmentSlot").transform;
                attachmentSlot.SetParent(viewPrefab.transform, false);

                var runtimeComponent = viewPrefab.AddComponent(muzzleRuntimeType);
                SetField(muzzleRuntimeType, runtimeComponent, "_muzzleSocket", muzzleSocket);
                SetField(muzzleRuntimeType, runtimeComponent, "_attachmentSlot", attachmentSlot);

                muzzleA = ScriptableObject.CreateInstance(muzzleDefinitionType);
                muzzleB = ScriptableObject.CreateInstance(muzzleDefinitionType);
                clipA = AudioClip.Create("muzzle-a", 128, 1, 44100, false);
                clipB = AudioClip.Create("muzzle-b", 128, 1, 44100, false);
                muzzlePrefabA = new GameObject("MuzzlePrefabA");
                muzzlePrefabB = new GameObject("MuzzlePrefabB");
                SetField(muzzleDefinitionType, muzzleA, "_attachmentId", "att-muzzle-a");
                SetField(muzzleDefinitionType, muzzleA, "_muzzlePrefab", muzzlePrefabA);
                SetField(muzzleDefinitionType, muzzleA, "_fireClipOverride", clipA);
                SetField(muzzleDefinitionType, muzzleB, "_attachmentId", "att-muzzle-b");
                SetField(muzzleDefinitionType, muzzleB, "_muzzlePrefab", muzzlePrefabB);
                SetField(muzzleDefinitionType, muzzleB, "_fireClipOverride", clipB);
                SetField(muzzleRuntimeType, runtimeComponent, "_defaultAttachment", muzzleB);

                var iconPrefabField = typeof(WeaponDefinition).GetField("_iconSourcePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(iconPrefabField, Is.Not.Null);
                iconPrefabField.SetValue(definition, viewPrefab);
                registry.SetDefinitionsForTests(new[] { definition });

                var controller = root.AddComponent<PlayerWeaponController>();
                SetControllerField(controller, "_weaponRegistry", registry);
                SetControllerField(controller, "_combatAudioEmitter", emitterSpy);
                SetControllerField(controller, "_attachmentItemMetadata", new[]
                {
                    WeaponAttachmentItemMetadata.CreateForTests("att-muzzle-a", WeaponAttachmentSlotType.Muzzle),
                    WeaponAttachmentItemMetadata.CreateForTests("att-muzzle-b", WeaponAttachmentSlotType.Muzzle)
                });

                yield return null;
                Assert.That(controller.TryGetRuntimeState("weapon-kar98k", out var state), Is.True);
                state.SetEquippedAttachmentItemId(WeaponAttachmentSlotType.Muzzle, "att-muzzle-b");

                runtime.SelectBeltSlot(1);
                yield return null;
                runtime.SelectBeltSlot(0);
                yield return null;

                input.FirePressedThisFrame = true;
                yield return null;
                Assert.That(emitterSpy.LastFireOverrideClip, Is.SameAs(clipB), "Initial equipped muzzle override should match runtime slot id.");

                Assert.That(controller.TrySwapEquippedWeaponAttachment(WeaponAttachmentSlotType.Muzzle, "att-muzzle-a"), Is.True);
                input.FirePressedThisFrame = true;
                yield return null;
                Assert.That(emitterSpy.LastFireOverrideClip, Is.SameAs(clipA), "Hot-swapped muzzle override should apply immediately without fallback randomness.");
            }
            finally
            {
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

                if (viewPrefab != null)
                {
                    Object.Destroy(viewPrefab);
                }

                if (muzzleA != null)
                {
                    Object.Destroy(muzzleA);
                }

                if (muzzleB != null)
                {
                    Object.Destroy(muzzleB);
                }

                if (clipA != null)
                {
                    Object.Destroy(clipA);
                }

                if (clipB != null)
                {
                    Object.Destroy(clipB);
                }

                if (muzzlePrefabA != null)
                {
                    Object.Destroy(muzzlePrefabA);
                }

                if (muzzlePrefabB != null)
                {
                    Object.Destroy(muzzlePrefabB);
                }
            }
        }

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

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public bool SprintHeldValue;
            public bool AimHeldValue;
            public float ZoomQueued;
            public int ZeroAdjustQueued;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => SprintHeldValue;
            public bool AimHeld => AimHeldValue;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumePickupPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeAimTogglePressed() => false;
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

        private sealed class TestPickupResolver : MonoBehaviour, IInventoryPickupTargetResolver
        {
            public bool TryResolvePickupTarget(out IInventoryPickupTarget target)
            {
                target = null;
                return false;
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

        private static T GetControllerField<T>(PlayerWeaponController controller, string fieldName)
        {
            var field = typeof(PlayerWeaponController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return (T)field.GetValue(controller);
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
