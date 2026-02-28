using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Reloader.Core;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.AmmoHud;
using Reloader.UI.Toolkit.BeltHud;
using Reloader.UI.Toolkit.ChestInventory;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.InteractionHint;
using Reloader.UI.Toolkit.Reloading;
using Reloader.UI.Toolkit.TabInventory;
using Reloader.UI.Toolkit.Trade;
using Reloader.Weapons.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.Runtime
{
    public sealed class UiToolkitScreenRuntimeBridge : MonoBehaviour
    {
        private const float MinSelfHealIntervalSeconds = 0.05f;
        private const float DefaultSelfHealIntervalSeconds = 0.2f;

        private const string BeltHudScreenId = "belt-hud";
        private const string AmmoHudScreenId = "ammo-hud";
        private const string TabInventoryScreenId = "tab-inventory";
        private const string ChestInventoryScreenId = "chest-inventory";
        private const string TradeScreenId = "trade-ui";
        private const string ReloadingScreenId = "reloading-workbench";
        private const string InteractionHintScreenId = "interaction-hint";

        private static readonly ScreenBindingDefinition[] ScreenBindings =
        {
            new(BeltHudScreenId, "belt-hud-controller", ScreenBindingKind.BeltHud, DependencyRequirement.Inventory),
            new(AmmoHudScreenId, "ammo-hud-controller", ScreenBindingKind.AmmoHud, DependencyRequirement.Weapon),
            new(TabInventoryScreenId, "tab-menu-controller", ScreenBindingKind.TabInventory, DependencyRequirement.Inventory | DependencyRequirement.Input),
            new(ChestInventoryScreenId, "chest-inventory-controller", ScreenBindingKind.ChestInventory, DependencyRequirement.Inventory | DependencyRequirement.Input),
            new(TradeScreenId, "trade-menu-controller", ScreenBindingKind.Trade, DependencyRequirement.None),
            new(ReloadingScreenId, "reloading-menu-controller", ScreenBindingKind.ReloadingWorkbench, DependencyRequirement.None),
            new(InteractionHintScreenId, "interaction-hint-controller", ScreenBindingKind.InteractionHint, DependencyRequirement.None)
        };

        [SerializeField] private float _selfHealIntervalSeconds = DefaultSelfHealIntervalSeconds;

        private UiToolkitRuntimeRoot _runtimeRoot;
        private readonly Dictionary<string, ScreenBindingState> _bindingStates = new(StringComparer.Ordinal);
        private float _nextSelfHealAt;

        public void Initialize(UiToolkitRuntimeRoot runtimeRoot)
        {
            _runtimeRoot = runtimeRoot;
            ForceRefreshBindings();
        }

        private void OnEnable()
        {
            ForceRefreshBindings();
        }

        private void OnDisable()
        {
            DisposeSubscriptions();
        }

        private void Update()
        {
            if (_runtimeRoot == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextSelfHealAt)
            {
                return;
            }

            EnsureBindings();
        }

        public int ActiveBindingsForTests()
        {
            var count = 0;
            foreach (var state in _bindingStates.Values)
            {
                if (state.Subscription != null)
                {
                    count++;
                }
            }

            return count;
        }

        public int BoundScreenCountForTests()
        {
            return ActiveBindingsForTests();
        }

        public bool IsScreenBoundForTests(string screenId)
        {
            return !string.IsNullOrWhiteSpace(screenId)
                   && _bindingStates.TryGetValue(screenId, out var state)
                   && state.Subscription != null;
        }

        private void ForceRefreshBindings()
        {
            _nextSelfHealAt = 0f;
            EnsureBindings(force: true);
        }

        private void EnsureBindings(bool force = false)
        {
            if (_runtimeRoot == null)
            {
                return;
            }

            var dependencies = ResolveDependencies();
            for (var i = 0; i < ScreenBindings.Length; i++)
            {
                EnsureBinding(ScreenBindings[i], dependencies, force);
            }

            _nextSelfHealAt = Time.unscaledTime + Mathf.Max(MinSelfHealIntervalSeconds, _selfHealIntervalSeconds);
        }

        private void EnsureBinding(ScreenBindingDefinition definition, ResolvedDependencies dependencies, bool force)
        {
            if (!TryGetDocumentRoot(definition.ScreenId, out var root))
            {
                UnbindScreen(definition.ScreenId);
                return;
            }

            if (!HasRequirements(definition.Requirements, dependencies))
            {
                UnbindScreen(definition.ScreenId);
                return;
            }

            var dependencyHash = BuildDependencyHash(definition.Requirements, dependencies);
            if (!force
                && _bindingStates.TryGetValue(definition.ScreenId, out var existing)
                && existing.Subscription != null
                && existing.DependencyHash == dependencyHash
                && ReferenceEquals(existing.Root, root))
            {
                return;
            }

            RebindScreen(definition, root, dependencies, dependencyHash);
        }

        private void RebindScreen(ScreenBindingDefinition definition, VisualElement root, ResolvedDependencies dependencies, int dependencyHash)
        {
            UnbindScreen(definition.ScreenId);

            IDisposable subscription = definition.Kind switch
            {
                ScreenBindingKind.BeltHud => BindBeltHud(root, definition.ControllerObjectName, dependencies.InventoryController),
                ScreenBindingKind.AmmoHud => BindAmmoHud(root, definition.ControllerObjectName, dependencies.WeaponController),
                ScreenBindingKind.TabInventory => BindTabInventory(root, definition.ControllerObjectName, dependencies.InventoryController, dependencies.InputSource),
                ScreenBindingKind.ChestInventory => BindChestInventory(root, definition.ControllerObjectName, dependencies.InventoryController, dependencies.InputSource),
                ScreenBindingKind.Trade => BindTrade(root, definition.ControllerObjectName),
                ScreenBindingKind.ReloadingWorkbench => BindReloadingWorkbench(root, definition.ControllerObjectName),
                ScreenBindingKind.InteractionHint => BindInteractionHint(root, definition.ControllerObjectName),
                _ => null
            };

            if (!_bindingStates.TryGetValue(definition.ScreenId, out var state))
            {
                state = new ScreenBindingState();
                _bindingStates[definition.ScreenId] = state;
            }

            state.Subscription = subscription;
            state.DependencyHash = dependencyHash;
            state.Root = root;
        }

        private IDisposable BindBeltHud(VisualElement root, string controllerName, PlayerInventoryController inventoryController)
        {
            var viewBinder = new BeltHudViewBinder();
            viewBinder.Initialize(root, PlayerInventoryRuntime.BeltSlotCount);

            var controller = GetOrAddController<BeltHudController>(controllerName);
            controller.SetInventoryController(inventoryController);
            controller.SetViewBinder(viewBinder);
            return UiContractGuard.Bind(controller, viewBinder);
        }

        private IDisposable BindAmmoHud(VisualElement root, string controllerName, PlayerWeaponController weaponController)
        {
            var viewBinder = new AmmoHudViewBinder();
            viewBinder.Initialize(root);

            var controller = GetOrAddController<AmmoHudController>(controllerName);
            controller.SetWeaponController(weaponController);
            controller.SetViewBinder(viewBinder);
            return UiContractGuard.Bind(controller, viewBinder);
        }

        private IDisposable BindTabInventory(
            VisualElement root,
            string controllerName,
            PlayerInventoryController inventoryController,
            IPlayerInputSource inputSource)
        {
            var backpackSlotCount = Mathf.Max(0, inventoryController?.Runtime?.BackpackCapacity ?? 0);
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, PlayerInventoryRuntime.BeltSlotCount, backpackSlotCount);

            var controller = GetOrAddController<TabInventoryController>(controllerName);
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.Configure(viewBinder, null);
            return UiContractGuard.Bind(controller, viewBinder);
        }

        private IDisposable BindTrade(VisualElement root, string controllerName)
        {
            var viewBinder = new TradeViewBinder();
            viewBinder.Initialize(root);

            var controller = GetOrAddController<TradeController>(controllerName);
            controller.SetViewBinder(viewBinder);
            return UiContractGuard.Bind(controller, viewBinder);
        }

        private IDisposable BindChestInventory(
            VisualElement root,
            string controllerName,
            PlayerInventoryController inventoryController,
            IPlayerInputSource inputSource)
        {
            var playerSlotCount = Mathf.Max(0, inventoryController?.Runtime?.BackpackCapacity ?? 0);
            var viewBinder = new ChestInventoryViewBinder();
            viewBinder.Initialize(root, chestSlotCount: 20, playerSlotCount);

            var controller = GetOrAddController<ChestInventoryController>(controllerName);
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.Configure(viewBinder);
            return UiContractGuard.Bind(controller, viewBinder);
        }

        private IDisposable BindReloadingWorkbench(VisualElement root, string controllerName)
        {
            var viewBinder = new ReloadingWorkbenchViewBinder();
            viewBinder.Initialize(root, operationCount: 3);

            var controller = GetOrAddController<ReloadingWorkbenchController>(controllerName);
            controller.SetViewBinder(viewBinder);
            return UiContractGuard.Bind(controller, viewBinder);
        }

        private IDisposable BindInteractionHint(VisualElement root, string controllerName)
        {
            var viewBinder = new InteractionHintViewBinder();
            viewBinder.Initialize(root);

            var controller = GetOrAddController<InteractionHintController>(controllerName);
            controller.SetViewBinder(viewBinder);
            return UiContractGuard.Bind(controller, viewBinder);
        }

        private TController GetOrAddController<TController>(string goName) where TController : Component
        {
            var target = transform.Find(goName);
            GameObject go;
            if (target == null)
            {
                go = new GameObject(goName);
                go.transform.SetParent(transform, false);
            }
            else
            {
                go = target.gameObject;
            }

            var controller = go.GetComponent<TController>();
            if (controller == null)
            {
                controller = go.AddComponent<TController>();
            }

            return controller;
        }

        private static bool HasRequirements(DependencyRequirement requirements, ResolvedDependencies dependencies)
        {
            if ((requirements & DependencyRequirement.Inventory) != 0 && dependencies.InventoryController == null)
            {
                return false;
            }

            if ((requirements & DependencyRequirement.Weapon) != 0 && dependencies.WeaponController == null)
            {
                return false;
            }

            if ((requirements & DependencyRequirement.Input) != 0 && dependencies.InputSource == null)
            {
                return false;
            }

            return true;
        }

        private static int BuildDependencyHash(DependencyRequirement requirements, ResolvedDependencies dependencies)
        {
            var hash = new HashCode();
            hash.Add((int)requirements);

            if ((requirements & DependencyRequirement.Inventory) != 0)
            {
                hash.Add(dependencies.InventoryController != null ? dependencies.InventoryController.GetInstanceID() : 0);
            }

            if ((requirements & DependencyRequirement.Weapon) != 0)
            {
                hash.Add(dependencies.WeaponController != null ? dependencies.WeaponController.GetInstanceID() : 0);
            }

            if ((requirements & DependencyRequirement.Input) != 0)
            {
                hash.Add(GetReferenceIdentityHash(dependencies.InputSource));
            }

            return hash.ToHashCode();
        }

        private static int GetReferenceIdentityHash(object instance)
        {
            if (instance == null)
            {
                return 0;
            }

            return instance is UnityEngine.Object unityObject
                ? unityObject.GetInstanceID()
                : RuntimeHelpers.GetHashCode(instance);
        }

        private ResolvedDependencies ResolveDependencies()
        {
            return new ResolvedDependencies(
                FindFirstObjectByType<PlayerInventoryController>(FindObjectsInactive.Include),
                FindFirstObjectByType<PlayerWeaponController>(FindObjectsInactive.Include),
                DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)));
        }

        private bool TryGetDocumentRoot(string screenId, out VisualElement root)
        {
            root = null;
            if (_runtimeRoot == null)
            {
                return false;
            }

            var screen = _runtimeRoot.transform.Find(screenId);
            if (screen == null)
            {
                return false;
            }

            var document = screen.GetComponent<UIDocument>();
            if (document == null)
            {
                return false;
            }

            root = document.rootVisualElement;
            return root != null;
        }

        private void DisposeSubscriptions()
        {
            foreach (var state in _bindingStates.Values)
            {
                state.Subscription?.Dispose();
                state.Subscription = null;
                state.Root = null;
                state.DependencyHash = 0;
            }
        }

        private void UnbindScreen(string screenId)
        {
            if (!_bindingStates.TryGetValue(screenId, out var state))
            {
                return;
            }

            state.Subscription?.Dispose();
            state.Subscription = null;
            state.Root = null;
            state.DependencyHash = 0;
        }

        private sealed class ScreenBindingState
        {
            public IDisposable Subscription;
            public int DependencyHash;
            public VisualElement Root;
        }

        private readonly struct ResolvedDependencies
        {
            public ResolvedDependencies(
                PlayerInventoryController inventoryController,
                PlayerWeaponController weaponController,
                IPlayerInputSource inputSource)
            {
                InventoryController = inventoryController;
                WeaponController = weaponController;
                InputSource = inputSource;
            }

            public PlayerInventoryController InventoryController { get; }
            public PlayerWeaponController WeaponController { get; }
            public IPlayerInputSource InputSource { get; }
        }

        [Flags]
        private enum DependencyRequirement
        {
            None = 0,
            Inventory = 1 << 0,
            Weapon = 1 << 1,
            Input = 1 << 2
        }

        private enum ScreenBindingKind
        {
            BeltHud,
            AmmoHud,
            TabInventory,
            ChestInventory,
            Trade,
            ReloadingWorkbench,
            InteractionHint
        }

        private readonly struct ScreenBindingDefinition
        {
            public ScreenBindingDefinition(
                string screenId,
                string controllerObjectName,
                ScreenBindingKind kind,
                DependencyRequirement requirements)
            {
                ScreenId = screenId;
                ControllerObjectName = controllerObjectName;
                Kind = kind;
                Requirements = requirements;
            }

            public string ScreenId { get; }
            public string ControllerObjectName { get; }
            public ScreenBindingKind Kind { get; }
            public DependencyRequirement Requirements { get; }
        }
    }
}
