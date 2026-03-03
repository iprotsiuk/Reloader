using System;
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
        private IUiStateEvents _uiStateEvents;
        private IUiStateEvents _subscribedUiStateEvents;
        private IShopEvents _shopEvents;
        private IShopEvents _subscribedShopEvents;
        private bool _useRuntimeKernelUiStateEvents = true;
        private bool _useRuntimeKernelShopEvents = true;
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
            SubscribeToShopEvents(ResolveShopEvents());
            SubscribeToUiStateEvents(ResolveUiStateEvents());
            ReconcileMenuFlagsFromCurrentEventState();
            ApplyCursorState();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromShopEvents();
            UnsubscribeFromUiStateEvents();
            IsAnyMenuOpen = false;
        }

        public void Configure(IUiStateEvents uiStateEvents = null, IShopEvents shopEvents = null)
        {
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
            _useRuntimeKernelShopEvents = shopEvents == null;
            _shopEvents = shopEvents;

            if (isActiveAndEnabled)
            {
                SubscribeToShopEvents(ResolveShopEvents());
                SubscribeToUiStateEvents(ResolveUiStateEvents());
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

            if (_useRuntimeKernelShopEvents)
            {
                SubscribeToShopEvents(ResolveShopEvents());
            }

            if (_useRuntimeKernelUiStateEvents)
            {
                SubscribeToUiStateEvents(ResolveUiStateEvents());
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
            var uiStateEvents = _uiStateEvents;
            var shopUiStateEvents = _shopEvents as IUiStateEvents;
            if (shopUiStateEvents != null)
            {
                _isTradeMenuOpen = shopUiStateEvents.IsShopTradeMenuOpen;
            }
            else if (uiStateEvents != null)
            {
                _isTradeMenuOpen = uiStateEvents.IsShopTradeMenuOpen;
            }
            else if (_useRuntimeKernelShopEvents || _useRuntimeKernelUiStateEvents)
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
            else if (_useRuntimeKernelUiStateEvents)
            {
                _isWorkbenchMenuOpen = false;
                _isTabInventoryOpen = false;
                _isEscMenuOpen = false;
                _isStorageMenuOpen = IsStorageUiOpen();
            }
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                var runtimeUiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
                if (!ReferenceEquals(_uiStateEvents, runtimeUiStateEvents))
                {
                    _uiStateEvents = runtimeUiStateEvents;
                    SubscribeToUiStateEvents(_uiStateEvents);
                }
                else if (!ReferenceEquals(_subscribedUiStateEvents, _uiStateEvents))
                {
                    SubscribeToUiStateEvents(_uiStateEvents);
                }
            }
            else if (!ReferenceEquals(_subscribedUiStateEvents, _uiStateEvents))
            {
                SubscribeToUiStateEvents(_uiStateEvents);
            }

            return _uiStateEvents;
        }

        private IShopEvents ResolveShopEvents()
        {
            if (_useRuntimeKernelShopEvents)
            {
                var runtimeShopEvents = RuntimeKernelBootstrapper.ShopEvents;
                if (!ReferenceEquals(_shopEvents, runtimeShopEvents))
                {
                    _shopEvents = runtimeShopEvents;
                    SubscribeToShopEvents(_shopEvents);
                }
                else if (!ReferenceEquals(_subscribedShopEvents, _shopEvents))
                {
                    SubscribeToShopEvents(_shopEvents);
                }
            }
            else if (!ReferenceEquals(_subscribedShopEvents, _shopEvents))
            {
                SubscribeToShopEvents(_shopEvents);
            }

            return _shopEvents;
        }

        private void SubscribeToUiStateEvents(IUiStateEvents uiStateEvents)
        {
            if (uiStateEvents == null)
            {
                UnsubscribeFromUiStateEvents();
                return;
            }

            if (ReferenceEquals(_subscribedUiStateEvents, uiStateEvents))
            {
                return;
            }

            UnsubscribeFromUiStateEvents();
            _subscribedUiStateEvents = uiStateEvents;
            _subscribedUiStateEvents.OnWorkbenchMenuVisibilityChanged += HandleWorkbenchMenuVisibilityChanged;
            _subscribedUiStateEvents.OnTabInventoryVisibilityChanged += HandleTabInventoryVisibilityChanged;
            _subscribedUiStateEvents.OnEscMenuVisibilityChanged += HandleEscMenuVisibilityChanged;
        }

        private void UnsubscribeFromUiStateEvents()
        {
            if (_subscribedUiStateEvents == null)
            {
                return;
            }

            _subscribedUiStateEvents.OnWorkbenchMenuVisibilityChanged -= HandleWorkbenchMenuVisibilityChanged;
            _subscribedUiStateEvents.OnTabInventoryVisibilityChanged -= HandleTabInventoryVisibilityChanged;
            _subscribedUiStateEvents.OnEscMenuVisibilityChanged -= HandleEscMenuVisibilityChanged;
            _subscribedUiStateEvents = null;
        }

        private void SubscribeToShopEvents(IShopEvents shopEvents)
        {
            if (shopEvents == null)
            {
                UnsubscribeFromShopEvents();
                return;
            }

            if (ReferenceEquals(_subscribedShopEvents, shopEvents))
            {
                return;
            }

            UnsubscribeFromShopEvents();
            _subscribedShopEvents = shopEvents;
            _subscribedShopEvents.OnShopTradeOpened += HandleShopTradeOpened;
            _subscribedShopEvents.OnShopTradeClosed += HandleShopTradeClosed;
        }

        private void UnsubscribeFromShopEvents()
        {
            if (_subscribedShopEvents == null)
            {
                return;
            }

            _subscribedShopEvents.OnShopTradeOpened -= HandleShopTradeOpened;
            _subscribedShopEvents.OnShopTradeClosed -= HandleShopTradeClosed;
            _subscribedShopEvents = null;
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
