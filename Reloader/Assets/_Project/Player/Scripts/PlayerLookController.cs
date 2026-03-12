using UnityEngine;
using Reloader.Core.Runtime;

namespace Reloader.Player
{
    public sealed class PlayerLookController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private Transform _pitchTransform;
        [SerializeField] private Vector2 _lookSensitivity = Vector2.one;
        [SerializeField] private Vector2 _pitchClamp = new Vector2(-85f, 85f);
        [SerializeField] private Vector2 _adsSensitivityMultiplier = new Vector2(0.35f, 0.35f);
        [SerializeField] private Vector2 _runtimeAdsSensitivityMultiplier = Vector2.one;
        [SerializeField] private bool _scaleByDeltaTime;
        [SerializeField] private bool _lookSmoothingEnabled;
        [SerializeField] private float _lookSmoothingSpeed = 20f;
        [SerializeField, Range(0f, 1f)] private float _lookSmoothingStrength = 1f;
        [SerializeField] private Vector2 _focusTargetSmoothingSpeed = new Vector2(10f, 10f);
        [SerializeField] private PlayerCameraDefaults _cameraDefaults;
        [SerializeField] private float _referenceFieldOfView = 60f;
        [SerializeField] private Vector2 _lookFovScaleClamp = new Vector2(0.1f, 2f);

        private IPlayerInputSource _inputSource;
        private IUiStateEvents _uiStateEvents;
        private bool _useRuntimeKernelUiStateEvents = true;
        private float _yaw;
        private float _pitch;
        private Vector2 _smoothedLookInput;
        private bool _hasSmoothedLookInput;
        private Transform _focusTargetOverride;

        public Vector2 LookSensitivity
        {
            get => _lookSensitivity;
            set => _lookSensitivity = value;
        }

        public Vector2 PitchClamp
        {
            get => _pitchClamp;
            set => _pitchClamp = value;
        }

        public Vector2 AdsSensitivityMultiplier
        {
            get => _adsSensitivityMultiplier;
            set => _adsSensitivityMultiplier = value;
        }

        public Vector2 RuntimeAdsSensitivityMultiplier
        {
            get => _runtimeAdsSensitivityMultiplier;
            set => _runtimeAdsSensitivityMultiplier = new Vector2(
                Mathf.Max(0.001f, value.x),
                Mathf.Max(0.001f, value.y));
        }

        public bool LookSmoothingEnabled
        {
            get => _lookSmoothingEnabled;
            set => _lookSmoothingEnabled = value;
        }

        public float LookSmoothingSpeed
        {
            get => _lookSmoothingSpeed;
            set => _lookSmoothingSpeed = Mathf.Max(0f, value);
        }

