using System;
using Reloader.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Reloader.Core.Runtime;

namespace Reloader.Player
{
    public interface IPlayerCursorEscapeKeySource
    {
        bool WasEscapePressedThisFrame();
    }

    public sealed class PlayerCursorLockController : MonoBehaviour
    {
        [SerializeField] private bool _lockOnStart;
        [SerializeField] private bool _lockOnClick = true;
        [SerializeField] private bool _unlockOnEscape = true;
        [SerializeField] private bool _unlockOnApplicationFocusLost = true;

        private bool _isCursorLockRequested;
        private bool _isTradeMenuOpen;
        private bool _isWorkbenchMenuOpen;
        private bool _isTabInventoryOpen;
        private bool _isEscMenuOpen;
        private bool _isStorageMenuOpen;
        private RuntimeHubChannelBinder<IUiStateEvents> _uiStateEventsBinder;
        private RuntimeHubChannelBinder<IShopEvents> _shopEventsBinder;
        private IPlayerCursorEscapeKeySource _escapeKeySource;
        private static int _escapeConsumedFrame = -1;

        public static bool IsAnyMenuOpen { get; private set; }
        public bool IsCursorLockRequested { get; private set; }

        private void Awake()
        {
            _escapeKeySource ??= new KeyboardCursorEscapeKeySource();
        }

        private void Start()
        {
            if (_lockOnStart)
            {
                LockCursor();
            }
        }

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            ShopEventsBinder.ResolveAndBind();
            UiStateEventsBinder.ResolveAndBind();
            ReconcileMenuFlagsFromCurrentEventState();
            ApplyCursorState();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            ShopEventsBinder.Unbind();
            UiStateEventsBinder.Unbind();
            IsAnyMenuOpen = false;
        }

        public void Configure(IUiStateEvents uiStateEvents = null, IShopEvents shopEvents = null)
        {
            UiStateEventsBinder.Configure(uiStateEvents);
            ShopEventsBinder.Configure(shopEvents);

            if (isActiveAndEnabled)
            {
                ShopEventsBinder.ResolveAndBind();
                UiStateEventsBinder.ResolveAndBind();
            }
        }

        public void SetEscapeKeySource(IPlayerCursorEscapeKeySource escapeKeySource)
        {
            _escapeKeySource = escapeKeySource ?? new KeyboardCursorEscapeKeySource();
        }

        public static void MarkEscapeConsumedThisFrame()
        {
            _escapeConsumedFrame = Time.frameCount;
        }

        public static bool WasEscapeConsumedThisFrame()
        {
            return _escapeConsumedFrame == Time.frameCount;
        }

        private void Update()
        {
            _escapeKeySource ??= new KeyboardCursorEscapeKeySource();
            var storageMenuOpen = IsStorageUiOpen();
            if (_isStorageMenuOpen != storageMenuOpen)
            {
                _isStorageMenuOpen = storageMenuOpen;
                ApplyCursorState();
            }

            if (IsAnyMenuOpen)
            {
                ApplyCursorState();
                return;
            }

            if (_lockOnClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }

            if (_unlockOnEscape
                && _escapeKeySource.WasEscapePressedThisFrame()
                && !WasEscapeConsumedThisFrame())
            {
                UnlockCursor();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _unlockOnApplicationFocusLost)
            {
                UnlockCursor();
            }
        }

        public void LockCursor()
        {
            _isCursorLockRequested = true;
            IsCursorLockRequested = true;
            ApplyCursorState();
        }

        public void UnlockCursor()
        {
            _isCursorLockRequested = false;
            IsCursorLockRequested = false;
            ApplyCursorState();
        }

        private void HandleShopTradeOpened(string _)
        {
            _isTradeMenuOpen = true;
            ApplyCursorState();
        }

        private void HandleShopTradeClosed()
        {
            _isTradeMenuOpen = false;
            ApplyCursorState();
        }

        private void HandleWorkbenchMenuVisibilityChanged(bool isVisible)
        {
            _isWorkbenchMenuOpen = isVisible;
            ApplyCursorState();
        }

        private void HandleTabInventoryVisibilityChanged(bool isVisible)
        {
            _isTabInventoryOpen = isVisible;
            ApplyCursorState();
        }

