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
        private const string BeltHudScreenId = UiRuntimeCompositionIds.ScreenIds.BeltHud;
        private const string AmmoHudScreenId = UiRuntimeCompositionIds.ScreenIds.AmmoHud;
        private const string TabInventoryScreenId = UiRuntimeCompositionIds.ScreenIds.TabInventory;
        private const string ChestInventoryScreenId = UiRuntimeCompositionIds.ScreenIds.ChestInventory;
        private const string EscMenuScreenId = UiRuntimeCompositionIds.ScreenIds.EscMenu;
        private const string TradeScreenId = UiRuntimeCompositionIds.ScreenIds.Trade;
        private const string ReloadingScreenId = UiRuntimeCompositionIds.ScreenIds.ReloadingWorkbench;
        private const string InteractionHintScreenId = UiRuntimeCompositionIds.ScreenIds.InteractionHint;
        private const string DialogueOverlayScreenId = UiRuntimeCompositionIds.ScreenIds.DialogueOverlay;
        private const string DevConsoleScreenId = UiRuntimeCompositionIds.ScreenIds.DevConsole;

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
        public IEnumerator ExecuteCutover_CreatesToolkitDocumentsAndRuntimeBridge()
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
            Assert.That(bridge.IsScreenBoundForTests(DevConsoleScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(BeltHudScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(TabInventoryScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(AmmoHudScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(ChestInventoryScreenId), Is.False);

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
            Assert.That(bridge.IsScreenBoundForTests(ChestInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TradeScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ReloadingScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(InteractionHintScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(EscMenuScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(DialogueOverlayScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(DevConsoleScreenId), Is.True);
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(10));
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
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(10));

            bridge.enabled = false;
            yield return null;
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(0));

            bridge.enabled = true;
            yield return null;
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(10));

            Assert.That(bridge.IsScreenBoundForTests(BeltHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TabInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(AmmoHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ChestInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TradeScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ReloadingScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(InteractionHintScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(EscMenuScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(DialogueOverlayScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(DevConsoleScreenId), Is.True);
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_TabInventoryUsesThreeRegionShell()
        {
            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;

            var runtimeRoot = Object.FindFirstObjectByType<UiToolkitRuntimeRoot>(FindObjectsInactive.Include);
            Assert.That(runtimeRoot, Is.Not.Null);

            var documentTransform = runtimeRoot.transform.Find(TabInventoryScreenId);
            Assert.That(documentTransform, Is.Not.Null);

            var document = documentTransform.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);

            var root = document.rootVisualElement;
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Q<VisualElement>("inventory__shell"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("inventory__rail"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("inventory__workspace"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("inventory__detail-pane"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_TabInventoryUsesIconRailNavigation()
        {
            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;

            var runtimeRoot = Object.FindFirstObjectByType<UiToolkitRuntimeRoot>(FindObjectsInactive.Include);
            Assert.That(runtimeRoot, Is.Not.Null);

            var documentTransform = runtimeRoot.transform.Find(TabInventoryScreenId);
            Assert.That(documentTransform, Is.Not.Null);

            var document = documentTransform.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);

            var root = document.rootVisualElement;
            Assert.That(root, Is.Not.Null);

            var tabBar = root.Q<VisualElement>("inventory__tabbar");
            var inventoryTab = root.Q<Button>("inventory__tab-inventory");
            var contractsTab = root.Q<Button>("inventory__tab-quests");
            var journalTab = root.Q<Button>("inventory__tab-journal");
            var calendarTab = root.Q<Button>("inventory__tab-calendar");
            var deviceTab = root.Q<Button>("inventory__tab-device");

            Assert.That(tabBar.ClassListContains("inventory__tabbar--icon-rail"), Is.True);
            Assert.That(inventoryTab.ClassListContains("inventory__tab--inventory"), Is.True);
            Assert.That(contractsTab.ClassListContains("inventory__tab--contracts"), Is.True);
            Assert.That(journalTab.ClassListContains("inventory__tab--journal"), Is.True);
            Assert.That(calendarTab.ClassListContains("inventory__tab--calendar"), Is.True);
            Assert.That(deviceTab.ClassListContains("inventory__tab--device"), Is.True);
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
        public bool ConsumeDevConsoleTogglePressed() => false;
        public bool ConsumeAutocompletePressed() => false;
        public int ConsumeSuggestionDelta() => 0;
        public bool ConsumeAimTogglePressed() => false;
        public float ConsumeZoomInput() => 0f;
        public int ConsumeZeroAdjustStep() => 0;
    }
}
