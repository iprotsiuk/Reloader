using System;
using Reloader.Core;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.AmmoHud;
using Reloader.UI.Toolkit.BeltHud;
using Reloader.UI.Toolkit.Contracts;
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
        private UiToolkitRuntimeRoot _runtimeRoot;
        private IDisposable _beltSubscription;
        private IDisposable _ammoSubscription;
        private IDisposable _tabSubscription;
        private IDisposable _tradeSubscription;
        private IDisposable _reloadingSubscription;
        private bool _awaitingInventoryBinding;

        public void Initialize(UiToolkitRuntimeRoot runtimeRoot)
        {
            _runtimeRoot = runtimeRoot;
            RebuildBindings();
        }

        private void OnEnable()
        {
            RebuildBindings();
        }

        private void OnDisable()
        {
            DisposeSubscriptions();
            _awaitingInventoryBinding = false;
        }

        private void Update()
        {
            if (!_awaitingInventoryBinding || _runtimeRoot == null)
            {
                return;
            }

            var inventoryController = FindFirstObjectByType<PlayerInventoryController>(FindObjectsInactive.Include);
            if (inventoryController == null)
            {
                return;
            }

            RebuildBindings();
        }

        public int ActiveBindingsForTests()
        {
            var count = 0;
            if (_beltSubscription != null) count++;
            if (_ammoSubscription != null) count++;
            if (_tabSubscription != null) count++;
            if (_tradeSubscription != null) count++;
            if (_reloadingSubscription != null) count++;
            return count;
        }

        private void RebuildBindings()
        {
            DisposeSubscriptions();
            if (_runtimeRoot == null)
            {
                return;
            }

            var inventoryController = FindFirstObjectByType<PlayerInventoryController>(FindObjectsInactive.Include);
            var weaponController = FindFirstObjectByType<PlayerWeaponController>(FindObjectsInactive.Include);
            var inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            _awaitingInventoryBinding = inventoryController == null;

            BindBeltHud(inventoryController);
            BindAmmoHud(weaponController);
            BindTabMenu(inventoryController, inputSource);
            BindTradeMenu();
            BindReloadingWorkbench();
        }

        private void BindBeltHud(PlayerInventoryController inventoryController)
        {
            if (!TryGetDocumentRoot("belt-hud", out var root))
            {
                return;
            }

            var viewBinder = new BeltHudViewBinder();
            viewBinder.Initialize(root, PlayerInventoryRuntime.BeltSlotCount);

            var controller = GetOrAddController<BeltHudController>("belt-hud-controller");
            controller.SetInventoryController(inventoryController);
            controller.SetViewBinder(viewBinder);
            _beltSubscription = UiContractGuard.Bind(controller, viewBinder);
        }

        private void BindAmmoHud(PlayerWeaponController weaponController)
        {
            if (!TryGetDocumentRoot("ammo-hud", out var root))
            {
                return;
            }

            var viewBinder = new AmmoHudViewBinder();
            viewBinder.Initialize(root);

            var controller = GetOrAddController<AmmoHudController>("ammo-hud-controller");
            controller.SetWeaponController(weaponController);
            controller.SetViewBinder(viewBinder);
            _ammoSubscription = UiContractGuard.Bind(controller, viewBinder);
        }

        private void BindTabMenu(PlayerInventoryController inventoryController, IPlayerInputSource inputSource)
        {
            if (!TryGetDocumentRoot("tab-inventory", out var root))
            {
                return;
            }

            var backpackSlotCount = Mathf.Max(16, inventoryController?.Runtime?.BackpackCapacity ?? 0);
            var viewBinder = new TabInventoryViewBinder();
            viewBinder.Initialize(root, PlayerInventoryRuntime.BeltSlotCount, backpackSlotCount);
            var controller = GetOrAddController<TabInventoryController>("tab-menu-controller");
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.Configure(viewBinder, null);
            _tabSubscription = UiContractGuard.Bind(controller, viewBinder);
        }

        private void BindTradeMenu()
        {
            if (!TryGetDocumentRoot("trade-ui", out var root))
            {
                return;
            }

            var viewBinder = new TradeViewBinder();
            viewBinder.Initialize(root);

            var controller = GetOrAddController<TradeController>("trade-menu-controller");
            controller.SetViewBinder(viewBinder);
            _tradeSubscription = UiContractGuard.Bind(controller, viewBinder);
        }

        private void BindReloadingWorkbench()
        {
            if (!TryGetDocumentRoot("reloading-workbench", out var root))
            {
                return;
            }

            var viewBinder = new ReloadingWorkbenchViewBinder();
            viewBinder.Initialize(root, operationCount: 3);

            var controller = GetOrAddController<ReloadingWorkbenchController>("reloading-menu-controller");
            controller.SetViewBinder(viewBinder);
            _reloadingSubscription = UiContractGuard.Bind(controller, viewBinder);
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
            _beltSubscription?.Dispose();
            _beltSubscription = null;
            _ammoSubscription?.Dispose();
            _ammoSubscription = null;
            _tabSubscription?.Dispose();
            _tabSubscription = null;
            _tradeSubscription?.Dispose();
            _tradeSubscription = null;
            _reloadingSubscription?.Dispose();
            _reloadingSubscription = null;
        }
    }
}
