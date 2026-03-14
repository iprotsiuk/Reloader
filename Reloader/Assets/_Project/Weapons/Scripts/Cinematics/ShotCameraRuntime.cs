using System.Collections.Generic;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Weapons.Cinematics
{
    [DefaultExecutionOrder(-50)]
    public sealed class ShotCameraRuntime : MonoBehaviour, IShotCameraRuntime
    {
        private const float DefaultFixedDeltaTime = 0.02f;
        private const float FollowDistanceMeters = 2.4f;
        private const float FollowHeightMeters = 0.35f;
        private const float DefaultMissLingerSeconds = 1f;
        private const float DefaultNpcHitLingerSeconds = 2f;
        private const float DefaultOrbitPitchDegrees = 8f;
        private const float OrbitYawSensitivity = 0.35f;
        private const float OrbitPitchSensitivity = 0.25f;
        private const int CinematicCameraPriority = 100;
        private const string CinematicCameraName = "ShotCameraRuntime_Camera";
        private const string FollowTargetName = "ShotCameraRuntime_FollowTarget";
        private const string LookTargetName = "ShotCameraRuntime_LookTarget";
        private const string UiRuntimeRootName = "UiToolkitRuntimeRoot";

        private static readonly Vector2 OrbitPitchClamp = new(-25f, 35f);

        private IShotCameraInputSource _inputSource;
        private IPlayerInputSource _playerInputSource;
        private WeaponProjectile _activeProjectile;
        private ShotCameraSettings _activeSettings;
        private float _baselineFixedDeltaTime = DefaultFixedDeltaTime;
        private bool _isShotActive;
        private bool _hasActiveCinematicCamera;
        private bool _presentationStateApplied;
        private bool _isLingeringAtImpact;
        private float _lingerRemainingSeconds;
        private float _orbitYawDegrees;
        private float _orbitPitchDegrees = DefaultOrbitPitchDegrees;
        private Vector3 _focusPoint;
        private Vector3 _focusDirection = Vector3.forward;
        private Camera _renderCamera;
        private PlayerWeaponController _weaponController;
        private CinemachineCamera _cinematicCamera;
        private Transform _cameraFollowTarget;
        private Transform _cameraLookTarget;
        private readonly List<SuppressedHudState> _suppressedHudScreens = new();

        public static bool IsAnyShotCameraActive => ShotCameraGameplayState.IsActive;
        public bool IsShotActive => _isShotActive;
        public bool HasActiveCinematicCamera => _hasActiveCinematicCamera;

        private void Awake()
        {
            ResolveInputSource();
        }

        private void OnDisable()
        {
            EndShotCamera();
            ShotCameraGameplayState.Reset();
        }

        public void Configure(IShotCameraInputSource inputSource, ShotCameraSettings settings)
        {
            _inputSource = inputSource;
            _playerInputSource = inputSource as IPlayerInputSource;
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

            UpdateOrbitInput();

            if (_isLingeringAtImpact)
            {
                UpdateCinematicCameraTarget();
                ApplyTimeScale(_inputSource != null && _inputSource.ShotCameraSpeedUpHeld
                    ? _activeSettings.SpeedUpTimeScale
                    : _activeSettings.SlowMotionTimeScale);
                _lingerRemainingSeconds -= Time.unscaledDeltaTime;
                if (_lingerRemainingSeconds <= 0f)
                {
                    EndShotCamera();
                }

                return;
            }

            if (_activeProjectile == null)
            {
                EndShotCamera();
                return;
            }

            _focusPoint = _activeProjectile.transform.position;
            _focusDirection = ResolveFocusDirection(_activeProjectile.CurrentDirection);
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

            if (_isShotActive)
            {
                EndShotCamera();
            }

            _baselineFixedDeltaTime = Mathf.Max(0.0001f, Time.fixedDeltaTime);
            _activeProjectile = request.Projectile;
            _activeSettings = request.Settings;
            _focusPoint = request.Projectile.transform.position;
            _focusDirection = ResolveFocusDirection(request.Projectile.CurrentDirection);
            _isLingeringAtImpact = false;
            _lingerRemainingSeconds = 0f;
            _orbitYawDegrees = 0f;
            _orbitPitchDegrees = DefaultOrbitPitchDegrees;

            BindActiveProjectile();
            EnsureCinematicCamera();
            ApplyPresentationState();
            UpdateCinematicCameraTarget();
            _activeProjectile.SetShotCameraPresentationActive(true);
            _isShotActive = true;
            _hasActiveCinematicCamera = _cinematicCamera != null;
            ApplyTimeScale(_activeSettings.SlowMotionTimeScale);
            return true;
        }

        private void ResolveInputSource()
        {
            if (_inputSource != null && _playerInputSource != null)
            {
                return;
            }

            var behaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (_inputSource == null && behaviours[i] is IShotCameraInputSource shotSource)
                {
                    _inputSource = shotSource;
                }

                if (_playerInputSource == null && behaviours[i] is IPlayerInputSource playerSource)
                {
                    _playerInputSource = playerSource;
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

        private void UpdateOrbitInput()
        {
            if (_playerInputSource == null)
            {
                return;
            }

            var lookInput = _playerInputSource.LookInput;
            if (lookInput.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _orbitYawDegrees += lookInput.x * OrbitYawSensitivity;
            _orbitPitchDegrees = Mathf.Clamp(
                _orbitPitchDegrees - (lookInput.y * OrbitPitchSensitivity),
                OrbitPitchClamp.x,
                OrbitPitchClamp.y);
        }

        private void EnsureCinematicCamera()
        {
            _renderCamera = EnsureRenderCameraBrain();

            if (_cameraFollowTarget == null)
            {
                var followTargetGo = new GameObject(FollowTargetName);
                followTargetGo.transform.SetParent(transform, worldPositionStays: false);
                _cameraFollowTarget = followTargetGo.transform;
            }

            if (_cameraLookTarget == null)
            {
                var lookTargetGo = new GameObject(LookTargetName);
                lookTargetGo.transform.SetParent(transform, worldPositionStays: false);
                _cameraLookTarget = lookTargetGo.transform;
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
            _cinematicCamera.LookAt = _cameraLookTarget;
        }

        private Camera EnsureRenderCameraBrain()
        {
            var renderCamera = ResolveRenderCamera();
            if (renderCamera == null)
            {
                return null;
            }

            var brain = renderCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = renderCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            return renderCamera;
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
            EnsureCinematicCamera();
            if (_cameraFollowTarget == null || _cameraLookTarget == null)
            {
                return;
            }

            var forward = ResolveFocusDirection(_focusDirection);
            var basisRotation = Quaternion.LookRotation(forward, Vector3.up);
            var orbitRotation = basisRotation * Quaternion.Euler(_orbitPitchDegrees, _orbitYawDegrees, 0f);
            var offset = orbitRotation * new Vector3(0f, FollowHeightMeters, -FollowDistanceMeters);
            var cameraPosition = _focusPoint + offset;
            var cameraRotation = Quaternion.LookRotation(_focusPoint - cameraPosition, Vector3.up);

            _cameraFollowTarget.SetPositionAndRotation(cameraPosition, cameraRotation);
            _cameraLookTarget.SetPositionAndRotation(_focusPoint, Quaternion.LookRotation(forward, Vector3.up));
            if (_cinematicCamera != null)
            {
                _cinematicCamera.Follow = _cameraFollowTarget;
                _cinematicCamera.LookAt = _cameraLookTarget;
            }
        }

        private void BeginImpactLinger(ProjectileLifecycleEndInfo lifecycleEndInfo)
        {
            _isLingeringAtImpact = true;
            _focusPoint = lifecycleEndInfo.TerminalPoint;
            _focusDirection = ResolveFocusDirection(_focusDirection);
            _lingerRemainingSeconds = lifecycleEndInfo.DidHit && lifecycleEndInfo.HitNpc
                ? DefaultNpcHitLingerSeconds
                : DefaultMissLingerSeconds;
            UpdateCinematicCameraTarget();
        }

        private void EndShotCamera()
        {
            if (_activeProjectile != null)
            {
                UnbindActiveProjectile();
                _activeProjectile.SetShotCameraPresentationActive(false);
            }

            RestorePresentationState();
            DestroyCinematicCamera();
            _activeProjectile = null;
            _isLingeringAtImpact = false;
            _lingerRemainingSeconds = 0f;
            _isShotActive = false;
            _hasActiveCinematicCamera = false;
            _focusDirection = Vector3.forward;
            _focusPoint = Vector3.zero;
            _orbitYawDegrees = 0f;
            _orbitPitchDegrees = DefaultOrbitPitchDegrees;
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

        private void ApplyPresentationState()
        {
            if (_presentationStateApplied)
            {
                return;
            }

            ShotCameraGameplayState.PushActive();
            _presentationStateApplied = true;
            _weaponController ??= GetComponent<PlayerWeaponController>();
            _weaponController?.SetShotCameraPresentationSuppressed(true);
            SuppressHudScreens();
        }

        private void RestorePresentationState()
        {
            if (!_presentationStateApplied)
            {
                return;
            }

            _weaponController?.SetShotCameraPresentationSuppressed(false);

            for (var i = 0; i < _suppressedHudScreens.Count; i++)
            {
                var state = _suppressedHudScreens[i];
                if (state.Screen == null)
                {
                    continue;
                }

                state.Screen.SetActive(state.WasActive);
                if (state.Document != null)
                {
                    state.Document.enabled = state.WasDocumentEnabled;
                }
            }

            _suppressedHudScreens.Clear();
            ShotCameraGameplayState.PopActive();
            _presentationStateApplied = false;
        }

        private void SuppressHudScreens()
        {
            _suppressedHudScreens.Clear();
            var runtimeRoot = GameObject.Find(UiRuntimeRootName);
            if (runtimeRoot == null)
            {
                return;
            }

            for (var i = 0; i < runtimeRoot.transform.childCount; i++)
            {
                var screen = runtimeRoot.transform.GetChild(i).gameObject;
                if (screen == null)
                {
                    continue;
                }

                var document = ResolveDocumentBehaviour(screen);
                _suppressedHudScreens.Add(new SuppressedHudState(screen, screen.activeSelf, document, document != null && document.enabled));
                if (document != null)
                {
                    document.enabled = false;
                }

                screen.SetActive(false);
            }
        }

        private static Behaviour ResolveDocumentBehaviour(GameObject screen)
        {
            if (screen == null)
            {
                return null;
            }

            var behaviours = screen.GetComponents<Behaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null && behaviours[i].GetType().Name == "UIDocument")
                {
                    return behaviours[i];
                }
            }

            return null;
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

            if (_cameraLookTarget != null)
            {
                _cameraLookTarget.gameObject.SetActive(false);
                Destroy(_cameraLookTarget.gameObject);
                _cameraLookTarget = null;
            }
        }

        private void ApplyTimeScale(float nextScale)
        {
            var clampedScale = Mathf.Clamp(nextScale, 0.01f, 1f);
            Time.timeScale = clampedScale;
            var baseline = _baselineFixedDeltaTime > 0f ? _baselineFixedDeltaTime : DefaultFixedDeltaTime;
            Time.fixedDeltaTime = baseline * clampedScale;
        }

        private void HandleActiveProjectileLifecycleEnded(WeaponProjectile projectile, ProjectileLifecycleEndInfo lifecycleEndInfo)
        {
            if (projectile == null || projectile != _activeProjectile)
            {
                return;
            }

            _focusDirection = ResolveFocusDirection(projectile.CurrentDirection);
            _activeProjectile.SetShotCameraPresentationActive(false);
            UnbindActiveProjectile();
            _activeProjectile = null;
            BeginImpactLinger(lifecycleEndInfo);
        }

        private static Vector3 ResolveFocusDirection(Vector3 forward)
        {
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
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

        private readonly struct SuppressedHudState
        {
            public SuppressedHudState(GameObject screen, bool wasActive, Behaviour document, bool wasDocumentEnabled)
            {
                Screen = screen;
                WasActive = wasActive;
                Document = document;
                WasDocumentEnabled = wasDocumentEnabled;
            }

            public GameObject Screen { get; }
            public bool WasActive { get; }
            public Behaviour Document { get; }
            public bool WasDocumentEnabled { get; }
        }
    }
}
