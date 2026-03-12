using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Reloader.Core.Runtime;

namespace Reloader.Player
{
    public sealed class PlayerInputReader : MonoBehaviour, IPlayerInputSource
    {
        [Header("Actions")]
        [SerializeField] private InputActionAsset _actionsAsset;
        [SerializeField] private string _mapName = "Player";
        [SerializeField] private string _moveActionName = "Move";
        [SerializeField] private string _lookActionName = "Look";
        [SerializeField] private string _sprintActionName = "Sprint";
        [SerializeField] private string _aimActionName = "Aim";
        [SerializeField] private string _jumpActionName = "Jump";
        [SerializeField] private string _fireActionName = "Attack";
        [SerializeField] private string _reloadActionName = "Reload";
        [SerializeField] private string _pickupActionName = "Pickup";
        [SerializeField] private string _menuToggleActionName = "MenuToggle";
        [SerializeField] private string _beltSlot1ActionName = "BeltSlot1";
        [SerializeField] private string _beltSlot2ActionName = "BeltSlot2";
        [SerializeField] private string _beltSlot3ActionName = "BeltSlot3";
        [SerializeField] private string _beltSlot4ActionName = "BeltSlot4";
        [SerializeField] private string _beltSlot5ActionName = "BeltSlot5";

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool AimHeld { get; private set; }
        public bool JumpQueued { get; private set; }
        public bool AimToggleQueued { get; private set; }
        public bool FireQueued { get; private set; }
        public bool ReloadQueued { get; private set; }
        public bool PickupQueued { get; private set; }
        public bool MenuToggleQueued { get; private set; }
        public bool DevConsoleToggleQueued { get; private set; }

        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _aimAction;
        private InputAction _jumpAction;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private InputAction _pickupAction;
        private InputAction _menuToggleAction;
        private readonly InputAction[] _beltSlotActions = new InputAction[5];
        private int _beltSlotQueued = -1;
        private float _zoomQueued;
        private int _zeroAdjustQueued;
        private bool _autocompleteQueued;
        private int _suggestionDeltaQueued;
        private bool _missingAssetWarningLogged;

        private void Awake()
        {
            ResolveActions();
        }

        private void OnEnable()
        {
            ResolveActions();
            _playerMap?.Enable();
        }

        private void OnDisable()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            SprintHeld = false;
            AimHeld = false;
            JumpQueued = false;
            AimToggleQueued = false;
            FireQueued = false;
            ReloadQueued = false;
            PickupQueued = false;
            MenuToggleQueued = false;
            DevConsoleToggleQueued = false;
            _beltSlotQueued = -1;
            _zoomQueued = 0f;
            _zeroAdjustQueued = 0;
            _autocompleteQueued = false;
            _suggestionDeltaQueued = 0;
        }

        private void Update()
        {
            if (_playerMap != null && !_playerMap.enabled)
            {
                _playerMap.Enable();
            }

            if (_moveAction != null)
            {
                MoveInput = _moveAction.ReadValue<Vector2>();
            }

            if (_lookAction != null)
            {
                LookInput = _lookAction.ReadValue<Vector2>();
            }

            SprintHeld = _sprintAction != null && _sprintAction.IsPressed();

            if (_aimAction != null && _aimAction.WasPressedThisFrame())
            {
                AimHeld = !AimHeld;
                AimToggleQueued = true;
            }

            if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
            {
                JumpQueued = true;
            }

            if (_fireAction != null && _fireAction.WasPressedThisFrame())
            {
                FireQueued = true;
            }

            if (_reloadAction != null && _reloadAction.WasPressedThisFrame())
            {
                ReloadQueued = true;
            }

            if (_pickupAction != null && _pickupAction.WasPressedThisFrame())
            {
                PickupQueued = true;
            }

            var keyboard = Keyboard.current;
            var isDevConsoleVisible = RuntimeKernelBootstrapper.UiStateEvents?.IsDevConsoleVisible == true;
            if (keyboard != null && keyboard.backquoteKey.wasPressedThisFrame)
            {
                DevConsoleToggleQueued = true;
            }

            if (!isDevConsoleVisible)
            {
                _autocompleteQueued = false;
                _suggestionDeltaQueued = 0;
            }

            if (isDevConsoleVisible && keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                _autocompleteQueued = true;
            }

            if (isDevConsoleVisible && keyboard != null && keyboard.upArrowKey.wasPressedThisFrame)
            {
                _suggestionDeltaQueued -= 1;
            }

            if (isDevConsoleVisible && keyboard != null && keyboard.downArrowKey.wasPressedThisFrame)
            {
                _suggestionDeltaQueued += 1;
            }

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                // Escape is handled directly by open UI/controllers as a close-only action.
            }
            else if (_menuToggleAction != null && _menuToggleAction.WasPressedThisFrame())
            {
                MenuToggleQueued = true;
            }
            else if (!isDevConsoleVisible && keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                MenuToggleQueued = true;
            }

