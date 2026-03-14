using UnityEngine;

namespace Reloader.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMover : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private PlayerMovementSettings _settings = new PlayerMovementSettings();
        [SerializeField, Range(0.1f, 1f)] private float _adsSpeedMultiplier = 0.7f;
        [Header("Debug (Runtime)")]
        [SerializeField] private bool _isGroundedDebug;
        [SerializeField] private float _jumpBufferTimerDebug;
        [SerializeField] private CollisionFlags _lastCollisionFlags;
        [SerializeField] private bool _jumpPressConsumedDebug;

        private IPlayerInputSource _inputSource;
        private Vector3 _horizontalVelocity;
        private float _jumpBufferTimer;
        private bool _movementLocked;
        private bool _devNoclipEnabled;
        private float _devNoclipSpeed;

        public float VerticalVelocity { get; private set; }
        public bool IsGrounded => _isGroundedDebug;
        public event System.Action<Vector3, Vector3, bool, float> LocomotionFramePublished;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(IPlayerInputSource inputSource, PlayerMovementSettings settings)
        {
            _inputSource = inputSource;
            _settings = settings ?? new PlayerMovementSettings();
            _characterController ??= GetComponent<CharacterController>();
        }

        public void SetInputSource(MonoBehaviour source)
        {
            _inputSourceBehaviour = source;
            _inputSource = source as IPlayerInputSource;
        }

        public void SetCharacterController(CharacterController characterController)
        {
            _characterController = characterController;
        }

        public void SetMovementLocked(bool isLocked)
        {
            _movementLocked = isLocked;
            if (isLocked)
            {
                _horizontalVelocity = Vector3.zero;
            }
        }

        public void SetDevNoclip(bool isEnabled, float speed)
        {
            _devNoclipEnabled = isEnabled;
            _devNoclipSpeed = speed > 0f
                ? Mathf.Max(0.1f, speed)
                : ResolveDefaultDevNoclipSpeed();
            if (!isEnabled)
            {
                return;
            }

            VerticalVelocity = 0f;
            _horizontalVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _isGroundedDebug = false;
            _lastCollisionFlags = CollisionFlags.None;
            _jumpBufferTimerDebug = 0f;
        }

        public void Tick(float deltaTime)
        {
            ResolveReferences();
            if (_characterController == null || _inputSource == null || deltaTime <= 0f)
            {
                return;
            }

            if (ShotCameraGameplayState.IsActive)
            {
                PublishLocomotionFrame(transform.position, Vector3.zero, _characterController.isGrounded, deltaTime);
                return;
            }

            if (_devNoclipEnabled)
            {
                TickDevNoclip(deltaTime);
                return;
            }

            var grounded = _characterController.isGrounded;
            if (grounded && VerticalVelocity < 0f)
            {
                VerticalVelocity = _settings.GroundedSnapVelocity;
            }

            var jumpPressed = !_movementLocked && _inputSource.ConsumeJumpPressed();
            _jumpPressConsumedDebug = jumpPressed;
            if (jumpPressed)
            {
                _jumpBufferTimer = _settings.JumpBufferTime;
            }
            else if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - deltaTime);
            }

            if (grounded && _jumpBufferTimer > 0f)
            {
                VerticalVelocity = Mathf.Sqrt(_settings.JumpHeight * -2f * _settings.Gravity);
                _jumpBufferTimer = 0f;
            }

            VerticalVelocity += _settings.Gravity * deltaTime;

            var moveInput = _movementLocked
                ? Vector2.zero
                : Vector2.ClampMagnitude(_inputSource.MoveInput, 1f);
            var moveDirection = (transform.right * moveInput.x) + (transform.forward * moveInput.y);
            moveDirection.y = 0f;
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            var targetSpeed = _inputSource.SprintHeld ? _settings.SprintSpeed : _settings.WalkSpeed;
            if (_inputSource.AimHeld)
            {
                targetSpeed *= _adsSpeedMultiplier;
            }
            var targetVelocity = moveDirection * targetSpeed;
            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                targetVelocity,
                _settings.Acceleration * deltaTime);

            var displacement = (_horizontalVelocity + (Vector3.up * VerticalVelocity)) * deltaTime;
            var collisionFlags = _characterController.Move(displacement);
            if ((collisionFlags & CollisionFlags.Below) != 0 && VerticalVelocity < 0f)
            {
                VerticalVelocity = _settings.GroundedSnapVelocity;
            }

            _lastCollisionFlags = collisionFlags;
            _isGroundedDebug = _characterController.isGrounded || (collisionFlags & CollisionFlags.Below) != 0;
            _jumpBufferTimerDebug = _jumpBufferTimer;
            PublishLocomotionFrame(transform.position, _horizontalVelocity, _isGroundedDebug, deltaTime);
        }

        public void PublishLocomotionFrame(Vector3 worldPosition, Vector3 horizontalVelocity, bool isGrounded, float deltaTime)
        {
            LocomotionFramePublished?.Invoke(worldPosition, horizontalVelocity, isGrounded, deltaTime);
        }

        private void TickDevNoclip(float deltaTime)
        {
            var moveInput = _movementLocked
                ? Vector2.zero
                : Vector2.ClampMagnitude(_inputSource.MoveInput, 1f);
            var moveBasis = ResolveDevNoclipMoveBasis();
            var moveDirection = (moveBasis.right * moveInput.x) + (moveBasis.forward * moveInput.y);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            var horizontalVelocity = moveDirection.sqrMagnitude > 0.0001f
                ? moveDirection.normalized * _devNoclipSpeed
                : Vector3.zero;

            transform.position += horizontalVelocity * deltaTime;
            VerticalVelocity = 0f;
            _horizontalVelocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _isGroundedDebug = false;
            _lastCollisionFlags = CollisionFlags.None;
            _jumpBufferTimerDebug = 0f;
            _jumpPressConsumedDebug = false;
            PublishLocomotionFrame(transform.position, horizontalVelocity, false, deltaTime);
        }

        private (Vector3 forward, Vector3 right) ResolveDevNoclipMoveBasis()
        {
            var viewTransform = Camera.main != null ? Camera.main.transform : transform;
            return (viewTransform.forward, viewTransform.right);
        }

        private float ResolveDefaultDevNoclipSpeed()
        {
            return Mathf.Max(0.1f, _settings.WalkSpeed * 5f);
        }

        private void ResolveReferences()
        {
            _characterController ??= GetComponent<CharacterController>();
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
        }
    }
}
