using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Weapons.Cinematics
{
    [DefaultExecutionOrder(-50)]
    public sealed class ShotCameraRuntime : MonoBehaviour, IShotCameraRuntime
    {
        private const float DefaultFixedDeltaTime = 0.02f;
        private const float FollowDistanceMeters = 2f;
        private const float FollowHeightMeters = 0.35f;
        private const int CinematicCameraPriority = 100;
        private const string CinematicCameraName = "ShotCameraRuntime_Camera";
        private const string FollowTargetName = "ShotCameraRuntime_FollowTarget";

        private IShotCameraInputSource _inputSource;
        private WeaponProjectile _activeProjectile;
        private ShotCameraSettings _activeSettings;
        private float _baselineFixedDeltaTime = DefaultFixedDeltaTime;
        private bool _isShotActive;
        private bool _hasActiveCinematicCamera;
        private CinemachineCamera _cinematicCamera;
        private Transform _cameraFollowTarget;

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

            if (_activeProjectile == null)
            {
                EndShotCamera();
                return;
            }

            UpdateCinematicCameraTarget();
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
            if (!_isShotActive)
            {
                _baselineFixedDeltaTime = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            }
            else if (_activeProjectile != null && _activeProjectile != request.Projectile)
            {
                UnbindActiveProjectile();
                _activeProjectile.SetShotCameraPresentationActive(false);
            }

            _activeProjectile = request.Projectile;
            BindActiveProjectile();
            _activeSettings = request.Settings;
            _isShotActive = true;
            EnsureCinematicCamera();
            UpdateCinematicCameraTarget();
            _hasActiveCinematicCamera = _cinematicCamera != null;
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

        private void EnsureCinematicCamera()
        {
            EnsureRenderCameraBrain();

            if (_cameraFollowTarget == null)
            {
                var followTargetGo = new GameObject(FollowTargetName);
                followTargetGo.transform.SetParent(transform, worldPositionStays: false);
                _cameraFollowTarget = followTargetGo.transform;
            }

            if (_cinematicCamera == null)
            {
                var cameraGo = new GameObject(CinematicCameraName);
                cameraGo.transform.SetParent(transform, worldPositionStays: false);
                _cinematicCamera = cameraGo.AddComponent<CinemachineCamera>();
                _cinematicCamera.Priority = CinematicCameraPriority;
                EnsurePipelineComponents(_cinematicCamera);

                var lens = _cinematicCamera.Lens;
                lens.FieldOfView = 30f;
                _cinematicCamera.Lens = lens;
            }
            else
            {
                _cinematicCamera.gameObject.SetActive(true);
                _cinematicCamera.Priority = CinematicCameraPriority;
            }

            _cinematicCamera.Follow = _cameraFollowTarget;
            _cinematicCamera.LookAt = _activeProjectile != null ? _activeProjectile.transform : null;
        }

        private void EnsureRenderCameraBrain()
        {
            var renderCamera = ResolveRenderCamera();
            if (renderCamera == null)
            {
                return;
            }

            var brain = renderCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = renderCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }

        private Camera ResolveRenderCamera()
        {
            var cameraDefaults = GetComponent<PlayerCameraDefaults>();
            if (cameraDefaults != null && cameraDefaults.TryGetMainCamera(out var configuredCamera))
            {
                return configuredCamera;
            }

            return Camera.main;
        }

        private void UpdateCinematicCameraTarget()
        {
            if (_activeProjectile == null)
            {
                return;
            }

            EnsureCinematicCamera();

            var projectileTransform = _activeProjectile.transform;
            var forward = projectileTransform.forward;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var projectilePosition = projectileTransform.position;
            _cameraFollowTarget.SetPositionAndRotation(
                projectilePosition - (forward * FollowDistanceMeters) + (Vector3.up * FollowHeightMeters),
                Quaternion.LookRotation(forward, Vector3.up));
            _cinematicCamera.Follow = _cameraFollowTarget;
            _cinematicCamera.LookAt = projectileTransform;
        }

        private void EndShotCamera()
        {
            if (_activeProjectile != null)
            {
                UnbindActiveProjectile();
                _activeProjectile.SetShotCameraPresentationActive(false);
            }

            DestroyCinematicCamera();
            _activeProjectile = null;
            _isShotActive = false;
            _hasActiveCinematicCamera = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baselineFixedDeltaTime > 0f ? _baselineFixedDeltaTime : DefaultFixedDeltaTime;
        }

        private void BindActiveProjectile()
        {
            if (_activeProjectile == null)
            {
                return;
            }

            _activeProjectile.LifecycleEnded -= HandleActiveProjectileLifecycleEnded;
            _activeProjectile.LifecycleEnded += HandleActiveProjectileLifecycleEnded;
        }

        private void UnbindActiveProjectile()
        {
            if (_activeProjectile == null)
            {
                return;
            }

            _activeProjectile.LifecycleEnded -= HandleActiveProjectileLifecycleEnded;
        }

        private void DestroyCinematicCamera()
        {
            if (_cinematicCamera != null)
            {
                _cinematicCamera.gameObject.SetActive(false);
                Destroy(_cinematicCamera.gameObject);
                _cinematicCamera = null;
            }

            if (_cameraFollowTarget != null)
            {
                _cameraFollowTarget.gameObject.SetActive(false);
                Destroy(_cameraFollowTarget.gameObject);
                _cameraFollowTarget = null;
            }
        }

        private void ApplyTimeScale(float nextScale)
        {
            var clampedScale = Mathf.Clamp(nextScale, 0.01f, 1f);
            Time.timeScale = clampedScale;
            var baseline = _baselineFixedDeltaTime > 0f ? _baselineFixedDeltaTime : DefaultFixedDeltaTime;
            Time.fixedDeltaTime = baseline * clampedScale;
        }

        private void HandleActiveProjectileLifecycleEnded(WeaponProjectile projectile, bool _)
        {
            if (projectile == null || projectile != _activeProjectile)
            {
                return;
            }

            EndShotCamera();
        }

        private static void EnsurePipelineComponents(CinemachineCamera cinemachineCamera)
        {
            var body = cinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Body);
            if (body == null)
            {
                cinemachineCamera.gameObject.AddComponent<CinemachineHardLockToTarget>();
            }

            var aim = cinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Aim);
            if (aim == null)
            {
                cinemachineCamera.gameObject.AddComponent<CinemachineHardLookAt>();
            }
        }
    }
}
