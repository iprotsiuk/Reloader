using UnityEngine;
using UnityEngine.InputSystem;
using Reloader.Core.Runtime;

namespace Reloader.Player
{
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
        private IUiStateEvents _uiStateEvents;
        private IUiStateEvents _subscribedUiStateEvents;
        private IShopEvents _shopEvents;
        private IShopEvents _subscribedShopEvents;
        private bool _useRuntimeKernelUiStateEvents = true;
        private bool _useRuntimeKernelShopEvents = true;

        public static bool IsAnyMenuOpen { get; private set; }
        public bool IsCursorLockRequested { get; private set; }

        private void Start()
        {
            if (_lockOnStart)
            {
                LockCursor();
            }
        }

        private void OnEnable()
        {
            SubscribeToShopEvents(ResolveShopEvents());
            SubscribeToUiStateEvents(ResolveUiStateEvents());
            ApplyCursorState();
        }

        private void OnDisable()
        {
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

        private void Update()
        {
            if (IsAnyMenuOpen)
            {
                ApplyCursorState();
                return;
            }

            if (_lockOnClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }

            if (_unlockOnEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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

        private void ApplyCursorState()
        {
            IsAnyMenuOpen = _isTradeMenuOpen || _isWorkbenchMenuOpen || _isTabInventoryOpen;
            if (IsAnyMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Cursor.lockState = _isCursorLockRequested ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_isCursorLockRequested;
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
        }

        private void UnsubscribeFromUiStateEvents()
        {
            if (_subscribedUiStateEvents == null)
            {
                return;
            }

            _subscribedUiStateEvents.OnWorkbenchMenuVisibilityChanged -= HandleWorkbenchMenuVisibilityChanged;
            _subscribedUiStateEvents.OnTabInventoryVisibilityChanged -= HandleTabInventoryVisibilityChanged;
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
    }
}
