using UnityEngine;
using UnityEngine.InputSystem;
using Reloader.Core.Events;

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
            GameEvents.OnShopTradeOpened += HandleShopTradeOpened;
            GameEvents.OnShopTradeClosed += HandleShopTradeClosed;
            GameEvents.OnWorkbenchMenuVisibilityChanged += HandleWorkbenchMenuVisibilityChanged;
            GameEvents.OnTabInventoryVisibilityChanged += HandleTabInventoryVisibilityChanged;
            ApplyCursorState();
        }

        private void OnDisable()
        {
            GameEvents.OnShopTradeOpened -= HandleShopTradeOpened;
            GameEvents.OnShopTradeClosed -= HandleShopTradeClosed;
            GameEvents.OnWorkbenchMenuVisibilityChanged -= HandleWorkbenchMenuVisibilityChanged;
            GameEvents.OnTabInventoryVisibilityChanged -= HandleTabInventoryVisibilityChanged;
            IsAnyMenuOpen = false;
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
    }
}
