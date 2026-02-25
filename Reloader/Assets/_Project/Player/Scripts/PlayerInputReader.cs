using UnityEngine;
using UnityEngine.InputSystem;

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
        public bool FireQueued { get; private set; }
        public bool ReloadQueued { get; private set; }
        public bool PickupQueued { get; private set; }

        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _aimAction;
        private InputAction _jumpAction;
        private InputAction _fireAction;
        private InputAction _reloadAction;
        private InputAction _pickupAction;
        private readonly InputAction[] _beltSlotActions = new InputAction[5];
        private int _beltSlotQueued = -1;
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
            _playerMap?.Disable();
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            SprintHeld = false;
            AimHeld = false;
            JumpQueued = false;
            FireQueued = false;
            ReloadQueued = false;
            PickupQueued = false;
            _beltSlotQueued = -1;
        }

        private void Update()
        {
            if (_moveAction != null)
            {
                MoveInput = _moveAction.ReadValue<Vector2>();
            }

            if (_lookAction != null)
            {
                LookInput = _lookAction.ReadValue<Vector2>();
            }

            SprintHeld = _sprintAction != null && _sprintAction.IsPressed();
            AimHeld = _aimAction != null && _aimAction.IsPressed();

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

        public void SetActionsAsset(InputActionAsset actionsAsset)
        {
            _actionsAsset = actionsAsset;
            ResolveActions();
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
            _beltSlotActions[0] = _playerMap != null ? _playerMap.FindAction(_beltSlot1ActionName, false) : null;
            _beltSlotActions[1] = _playerMap != null ? _playerMap.FindAction(_beltSlot2ActionName, false) : null;
            _beltSlotActions[2] = _playerMap != null ? _playerMap.FindAction(_beltSlot3ActionName, false) : null;
            _beltSlotActions[3] = _playerMap != null ? _playerMap.FindAction(_beltSlot4ActionName, false) : null;
            _beltSlotActions[4] = _playerMap != null ? _playerMap.FindAction(_beltSlot5ActionName, false) : null;
        }
    }
}
