using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Player
{
    public sealed class PlayerCursorLockController : MonoBehaviour
    {
        [SerializeField] private bool _lockOnStart;
        [SerializeField] private bool _lockOnClick = true;
        [SerializeField] private bool _unlockOnEscape = true;
        [SerializeField] private bool _unlockOnApplicationFocusLost = true;

        public bool IsCursorLockRequested { get; private set; }

        private void Start()
        {
            if (_lockOnStart)
            {
                LockCursor();
            }
        }

        private void Update()
        {
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
            IsCursorLockRequested = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            IsCursorLockRequested = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
