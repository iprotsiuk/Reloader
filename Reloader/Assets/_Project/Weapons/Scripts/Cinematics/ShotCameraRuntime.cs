using Reloader.Player;
using Reloader.Weapons.Ballistics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Weapons.Cinematics
{
    [DefaultExecutionOrder(-50)]
    public sealed class ShotCameraRuntime : MonoBehaviour, IShotCameraRuntime
    {
        private const float DefaultFixedDeltaTime = 0.02f;

        private IShotCameraInputSource _inputSource;
        private WeaponProjectile _activeProjectile;
        private ShotCameraSettings _activeSettings;
        private float _baselineFixedDeltaTime = DefaultFixedDeltaTime;
        private bool _isShotActive;
        private bool _hasActiveCinematicCamera;

        public bool IsShotActive => _isShotActive;
        public bool HasActiveCinematicCamera => _hasActiveCinematicCamera;

        private void Awake()
        {
            ResolveInputSource();
        }

        private void OnDisable()
        {
            EndShotCamera();
        }

        public void Configure(IShotCameraInputSource inputSource, ShotCameraSettings settings)
        {
            _inputSource = inputSource;
            _activeSettings = settings;
        }

        private void Update()
        {
            if (!_isShotActive)
            {
                return;
            }

            ResolveInputSource();

            if (ShouldCancelShotCamera())
            {
                PlayerCursorLockController.MarkEscapeConsumedThisFrame();
                EndShotCamera();
                return;
            }

            ApplyTimeScale(_inputSource != null && _inputSource.ShotCameraSpeedUpHeld
                ? _activeSettings.SpeedUpTimeScale
                : _activeSettings.SlowMotionTimeScale);
        }

        public bool TryRegisterShot(ShotCameraRequest request)
        {
            if (request.Projectile == null)
            {
                return false;
            }

            ResolveInputSource();
            _activeProjectile = request.Projectile;
            _activeSettings = request.Settings;
            _baselineFixedDeltaTime = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            _isShotActive = true;
            _hasActiveCinematicCamera = true;
            _activeProjectile.SetShotCameraPresentationActive(true);
            ApplyTimeScale(_activeSettings.SlowMotionTimeScale);
            return true;
        }

        private void ResolveInputSource()
        {
            if (_inputSource != null)
            {
                return;
            }

            var behaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IShotCameraInputSource source)
                {
                    _inputSource = source;
                    return;
                }
            }
        }

        private bool ShouldCancelShotCamera()
        {
            if (_inputSource != null && _inputSource.ConsumeShotCameraCancelPressed())
            {
                return true;
            }

            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        private void EndShotCamera()
        {
            if (_activeProjectile != null)
            {
                _activeProjectile.SetShotCameraPresentationActive(false);
            }

            _activeProjectile = null;
            _isShotActive = false;
            _hasActiveCinematicCamera = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baselineFixedDeltaTime > 0f ? _baselineFixedDeltaTime : DefaultFixedDeltaTime;
        }

        private void ApplyTimeScale(float nextScale)
        {
            var clampedScale = Mathf.Clamp(nextScale, 0.01f, 1f);
            Time.timeScale = clampedScale;
            var baseline = _baselineFixedDeltaTime > 0f ? _baselineFixedDeltaTime : DefaultFixedDeltaTime;
            Time.fixedDeltaTime = baseline * clampedScale;
        }
    }
}