            var scrollY = 0f;
            var pointer = Pointer.current;
            if (pointer != null)
            {
                var pointerScrollControl = pointer.TryGetChildControl<Vector2Control>("scroll");
                if (pointerScrollControl != null)
                {
                    var pointerScrollY = pointerScrollControl.ReadValue().y;
                    if (Mathf.Abs(pointerScrollY) > 0.0001f)
                    {
                        scrollY += pointerScrollY;
                    }
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && !ReferenceEquals(mouse, pointer))
            {
                var mouseScrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(mouseScrollY) > 0.0001f)
                {
                    scrollY += mouseScrollY;
                }
            }

            if (Mathf.Abs(scrollY) > 0.0001f)
            {
                _zoomQueued += scrollY;
            }

            var plusPressed = false;
            var minusPressed = false;
            if (keyboard != null)
            {
                plusPressed = keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame;
                minusPressed = keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame;
            }

            if (plusPressed)
            {
                _zeroAdjustQueued += 1;
            }

            if (minusPressed)
            {
                _zeroAdjustQueued -= 1;
            }

            for (var i = 0; i < _beltSlotActions.Length; i++)
            {
                if (_beltSlotActions[i] != null && _beltSlotActions[i].WasPressedThisFrame())
                {
                    _beltSlotQueued = i;
                }
            }
        }

        public bool ConsumeJumpPressed()
        {
            if (!JumpQueued)
            {
                return false;
            }

            JumpQueued = false;
            return true;
        }

        public bool ConsumeAimTogglePressed()
        {
            if (!AimToggleQueued)
            {
                return false;
            }

            AimToggleQueued = false;
            return true;
        }

        public bool ConsumePickupPressed()
        {
            if (!PickupQueued)
            {
                return false;
            }

            PickupQueued = false;
            return true;
        }

        public bool ConsumeFirePressed()
        {
            if (!FireQueued)
            {
                return false;
            }

            FireQueued = false;
            return true;
        }

        public bool ConsumeReloadPressed()
        {
            if (!ReloadQueued)
            {
                return false;
            }

            ReloadQueued = false;
            return true;
        }

        public bool ConsumeDevConsoleTogglePressed()
        {
            if (!DevConsoleToggleQueued)
            {
                return false;
            }

            DevConsoleToggleQueued = false;
            return true;
        }

        public bool ConsumeAutocompletePressed()
        {
            if (RuntimeKernelBootstrapper.UiStateEvents?.IsDevConsoleVisible != true)
            {
                _autocompleteQueued = false;
                return false;
            }

            if (!_autocompleteQueued)
            {
                return false;
            }

            _autocompleteQueued = false;
            return true;
        }

        public int ConsumeSuggestionDelta()
        {
            if (RuntimeKernelBootstrapper.UiStateEvents?.IsDevConsoleVisible != true)
            {
                _suggestionDeltaQueued = 0;
                return 0;
            }

            if (_suggestionDeltaQueued == 0)
            {
                return 0;
            }

            var delta = _suggestionDeltaQueued;
            _suggestionDeltaQueued = 0;
            return delta;
        }

        public float ConsumeZoomInput()
        {
            if (Mathf.Approximately(_zoomQueued, 0f))
            {
                return 0f;
            }

            var queued = _zoomQueued;
            _zoomQueued = 0f;
            return queued;
        }

        public int ConsumeZeroAdjustStep()
        {
            if (_zeroAdjustQueued == 0)
            {
                return 0;
            }

            var queued = _zeroAdjustQueued;
            _zeroAdjustQueued = 0;
            return queued;
        }

        public int ConsumeBeltSelectPressed()
        {
            if (_beltSlotQueued < 0 || _beltSlotQueued >= _beltSlotActions.Length)
            {
                return -1;
            }

            var queued = _beltSlotQueued;
            _beltSlotQueued = -1;
            return queued;
        }

        public bool ConsumeMenuTogglePressed()
        {
            if (!MenuToggleQueued)
            {
                return false;
            }

            MenuToggleQueued = false;
            return true;
        }

        public void SetActionsAsset(InputActionAsset actionsAsset)
        {
            _actionsAsset = actionsAsset;
            ResolveActions();
        }

        public void EnsureActionMapEnabled()
        {
            ResolveActions();
            _actionsAsset?.Enable();
            _playerMap?.Enable();
        }

        private void ResolveActions()
        {
            if (_actionsAsset == null)
            {
                if (!_missingAssetWarningLogged)
                {
                    Debug.LogWarning("PlayerInputReader has no InputActionAsset assigned. Movement/look input is disabled until one is set.", this);
                    _missingAssetWarningLogged = true;
                }

                _playerMap = null;
                _moveAction = null;
                _lookAction = null;
                _sprintAction = null;
                _aimAction = null;
                _jumpAction = null;
                _fireAction = null;
                _reloadAction = null;
                _pickupAction = null;
                _menuToggleAction = null;
                for (var i = 0; i < _beltSlotActions.Length; i++)
                {
                    _beltSlotActions[i] = null;
                }
                return;
            }

            _missingAssetWarningLogged = false;
            _playerMap = _actionsAsset != null ? _actionsAsset.FindActionMap(_mapName, false) : null;
            _moveAction = _playerMap != null ? _playerMap.FindAction(_moveActionName, false) : null;
            _lookAction = _playerMap != null ? _playerMap.FindAction(_lookActionName, false) : null;
            _sprintAction = _playerMap != null ? _playerMap.FindAction(_sprintActionName, false) : null;
            _aimAction = _playerMap != null ? _playerMap.FindAction(_aimActionName, false) : null;
            _jumpAction = _playerMap != null ? _playerMap.FindAction(_jumpActionName, false) : null;
            _fireAction = _playerMap != null ? _playerMap.FindAction(_fireActionName, false) : null;
            _reloadAction = _playerMap != null ? _playerMap.FindAction(_reloadActionName, false) : null;
            _pickupAction = _playerMap != null ? _playerMap.FindAction(_pickupActionName, false) : null;
            _menuToggleAction = _playerMap != null ? _playerMap.FindAction(_menuToggleActionName, false) : null;
            _beltSlotActions[0] = _playerMap != null ? _playerMap.FindAction(_beltSlot1ActionName, false) : null;
            _beltSlotActions[1] = _playerMap != null ? _playerMap.FindAction(_beltSlot2ActionName, false) : null;
            _beltSlotActions[2] = _playerMap != null ? _playerMap.FindAction(_beltSlot3ActionName, false) : null;
            _beltSlotActions[3] = _playerMap != null ? _playerMap.FindAction(_beltSlot4ActionName, false) : null;
            _beltSlotActions[4] = _playerMap != null ? _playerMap.FindAction(_beltSlot5ActionName, false) : null;

            if (isActiveAndEnabled)
            {
                _playerMap?.Enable();
            }
        }

    }
}
