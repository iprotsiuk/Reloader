using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Startup.Runtime;
using Reloader.UI;
using Reloader.UI.Toolkit.Runtime;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Reloader.UI.Tests.PlayMode
{
    public class UiRuntimeCutoverPlayModeTests
    {
        private const string BeltHudScreenId = UiRuntimeCompositionIds.ScreenIds.BeltHud;
        private const string CompassHudScreenId = UiRuntimeCompositionIds.ScreenIds.CompassHud;
        private const string AmmoHudScreenId = UiRuntimeCompositionIds.ScreenIds.AmmoHud;
        private const string HealthHudScreenId = UiRuntimeCompositionIds.ScreenIds.HealthHud;
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

            Assert.That(runtimeRoot.GetComponentsInChildren<UIDocument>(true).Length, Is.GreaterThanOrEqualTo(8));
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_WithEventSystem_CreatesPanelRaycasterForRuntimePanels()
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;
            yield return null;

            Assert.That(
                Object.FindFirstObjectByType<PanelRaycaster>(FindObjectsInactive.Include),
                Is.Not.Null,
                "Expected runtime UI Toolkit cutover to create a PanelRaycaster bridge under the active EventSystem so menus can receive clicks.");
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_WithActiveStartupPanelBridge_CreatesSeparateRuntimePanelRaycaster()
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
            var eventSystem = eventSystemGo.GetComponent<EventSystem>();

            var startupGo = new GameObject("StartupMenu");
            startupGo.SetActive(false);
            var startupController = startupGo.AddComponent<StartupMenuController>();
            var startupPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            startupPanelSettings.name = "StartupPanelSettings";
            SetPrivateField(startupController, "_panelSettings", startupPanelSettings);
            startupGo.SetActive(true);

            yield return null;
            yield return null;

            Assert.That(
                CountPanelRaycastersForEventSystem(eventSystem),
                Is.EqualTo(1),
                "Expected the startup menu to own the only active panel bridge before gameplay UI cutover.");

            var installerGo = new GameObject("Installer");
            var installer = installerGo.AddComponent<UiToolkitRuntimeInstaller>();
            var runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            runtimePanelSettings.name = "RuntimePanelSettings";
            SetPrivateField(installer, "_panelSettings", runtimePanelSettings);

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;
            yield return null;

            Assert.That(
                CountPanelRaycastersForEventSystem(eventSystem),
                Is.GreaterThanOrEqualTo(2),
                "Expected gameplay UI cutover to add its own runtime panel bridge instead of treating the startup menu bridge as sufficient.");
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_RearmsEventSystemAfterDisableEnableCycle()
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;
            yield return null;

            var eventSystem = eventSystemGo.GetComponent<EventSystem>();
            var inputSystemUiModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            Assert.That(inputSystemUiModuleType, Is.Not.Null, "Expected InputSystemUIInputModule type to resolve.");

            var currentInputModuleProperty = typeof(EventSystem).GetProperty("currentInputModule", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(currentInputModuleProperty, Is.Not.Null, "Expected EventSystem.currentInputModule property.");

            var uiModule = eventSystemGo.GetComponent(inputSystemUiModuleType!);
            Assert.That(currentInputModuleProperty!.GetValue(eventSystem), Is.SameAs(uiModule),
                "Expected the gameplay EventSystem to be armed before the disable/enable cycle.");
            Assert.That(
                FindPanelRaycasterForEventSystem(eventSystem),
                Is.Not.Null,
                "Expected the runtime UI bridge to have already created a PanelRaycaster before the disable/enable cycle.");

            eventSystemGo.SetActive(false);
            yield return null;

            eventSystemGo.SetActive(true);
            yield return null;
            yield return null;

            Assert.That(currentInputModuleProperty.GetValue(eventSystem), Is.SameAs(uiModule),
                "Expected the runtime UI bridge to rearm the gameplay EventSystem after it is re-enabled.");
            Assert.That(
                FindPanelRaycasterForEventSystem(eventSystem),
                Is.Not.Null,
                "Expected the rearmed gameplay EventSystem to keep the runtime PanelRaycaster bound after travel-style reactivation.");
            Assert.That(
                CountPanelRaycastersForEventSystem(eventSystem),
                Is.EqualTo(1),
                "Expected the existing runtime PanelRaycaster bridge to stay intact across the disable/enable cycle.");
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_IgnoresInactiveStalePanelRaycaster_AndCreatesActiveGameplayBridge()
        {
            var staleEventSystemGo = new GameObject("StaleEventSystem");
            staleEventSystemGo.AddComponent<EventSystem>();
            var staleBridgeGo = new GameObject("RuntimePanelSettings");
            staleBridgeGo.transform.SetParent(staleEventSystemGo.transform, false);
            staleBridgeGo.AddComponent<PanelRaycaster>();
            staleEventSystemGo.SetActive(false);

            var activeEventSystemGo = new GameObject("ActiveEventSystem");
            activeEventSystemGo.AddComponent<EventSystem>();
            activeEventSystemGo.AddComponent<InputSystemUIInputModule>();

            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;
            yield return null;

            var raycasters = Object.FindObjectsByType<PanelRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var activeRaycasterCount = 0;
            PanelRaycaster gameplayRaycaster = null;
            for (var i = 0; i < raycasters.Length; i++)
            {
                var raycaster = raycasters[i];
                if (raycaster == null || !raycaster.gameObject.activeInHierarchy)
                {
                    continue;
                }

                activeRaycasterCount++;
                gameplayRaycaster = raycaster;
            }

            Assert.That(activeRaycasterCount, Is.EqualTo(1),
                "Expected runtime UI cutover to recreate an active gameplay PanelRaycaster even if a stale inactive bridge exists.");
            Assert.That(gameplayRaycaster, Is.Not.Null);
            Assert.That(gameplayRaycaster!.GetComponentInParent<EventSystem>(), Is.SameAs(activeEventSystemGo.GetComponent<EventSystem>()),
                "Expected the recreated gameplay PanelRaycaster to bind under the active EventSystem.");
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_IgnoresDisabledPanelRaycaster_AndCreatesActiveGameplayBridge()
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            var staleBridgeGo = new GameObject("DisabledRuntimePanelSettings");
            staleBridgeGo.transform.SetParent(eventSystemGo.transform, false);
            var staleRaycaster = staleBridgeGo.AddComponent<PanelRaycaster>();
            staleRaycaster.enabled = false;

            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;
            yield return null;

            var raycasters = Object.FindObjectsByType<PanelRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var activeRaycasterCount = 0;
            PanelRaycaster gameplayRaycaster = null;
            for (var i = 0; i < raycasters.Length; i++)
            {
                var raycaster = raycasters[i];
                if (raycaster == null || !raycaster.isActiveAndEnabled)
                {
                    continue;
                }

                activeRaycasterCount++;
                gameplayRaycaster = raycaster;
            }

            Assert.That(activeRaycasterCount, Is.EqualTo(1),
                "Expected runtime UI cutover to recreate an active gameplay PanelRaycaster even if a stale disabled bridge exists.");
            Assert.That(gameplayRaycaster, Is.Not.Null);
            Assert.That(gameplayRaycaster!.GetComponentInParent<EventSystem>(), Is.SameAs(eventSystemGo.GetComponent<EventSystem>()),
                "Expected the recreated gameplay PanelRaycaster to bind under the active EventSystem.");
        }

        [UnityTest]
        public IEnumerator ExecuteCutover_PrefersActiveSceneEventSystem_OverStaleCurrentEventSystem()
        {
            var gameplayEventSystemGo = new GameObject("GameplayEventSystem");
            gameplayEventSystemGo.AddComponent<EventSystem>();
            gameplayEventSystemGo.AddComponent<InputSystemUIInputModule>();

            var staleScene = SceneManager.CreateScene("StaleUiEventSystemScene");
            var staleEventSystemGo = new GameObject("StaleEventSystem");
            staleEventSystemGo.AddComponent<EventSystem>();
            staleEventSystemGo.AddComponent<InputSystemUIInputModule>();
            SceneManager.MoveGameObjectToScene(staleEventSystemGo, staleScene);
            staleEventSystemGo.AddComponent<PanelRaycaster>();
            staleEventSystemGo.SetActive(false);
            staleEventSystemGo.SetActive(true);

            var installerGo = new GameObject("Installer");
            installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;
            yield return null;

            Assert.That(
                FindPanelRaycasterForEventSystem(gameplayEventSystemGo.GetComponent<EventSystem>()),
                Is.Not.Null,
                "Expected runtime UI bridge to bind the active-scene EventSystem even when a stale current EventSystem exists in another scene.");
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
            Assert.That(bridge.IsScreenBoundForTests(CompassHudScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(TabInventoryScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(AmmoHudScreenId), Is.False);
            Assert.That(bridge.IsScreenBoundForTests(HealthHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ChestInventoryScreenId), Is.False);

            var playerGo = new GameObject("Player");
            playerGo.AddComponent<StubPlayerInputSource>();
            playerGo.AddComponent<PlayerInventoryController>();
            playerGo.AddComponent<WeaponRegistry>();
            playerGo.AddComponent<PlayerWeaponController>();

            yield return new WaitForSecondsRealtime(0.35f);
            yield return null;

            Assert.That(bridge.IsScreenBoundForTests(BeltHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(CompassHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TabInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(AmmoHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ChestInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TradeScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ReloadingScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(InteractionHintScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(EscMenuScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(DialogueOverlayScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(DevConsoleScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(HealthHudScreenId), Is.True);
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(12));
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
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(12));

            bridge.enabled = false;
            yield return null;
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(0));

            bridge.enabled = true;
            yield return null;
            Assert.That(bridge.BoundScreenCountForTests(), Is.EqualTo(12));

            Assert.That(bridge.IsScreenBoundForTests(BeltHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(CompassHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TabInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(AmmoHudScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ChestInventoryScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(TradeScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(ReloadingScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(InteractionHintScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(EscMenuScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(DialogueOverlayScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(DevConsoleScreenId), Is.True);
            Assert.That(bridge.IsScreenBoundForTests(HealthHudScreenId), Is.True);
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
            DestroyOwnersOfType<StartupMenuController>();
            DestroyOwnersOfType<UiToolkitRuntimeRoot>();
            DestroyOwnersOfType<UiToolkitRuntimeInstaller>();
            DestroyOwnersOfType<BeltHudBootstrap>();
            DestroyOwnersOfType<PlayerInventoryController>();
            DestroyOwnersOfType<PlayerWeaponController>();
            DestroyOwnersOfType<WeaponRegistry>();
            DestroyOwnersOfType<StubPlayerInputSource>();
            DestroyOwnersOfType<EventSystem>();
            DestroyOwnersOfType<PanelRaycaster>();
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

        private static PanelRaycaster FindPanelRaycasterForEventSystem(EventSystem eventSystem)
        {
            if (eventSystem == null)
            {
                return null;
            }

            var raycasters = Object.FindObjectsByType<PanelRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < raycasters.Length; i++)
            {
                var raycaster = raycasters[i];
                if (raycaster != null && raycaster.GetComponentInParent<EventSystem>() == eventSystem)
                {
                    return raycaster;
                }
            }

            return null;
        }

        private static int CountPanelRaycastersForEventSystem(EventSystem eventSystem)
        {
            if (eventSystem == null)
            {
                return 0;
            }

            var count = 0;
            var raycasters = Object.FindObjectsByType<PanelRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < raycasters.Length; i++)
            {
                var raycaster = raycasters[i];
                if (raycaster != null
                    && raycaster.isActiveAndEnabled
                    && raycaster.GetComponentInParent<EventSystem>() == eventSystem)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field!.SetValue(target, value);
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
