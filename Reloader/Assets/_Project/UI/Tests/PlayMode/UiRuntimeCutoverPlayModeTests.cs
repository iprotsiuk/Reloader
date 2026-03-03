using System.Collections;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI;
using Reloader.UI.Toolkit.Runtime;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class UiRuntimeCutoverPlayModeTests
    {
        private const string BeltHudScreenId = "belt-hud";
        private const string AmmoHudScreenId = "ammo-hud";
        private const string TabInventoryScreenId = "tab-inventory";
        private const string EscMenuScreenId = "esc-menu";
        private const string TradeScreenId = "trade-ui";
        private const string ReloadingScreenId = "reloading-workbench";
        private const string InteractionHintScreenId = "interaction-hint";

        [SetUp]
        public void SetUp()
        {
            CleanupScene();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupScene();
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_DisablesLegacyPresenters_AndCreatesToolkitDocuments()
        {
            var installerGo = new GameObject("Installer");
            var installer = installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var beltGo = new GameObject("LegacyBelt");
            var ammoGo = new GameObject("LegacyAmmo");
            var tabGo = new GameObject("LegacyTab");
            beltGo.AddComponent<Reloader.UI.BeltHudPresenter>();
            ammoGo.AddComponent<Reloader.UI.AmmoHudPresenter>();
            tabGo.AddComponent<Reloader.UI.TabUiPresenter>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;

            Assert.That(installer.ActiveLegacyPresenterCountForTests(), Is.EqualTo(0));

            var runtimeRoot = Object.FindFirstObjectByType<UiToolkitRuntimeRoot>(FindObjectsInactive.Include);
            Assert.That(runtimeRoot, Is.Not.Null);
            var bridge = runtimeRoot.GetComponent<UiToolkitScreenRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null);
            Assert.That(bridge.ActiveBindingsForTests(), Is.GreaterThanOrEqualTo(2));

            bridge.enabled = false;
            Assert.That(bridge.ActiveBindingsForTests(), Is.EqualTo(0));

            bridge.enabled = true;
            Assert.That(bridge.ActiveBindingsForTests(), Is.GreaterThanOrEqualTo(2));

            Assert.That(runtimeRoot.GetComponentsInChildren<UIDocument>(true).Length, Is.GreaterThanOrEqualTo(7));
        }

        [UnityTest]
        public IEnumerator Bridge_SelfHeals_WhenDependenciesSpawnLate_AndBindsAllScreens()
        {
            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();
            yield return null;

            var runtimeRoot = Object.FindFirstObjectByType<UiToolkitRuntimeRoot>(FindObjectsInactive.Include);
            Assert.That(runtimeRoot, Is.Not.Null);
            var bridge = runtimeRoot.GetComponent<UiToolkitScreenRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null);

            Assert.That(bridge.IsScreenBoundForTests(TradeScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ReloadingScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(InteractionHintScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(EscMenuScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(BeltHudScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(TabInventoryScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(AmmoHudScreenId), Is.False);

            var playerGo = new GameObject("Player");
            playerGo.AddComponent<StubPlayerInputSource>();
            playerGo.AddComponent<PlayerInventoryController>();
            playerGo.AddComponent<WeaponRegistry>();
            playerGo.AddComponent<PlayerWeaponController>();

            yield return new WaitForSecondsRealtime(0.35f);
            yield return null;

            Assert.That(bridge.IsScreenBoundForTests(BeltHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TabInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(AmmoHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TradeScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ReloadingScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(InteractionHintScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(EscMenuScreenId), Is.True);
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator Bridge_DisableEnable_RebindsAllScreenContracts()
        {
            var playerGo = new GameObject("Player");
            playerGo.AddComponent<StubPlayerInputSource>();
            playerGo.AddComponent<PlayerInventoryController>();
            playerGo.AddComponent<WeaponRegistry>();
            playerGo.AddComponent<PlayerWeaponController>();

            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();
            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();
            yield return null;

            var runtimeRoot = Object.FindFirstObjectByType<UiToolkitRuntimeRoot>(FindObjectsInactive.Include);
            Assert.That(runtimeRoot, Is.Not.Null);
            var bridge = runtimeRoot.GetComponent<UiToolkitScreenRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null);
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(7));

            bridge.enabled = false;
            yield return null;
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(0));

            bridge.enabled = true;
            yield return null;
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(7));

            Assert.That(bridge.IsScreenBoundForTests(BeltHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TabInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(AmmoHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TradeScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ReloadingScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(InteractionHintScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(EscMenuScreenId), Is.True);
        }

        private static void CleanupScene()
        {
            DestroyOwnersOfType<UiToolkitRuntimeRoot>();
            DestroyOwnersOfType<UiToolkitRuntimeInstaller>();
            DestroyOwnersOfType<BeltHudBootstrap>();
            DestroyOwnersOfType<PlayerInventoryController>();
            DestroyOwnersOfType<PlayerWeaponController>();
            DestroyOwnersOfType<WeaponRegistry>();
            DestroyOwnersOfType<StubPlayerInputSource>();
            DestroyOwnersOfType<BeltHudPresenter>();
            DestroyOwnersOfType<AmmoHudPresenter>();
            DestroyOwnersOfType<TabUiPresenter>();
        }

        private static void DestroyOwnersOfType<T>() where T : Component
        {
            var components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && component.gameObject != null)
                {
                    Object.DestroyImmediate(component.gameObject);
                }
            }
        }
    }
}

namespace Reloader.UI
{
    // Legacy marker stubs for cutover verification after legacy presenter deletion.
    public sealed class BeltHudPresenter : MonoBehaviour { }
    public sealed class AmmoHudPresenter : MonoBehaviour { }
    public sealed class TabUiPresenter : MonoBehaviour { }

    public sealed class StubPlayerInputSource : MonoBehaviour, IPlayerInputSource
    {
        public Vector2 MoveInput => Vector2.zero;
        public Vector2 LookInput => Vector2.zero;
        public bool SprintHeld => false;
        public bool AimHeld => false;
        public bool ConsumeJumpPressed() => false;
        public bool ConsumeFirePressed() => false;
        public bool ConsumeReloadPressed() => false;
        public bool ConsumePickupPressed() => false;
        public int ConsumeBeltSelectPressed() => -1;
        public bool ConsumeMenuTogglePressed() => false;
        public bool ConsumeAimTogglePressed() => false;
        public float ConsumeZoomInput() => 0f;
        public int ConsumeZeroAdjustStep() => 0;
    }
}
