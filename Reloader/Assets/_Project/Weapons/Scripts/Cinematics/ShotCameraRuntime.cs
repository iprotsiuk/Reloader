using System;
using System.Collections.Generic;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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
        private const float CinematicFieldOfView = 30f;
        private const float OrbitYawSensitivity = 0.35f;
        private const float OrbitPitchSensitivity = 0.25f;
        private const string CinematicCameraName = "ShotCameraRuntime_Camera";
        private const string FollowTargetName = "ShotCameraRuntime_FollowTarget";
        private const string LookTargetName = "ShotCameraRuntime_LookTarget";
        private const string UiRuntimeRootName = "UiToolkitRuntimeRoot";

        private static readonly Vector2 OrbitPitchClamp = new(-25f, 35f);
        private static readonly HashSet<string> SuppressedHudScreenIds = new(StringComparer.Ordinal)
        {
            "belt-hud",
            "compass-hud",
            "ammo-hud",
            "interaction-hint"
        };

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
        private Camera _gameplayRenderCamera;
        private PlayerWeaponController _weaponController;
        private Camera _cinematicRenderCamera;
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
            _hasActiveCinematicCamera = _cinematicRenderCamera != null;
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
            _gameplayRenderCamera = ResolveRenderCamera();

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

            if (_cinematicRenderCamera == null)
            {
                var renderCameraGo = new GameObject(CinematicCameraName);
                renderCameraGo.transform.SetParent(transform, worldPositionStays: false);
                _cinematicRenderCamera = renderCameraGo.AddComponent<Camera>();
            }

            ConfigureCinematicRenderCamera();
        }

        private void ConfigureCinematicRenderCamera()
        {
            if (_cinematicRenderCamera == null)
            {
                return;
            }

            if (_gameplayRenderCamera != null)
            {
                _cinematicRenderCamera.CopyFrom(_gameplayRenderCamera);
                _cinematicRenderCamera.cullingMask = ExcludeViewmodelLayer(_gameplayRenderCamera.cullingMask);
                _cinematicRenderCamera.depth = Mathf.Max(_gameplayRenderCamera.depth + 10f, _gameplayRenderCamera.depth + 1f);
                _cinematicRenderCamera.rect = _gameplayRenderCamera.rect;
                _cinematicRenderCamera.targetDisplay = _gameplayRenderCamera.targetDisplay;
                _cinematicRenderCamera.allowHDR = _gameplayRenderCamera.allowHDR;
                _cinematicRenderCamera.allowMSAA = _gameplayRenderCamera.allowMSAA;
                _cinematicRenderCamera.useOcclusionCulling = _gameplayRenderCamera.useOcclusionCulling;
                _cinematicRenderCamera.depthTextureMode = _gameplayRenderCamera.depthTextureMode;
            }

            _cinematicRenderCamera.targetTexture = null;
            _cinematicRenderCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _cinematicRenderCamera.enabled = true;
            EnsureUniversalRenderPipelineBaseCamera(_cinematicRenderCamera);

            _cinematicRenderCamera.fieldOfView = CinematicFieldOfView;
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
            if (_cinematicRenderCamera != null)
            {
                _cinematicRenderCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                _cinematicRenderCamera.fieldOfView = CinematicFieldOfView;
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
            var shouldRestoreGlobalTime = _isShotActive;

            if (_activeProjectile != null)
            {
                UnbindActiveProjectile();
                _activeProjectile.SetShotCameraPresentationActive(false);
            }

            DestroyCinematicCamera();
            RestorePresentationState();
            _activeProjectile = null;
            _isLingeringAtImpact = false;
            _lingerRemainingSeconds = 0f;
            _isShotActive = false;
            _hasActiveCinematicCamera = false;
            _focusDirection = Vector3.forward;
            _focusPoint = Vector3.zero;
            _orbitYawDegrees = 0f;
            _orbitPitchDegrees = DefaultOrbitPitchDegrees;
            if (shouldRestoreGlobalTime)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = _baselineFixedDeltaTime > 0f ? _baselineFixedDeltaTime : DefaultFixedDeltaTime;
            }
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
            ShotCameraGameplayState.SetPresentationCamera(_cinematicRenderCamera);
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

                if (state.Root != null)
                {
                    state.Root.visible = state.WasRootVisible;
                    state.Root.pickingMode = state.WasPickingMode;
                }
                else
                {
                    state.Screen.SetActive(state.WasActive);
                    if (state.Document != null)
                    {
                        state.Document.enabled = state.WasDocumentEnabled;
                    }
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

                if (!SuppressedHudScreenIds.Contains(screen.name))
                {
                    continue;
                }

                var document = ResolveDocumentBehaviour(screen);
                var root = ResolveDocumentRoot(document);
                if (root != null)
                {
                    _suppressedHudScreens.Add(new SuppressedHudState(
                        screen,
                        screen.activeSelf,
                        document,
                        document != null && document.enabled,
                        root,
                        root.visible,
                        root.pickingMode));
                    root.visible = false;
                    root.pickingMode = PickingMode.Ignore;
                    continue;
                }

                _suppressedHudScreens.Add(new SuppressedHudState(screen, screen.activeSelf, document, document != null && document.enabled, null, false, PickingMode.Position));
                if (document != null)
                {
                    document.enabled = false;
                }
                screen.SetActive(false);
            }
        }

        private static UIDocument ResolveDocumentBehaviour(GameObject screen)
        {
            if (screen == null)
            {
                return null;
            }

            return screen.GetComponent<UIDocument>();
        }

        private static VisualElement ResolveDocumentRoot(UIDocument document)
        {
            return document != null && document.enabled ? document.rootVisualElement : null;
        }

        private void DestroyCinematicCamera()
        {
            if (_cinematicRenderCamera != null)
            {
                ShotCameraGameplayState.ClearPresentationCamera(_cinematicRenderCamera);
                _cinematicRenderCamera.gameObject.SetActive(false);
                Destroy(_cinematicRenderCamera.gameObject);
                _cinematicRenderCamera = null;
            }

            _gameplayRenderCamera = null;

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

        private static int ExcludeViewmodelLayer(int cullingMask)
        {
            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            if (viewmodelLayer < 0)
            {
                return cullingMask;
            }

            return cullingMask & ~(1 << viewmodelLayer);
        }

        private static void EnsureUniversalRenderPipelineBaseCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            var additionalCameraDataType = ResolveTypeByName("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (additionalCameraDataType == null)
            {
                return;
            }

            var additionalCameraData = camera.GetComponent(additionalCameraDataType)
                ?? camera.gameObject.AddComponent(additionalCameraDataType);
            var renderTypeProperty = additionalCameraDataType.GetProperty("renderType");
            if (renderTypeProperty?.CanWrite == true)
            {
                renderTypeProperty.SetValue(additionalCameraData, Enum.Parse(renderTypeProperty.PropertyType, "Base"));
            }

            var cameraStackProperty = additionalCameraDataType.GetProperty("cameraStack");
            var stack = cameraStackProperty?.GetValue(additionalCameraData);
            var clearMethod = stack?.GetType().GetMethod("Clear");
            clearMethod?.Invoke(stack, null);
        }

        private static Type ResolveTypeByName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            var resolvedType = Type.GetType(fullName, throwOnError: false);
            if (resolvedType != null)
            {
                return resolvedType;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                resolvedType = assemblies[i].GetType(fullName, throwOnError: false);
                if (resolvedType != null)
                {
                    return resolvedType;
                }
            }

            return null;
        }

        private readonly struct SuppressedHudState
        {
            public SuppressedHudState(
                GameObject screen,
                bool wasActive,
                UIDocument document,
                bool wasDocumentEnabled,
                VisualElement root,
                bool wasRootVisible,
                PickingMode wasPickingMode)
            {
                Screen = screen;
                WasActive = wasActive;
                Document = document;
                WasDocumentEnabled = wasDocumentEnabled;
                Root = root;
                WasRootVisible = wasRootVisible;
                WasPickingMode = wasPickingMode;
            }

            public GameObject Screen { get; }
            public bool WasActive { get; }
            public UIDocument Document { get; }
            public bool WasDocumentEnabled { get; }
            public VisualElement Root { get; }
            public bool WasRootVisible { get; }
            public PickingMode WasPickingMode { get; }
        }
    }
}
