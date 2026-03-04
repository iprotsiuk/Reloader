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

        public void Tick(float deltaTime)
        {
            ResolveReferences();
            if (_characterController == null || _inputSource == null || deltaTime <= 0f)
            {
                return;
            }

            var grounded = _characterController.isGrounded;
            if (grounded && VerticalVelocity < 0f)
            {
                VerticalVelocity = _settings.GroundedSnapVelocity;
            }

            var jumpPressed = _inputSource.ConsumeJumpPressed();
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

            var moveInput = Vector2.ClampMagnitude(_inputSource.MoveInput, 1f);
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

        private void ResolveReferences()
        {
            _characterController ??= GetComponent<CharacterController>();
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
        }
    }
}
