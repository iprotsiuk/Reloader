using System;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.UI.Toolkit.AmmoHud;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class AmmoHudControllerWeaponEventsPlayModeTests
    {
        [Test]
        public void Configure_WithInjectedWeaponEvents_StaticGameEventsDoNotDriveController()
        {
            var root = new GameObject("AmmoHudInjectedEvents");
            var weaponController = BuildWeaponController(root, "weapon-injected", "Injected Round", 3, 14);
            var (binder, visualRoot, label) = BuildAmmoHudBinder();

            var controller = root.AddComponent<AmmoHudController>();
            controller.SetWeaponController(weaponController);
            controller.SetViewBinder(binder);

            var injectedWeaponEvents = new DefaultRuntimeEvents();
            controller.Configure(injectedWeaponEvents);

            GameEvents.RaiseWeaponEquipped("weapon-injected");
            Assert.That(visualRoot.style.display.value, Is.EqualTo(DisplayStyle.None));

            injectedWeaponEvents.RaiseWeaponEquipped("weapon-injected");
            Assert.That(visualRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(label.text, Is.EqualTo("Injected Round 0/14"));

            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void Configure_WithoutInjectedWeaponEvents_RebindsWhenRuntimeKernelHubIsReplaced()
        {
            var originalHub = RuntimeKernelBootstrapper.Events;
            var initialHub = new DefaultRuntimeEvents();
            var replacementHub = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialHub;

            var root = new GameObject("AmmoHudFallbackRebind");
            var weaponController = BuildWeaponController(root, "weapon-fallback", "Initial Round", 4, 18);
            var (binder, visualRoot, label) = BuildAmmoHudBinder();

            var controller = root.AddComponent<AmmoHudController>();
            controller.SetWeaponController(weaponController);
            controller.SetViewBinder(binder);

            try
            {
                initialHub.RaiseWeaponEquipped("weapon-fallback");
                Assert.That(visualRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(label.text, Is.EqualTo("Initial Round 0/18"));

                RuntimeKernelBootstrapper.Events = replacementHub;
                weaponController.ApplyRuntimeState("weapon-fallback", 1, 7, true);
                weaponController.ApplyRuntimeBallistics(
                    "weapon-fallback",
                    new AmmoBallisticSnapshot(AmmoSourceType.Handload, 2650f, 10f, 147f, 0.42f, 1.4f, "Replacement Round"),
                    Array.Empty<AmmoBallisticSnapshot>());

                initialHub.RaiseWeaponFired("weapon-fallback", Vector3.zero, Vector3.forward);
                Assert.That(label.text, Is.EqualTo("Initial Round 0/18"));

                replacementHub.RaiseWeaponFired("weapon-fallback", Vector3.zero, Vector3.forward);
                Assert.That(label.text, Is.EqualTo("Replacement Round 0/7"));
            }
            finally
            {
                RuntimeKernelBootstrapper.Events = originalHub;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static PlayerWeaponController BuildWeaponController(GameObject root, string itemId, string ammoDisplayName, int magCount, int reserveCount)
        {
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.SetRuntimeValuesForTests(
                itemId,
                "Test Weapon",
                magazineCapacity: 10,
                fireIntervalSeconds: 0.2f,
                projectileSpeed: 2400f,
                projectileGravityMultiplier: 1f,
                baseDamage: 35f,
                maxRangeMeters: 500f,
                startingMagazineCount: magCount,
                startingReserveCount: reserveCount,
                startingChamberLoaded: true);

            var registry = root.AddComponent<WeaponRegistry>();
            registry.SetDefinitionsForTests(new[] { definition });

            var weaponController = root.AddComponent<PlayerWeaponController>();
            weaponController.ApplyRuntimeState(itemId, magCount, reserveCount, true);
            weaponController.ApplyRuntimeBallistics(
                itemId,
                new AmmoBallisticSnapshot(AmmoSourceType.Handload, 2650f, 10f, 147f, 0.42f, 1.4f, ammoDisplayName),
                Array.Empty<AmmoBallisticSnapshot>());

            return weaponController;
        }

        private static (AmmoHudViewBinder binder, VisualElement root, Label label) BuildAmmoHudBinder()
        {
            var visualRoot = new VisualElement { name = "ammo__root" };
            var label = new Label { name = "ammo__count-label" };
            visualRoot.Add(label);

            var binder = new AmmoHudViewBinder();
            binder.Initialize(visualRoot);
            return (binder, visualRoot, label);
        }
    }
}