        public float LookSmoothingStrength
        {
            get => _lookSmoothingStrength;
            set => _lookSmoothingStrength = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            ResolveReferences();
            _yaw = transform.eulerAngles.y;
            _pitch = _pitchTransform != null ? Mathf.DeltaAngle(0f, _pitchTransform.localEulerAngles.x) : 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(IPlayerInputSource inputSource, Transform pitchTransform, IUiStateEvents uiStateEvents = null)
        {
            _inputSource = inputSource;
            _pitchTransform = pitchTransform;
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
            _yaw = transform.eulerAngles.y;
            _pitch = _pitchTransform != null ? Mathf.DeltaAngle(0f, _pitchTransform.localEulerAngles.x) : 0f;
            ResetSmoothingState();
        }

        public void SetInputSource(MonoBehaviour source)
        {
            _inputSourceBehaviour = source;
            _inputSource = source as IPlayerInputSource;
        }

        public void SetPitchTransform(Transform pitchTransform)
        {
            _pitchTransform = pitchTransform;
        }

        public void SetFocusTargetOverride(Transform focusTarget)
        {
            _focusTargetOverride = focusTarget;
        }

        public void Tick(float deltaTime)
        {
            ResolveReferences();
            if (_inputSource == null)
            {
                return;
            }

            if (_focusTargetOverride != null)
            {
                ApplyFocusTargetOverride(deltaTime);
                return;
            }

            if (PlayerCursorLockController.IsAnyMenuOpen || (ResolveUiStateEvents()?.IsAnyMenuOpen ?? false))
            {
                return;
            }

            var lookInput = _inputSource.LookInput;
            lookInput = ApplyLookSmoothing(lookInput, deltaTime);
            var sensitivity = _inputSource.AimHeld
                ? Vector2.Scale(_lookSensitivity, Vector2.Scale(_adsSensitivityMultiplier, _runtimeAdsSensitivityMultiplier))
                : _lookSensitivity;
            sensitivity *= GetFieldOfViewSensitivityScale();
            var scale = _scaleByDeltaTime ? deltaTime : 1f;

            _yaw += lookInput.x * sensitivity.x * scale;
            _pitch -= lookInput.y * sensitivity.y * scale;
            _pitch = Mathf.Clamp(_pitch, _pitchClamp.x, _pitchClamp.y);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (_pitchTransform != null)
            {
                _pitchTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            _cameraDefaults ??= GetComponent<PlayerCameraDefaults>();
        }

        private Vector2 ApplyLookSmoothing(Vector2 lookInput, float deltaTime)
        {
            if (!_lookSmoothingEnabled)
            {
                return lookInput;
            }

            if (!_hasSmoothedLookInput)
            {
                _smoothedLookInput = lookInput;
                _hasSmoothedLookInput = true;
                return lookInput;
            }

            var sampleDeltaTime = deltaTime > 0f ? deltaTime : Time.deltaTime;
            if (sampleDeltaTime <= 0f)
            {
                sampleDeltaTime = 1f / 60f;
            }

            var smoothingAlpha = 1f - Mathf.Exp(-Mathf.Max(0f, _lookSmoothingSpeed) * sampleDeltaTime);
            _smoothedLookInput = Vector2.Lerp(_smoothedLookInput, lookInput, smoothingAlpha);
            return Vector2.Lerp(lookInput, _smoothedLookInput, Mathf.Clamp01(_lookSmoothingStrength));
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                _uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            }

            return _uiStateEvents;
        }

        private void ResetSmoothingState()
        {
            _smoothedLookInput = Vector2.zero;
            _hasSmoothedLookInput = false;
        }

        private void ApplyFocusTargetOverride(float deltaTime)
        {
            var focusTarget = _focusTargetOverride;
            if (focusTarget == null)
            {
                return;
            }

            var origin = _pitchTransform != null ? _pitchTransform.position : transform.position;
            var direction = focusTarget.position - origin;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var planarMagnitude = new Vector2(direction.x, direction.z).magnitude;
            var desiredYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            var desiredPitch = Mathf.Clamp(-Mathf.Atan2(direction.y, planarMagnitude) * Mathf.Rad2Deg, _pitchClamp.x, _pitchClamp.y);
            var yawSmoothing = ResolveFocusTargetSmoothingFactor(_focusTargetSmoothingSpeed.x, deltaTime);
            var pitchSmoothing = ResolveFocusTargetSmoothingFactor(_focusTargetSmoothingSpeed.y, deltaTime);

            _yaw = Mathf.LerpAngle(_yaw, desiredYaw, yawSmoothing);
            _pitch = Mathf.Lerp(_pitch, desiredPitch, pitchSmoothing);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (_pitchTransform != null)
            {
                _pitchTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private static float ResolveFocusTargetSmoothingFactor(float smoothingSpeed, float deltaTime)
        {
            if (smoothingSpeed <= 0f)
            {
                return 1f;
            }

            var sampleDeltaTime = deltaTime > 0f ? deltaTime : Time.deltaTime;
            if (sampleDeltaTime <= 0f)
            {
                sampleDeltaTime = 1f / 60f;
            }

            return 1f - Mathf.Exp(-smoothingSpeed * sampleDeltaTime);
        }

        private float GetFieldOfViewSensitivityScale()
        {
            if (!TryGetEffectiveFieldOfView(out var currentFieldOfView))
            {
                return 1f;
            }

            var clampedCurrentFov = Mathf.Clamp(currentFieldOfView, 1f, 179f);
            var clampedReferenceFov = Mathf.Clamp(_referenceFieldOfView, 1f, 179f);
            var currentHalfFovTangent = Mathf.Tan(clampedCurrentFov * 0.5f * Mathf.Deg2Rad);
            var referenceHalfFovTangent = Mathf.Tan(clampedReferenceFov * 0.5f * Mathf.Deg2Rad);
            if (referenceHalfFovTangent <= Mathf.Epsilon)
            {
                return 1f;
            }

            var scale = currentHalfFovTangent / referenceHalfFovTangent;
            var minScale = Mathf.Min(_lookFovScaleClamp.x, _lookFovScaleClamp.y);
            var maxScale = Mathf.Max(_lookFovScaleClamp.x, _lookFovScaleClamp.y);
            minScale = Mathf.Max(0.01f, minScale);
            maxScale = Mathf.Max(minScale, maxScale);
            return Mathf.Clamp(scale, minScale, maxScale);
        }

        private bool TryGetEffectiveFieldOfView(out float fieldOfView)
        {
            if (_cameraDefaults != null && _cameraDefaults.TryGetEffectiveFieldOfView(out fieldOfView))
            {
                return true;
            }

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                fieldOfView = mainCamera.fieldOfView;
                return true;
            }

            fieldOfView = default;
            return false;
        }
    }
}