        private void HandleEscMenuVisibilityChanged(bool isVisible)
        {
            _isEscMenuOpen = isVisible;
            ApplyCursorState();
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (ShopEventsBinder.UsesRuntimeChannel)
            {
                ShopEventsBinder.ResolveAndBind();
            }

            if (UiStateEventsBinder.UsesRuntimeChannel)
            {
                UiStateEventsBinder.ResolveAndBind();
            }

            ReconcileMenuFlagsFromCurrentEventState();
            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            IsAnyMenuOpen = _isTradeMenuOpen || _isWorkbenchMenuOpen || _isTabInventoryOpen || _isEscMenuOpen || _isStorageMenuOpen;
            if (IsAnyMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Cursor.lockState = _isCursorLockRequested ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_isCursorLockRequested;
        }

        private void ReconcileMenuFlagsFromCurrentEventState()
        {
            var uiStateEvents = ResolveUiStateEvents();
            var shopUiStateEvents = ResolveShopEvents() as IUiStateEvents;
            if (shopUiStateEvents != null)
            {
                _isTradeMenuOpen = shopUiStateEvents.IsShopTradeMenuOpen;
            }
            else if (uiStateEvents != null)
            {
                _isTradeMenuOpen = uiStateEvents.IsShopTradeMenuOpen;
            }
            else if (ShopEventsBinder.UsesRuntimeChannel || UiStateEventsBinder.UsesRuntimeChannel)
            {
                _isTradeMenuOpen = false;
            }

            if (uiStateEvents != null)
            {
                _isWorkbenchMenuOpen = uiStateEvents.IsWorkbenchMenuVisible;
                _isTabInventoryOpen = uiStateEvents.IsTabInventoryVisible;
                _isEscMenuOpen = uiStateEvents.IsEscMenuVisible;
                _isStorageMenuOpen = IsStorageUiOpen();
            }
            else if (UiStateEventsBinder.UsesRuntimeChannel)
            {
                _isWorkbenchMenuOpen = false;
                _isTabInventoryOpen = false;
                _isEscMenuOpen = false;
                _isStorageMenuOpen = IsStorageUiOpen();
            }
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            return UiStateEventsBinder.ResolveAndBind();
        }

        private IShopEvents ResolveShopEvents()
        {
            return ShopEventsBinder.ResolveAndBind();
        }

        private void SubscribeToUiStateEvents(IUiStateEvents uiStateEvents)
        {
            uiStateEvents.OnWorkbenchMenuVisibilityChanged += HandleWorkbenchMenuVisibilityChanged;
            uiStateEvents.OnTabInventoryVisibilityChanged += HandleTabInventoryVisibilityChanged;
            uiStateEvents.OnEscMenuVisibilityChanged += HandleEscMenuVisibilityChanged;
        }

        private void UnsubscribeFromUiStateEvents(IUiStateEvents uiStateEvents)
        {
            uiStateEvents.OnWorkbenchMenuVisibilityChanged -= HandleWorkbenchMenuVisibilityChanged;
            uiStateEvents.OnTabInventoryVisibilityChanged -= HandleTabInventoryVisibilityChanged;
            uiStateEvents.OnEscMenuVisibilityChanged -= HandleEscMenuVisibilityChanged;
        }

        private void SubscribeToShopEvents(IShopEvents shopEvents)
        {
            shopEvents.OnShopTradeOpened += HandleShopTradeOpened;
            shopEvents.OnShopTradeClosed += HandleShopTradeClosed;
        }

        private void UnsubscribeFromShopEvents(IShopEvents shopEvents)
        {
            shopEvents.OnShopTradeOpened -= HandleShopTradeOpened;
            shopEvents.OnShopTradeClosed -= HandleShopTradeClosed;
        }

        private void SubscribeToRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void UnsubscribeFromRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
        }

        private RuntimeHubChannelBinder<IUiStateEvents> UiStateEventsBinder => _uiStateEventsBinder ??=
            new RuntimeHubChannelBinder<IUiStateEvents>(
                () => RuntimeKernelBootstrapper.UiStateEvents,
                SubscribeToUiStateEvents,
                UnsubscribeFromUiStateEvents);

        private RuntimeHubChannelBinder<IShopEvents> ShopEventsBinder => _shopEventsBinder ??=
            new RuntimeHubChannelBinder<IShopEvents>(
                () => RuntimeKernelBootstrapper.ShopEvents,
                SubscribeToShopEvents,
                UnsubscribeFromShopEvents);

        private static bool IsStorageUiOpen()
        {
            var type = Type.GetType("Reloader.Inventory.StorageUiSession, Reloader.Inventory");
            if (type == null)
            {
                return false;
            }

            var prop = type.GetProperty("IsOpen");
            return prop != null
                   && prop.PropertyType == typeof(bool)
                   && prop.GetValue(null) is bool isOpen
                   && isOpen;
        }

        private sealed class KeyboardCursorEscapeKeySource : IPlayerCursorEscapeKeySource
        {
            public bool WasEscapePressedThisFrame()
            {
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            }
        }
    }
}
