using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Reloader.Player
{
    public sealed class PlayerCameraDefaults : MonoBehaviour
    {
        private const string ViewmodelCameraName = "ViewmodelCamera";
        [SerializeField] private bool _applyOnAwake = true;
        [SerializeField] private bool _enableVSync = true;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraFollowTarget;
        [SerializeField] private Transform _cameraLookTarget;
        [SerializeField] private Transform _viewmodelCameraParent;
        [SerializeField] private Camera _viewmodelCamera;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _playerArmsRoot;
        [SerializeField] private Animator _playerArmsAnimator;
        [SerializeField] private Transform _weaponPresentationRoot;
        [SerializeField] private float _nearClipPlane = 0.001f;
        [SerializeField] private float _farClipPlane = 2828f;

        private void Awake()
        {
            if (_applyOnAwake)
            {
                ApplyDefaults();
            }
        }

        private void LateUpdate()
        {
            SyncViewmodelCameraLens();
        }

        [ContextMenu("Apply Camera Defaults")]
        public void ApplyDefaults()
        {
            var desiredNearClipPlane = GetDesiredNearClipPlane();
            var desiredFarClipPlane = GetDesiredFarClipPlane(desiredNearClipPlane);

            if (_enableVSync)
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }

            if (_brain != null)
            {
                _brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
                _brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            }

            if (_cinemachineCamera != null)
            {
                var lens = _cinemachineCamera.Lens;
                lens.NearClipPlane = desiredNearClipPlane;
                lens.FarClipPlane = desiredFarClipPlane;
                _cinemachineCamera.Lens = lens;
            }

            if (_cinemachineCamera != null && _cameraFollowTarget != null)
            {
                var lookTarget = _cameraLookTarget != null ? _cameraLookTarget : _cameraFollowTarget;
                _cinemachineCamera.Follow = _cameraFollowTarget;
                _cinemachineCamera.LookAt = lookTarget;
            }

            if (_mainCamera != null)
            {
                _mainCamera.nearClipPlane = desiredNearClipPlane;
                _mainCamera.farClipPlane = desiredFarClipPlane;
                var cameraData = _mainCamera.GetUniversalAdditionalCameraData();
                cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
                _viewmodelCamera = ConfigureViewmodelCamera(_mainCamera, cameraData);
                SyncViewmodelCameraLens();
            }
        }

        public bool TryGetMainCamera(out Camera mainCamera)
        {
            if (_mainCamera == null || !IsUsableHierarchyReference(_mainCamera.transform))
            {
                mainCamera = null;
                return false;
            }

            mainCamera = _mainCamera;
            return true;
        }

        public bool TryGetAuthoredMainCamera(out Camera mainCamera)
        {
            if (_mainCamera == null || !IsUsableHierarchyReference(_mainCamera.transform))
            {
                mainCamera = null;
                return false;
            }

            mainCamera = _mainCamera;
            return true;
        }

        public bool TryGetPresentationCamera(out Camera presentationCamera)
        {
            presentationCamera = ShotCameraGameplayState.PresentationCamera;
            return presentationCamera != null;
        }

        public bool TryGetCameraLookTarget(out Transform cameraLookTarget)
        {
            cameraLookTarget = ResolveCameraLookTarget();
            return cameraLookTarget != null;
        }

        public bool TryGetCameraPivot(out Transform cameraPivot)
        {
            cameraPivot = ResolveCameraPivot();
            return cameraPivot != null;
        }

        public bool TryGetPlayerArmsRoot(out Transform playerArmsRoot)
        {
            playerArmsRoot = ResolvePlayerArmsRoot();
            return playerArmsRoot != null;
        }

        public bool TryGetPlayerArmsAnimator(out Animator playerArmsAnimator)
        {
            playerArmsAnimator = ResolvePlayerArmsAnimator();
            return playerArmsAnimator != null;
        }

        public bool TryGetWeaponPresentationRoot(out Transform weaponPresentationRoot)
        {
            weaponPresentationRoot = ResolveWeaponPresentationRoot();
            return weaponPresentationRoot != null;
        }

        public bool TryGetEffectiveFieldOfView(out float fieldOfView)
        {
            if (_cinemachineCamera != null)
            {
                fieldOfView = _cinemachineCamera.Lens.FieldOfView;
                return true;
            }

            if (_mainCamera != null)
            {
                fieldOfView = _mainCamera.fieldOfView;
                return true;
            }

            fieldOfView = default;
            return false;
        }

        public bool TrySetEffectiveFieldOfView(float fieldOfView)
        {
            if (_cinemachineCamera != null)
            {
                var lens = _cinemachineCamera.Lens;
                lens.FieldOfView = fieldOfView;
                _cinemachineCamera.Lens = lens;
                SyncViewmodelCameraLens();
                return true;
            }

            if (_mainCamera != null)
            {
                _mainCamera.fieldOfView = fieldOfView;
                SyncViewmodelCameraLens();
                return true;
            }

            return false;
        }

        public bool TryGetViewmodelCamera(out Camera viewmodelCamera)
        {
            if (!IsUsableHierarchyReference(_viewmodelCamera?.transform))
            {
                viewmodelCamera = null;
                return false;
            }

            var viewmodelParent = ResolveViewmodelCameraParent();
            if (viewmodelParent == null || _viewmodelCamera.transform.parent != viewmodelParent)
            {
                viewmodelCamera = null;
                return false;
            }

            viewmodelCamera = _viewmodelCamera;
            return true;
        }

        public void RestoreGameplayView()
        {
            if (_cameraFollowTarget == null || !TryGetMainCamera(out var mainCamera))
            {
                return;
            }

            var lookTarget = _cameraLookTarget != null ? _cameraLookTarget : _cameraFollowTarget;
            var lookDirection = lookTarget.position - _cameraFollowTarget.position;
            var rotation = lookDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : _cameraFollowTarget.rotation;

            mainCamera.transform.SetPositionAndRotation(_cameraFollowTarget.position, rotation);
        }

        private void SyncViewmodelCameraLens()
        {
            if (_mainCamera == null)
            {
                return;
            }

            var viewmodelParent = ResolveViewmodelCameraParent();
            if (viewmodelParent == null)
            {
                return;
            }

            if (!TryGetEffectiveFieldOfView(out var fieldOfView))
            {
                return;
            }

            if (!IsUsableHierarchyReference(_viewmodelCamera?.transform) || _viewmodelCamera.transform.parent != viewmodelParent)
            {
                return;
            }

            var desiredNearClipPlane = GetDesiredNearClipPlane();
            _viewmodelCamera.nearClipPlane = desiredNearClipPlane;
            _viewmodelCamera.farClipPlane = Mathf.Max(desiredNearClipPlane + 0.01f, 10f);
            _viewmodelCamera.depth = _mainCamera.depth + 1f;
            _viewmodelCamera.fieldOfView = fieldOfView;
            _viewmodelCamera.allowHDR = _mainCamera.allowHDR;
            _viewmodelCamera.allowMSAA = _mainCamera.allowMSAA;
        }

        private Camera ConfigureViewmodelCamera(Camera mainCamera, UniversalAdditionalCameraData mainCameraData)
        {
            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            if (mainCamera == null || viewmodelLayer < 0)
            {
                return null;
            }

            var viewmodelParent = ResolveViewmodelCameraParent();
            if (!IsUsableHierarchyReference(_viewmodelCamera?.transform) || viewmodelParent == null || _viewmodelCamera.transform.parent != viewmodelParent)
            {
                return null;
            }

            var viewmodelMask = 1 << viewmodelLayer;
            _viewmodelCamera.transform.localPosition = Vector3.zero;
            _viewmodelCamera.transform.localRotation = Quaternion.identity;
            _viewmodelCamera.transform.localScale = Vector3.one;
            _viewmodelCamera.clearFlags = CameraClearFlags.Depth;
            _viewmodelCamera.cullingMask = viewmodelMask;
            _viewmodelCamera.nearClipPlane = mainCamera.nearClipPlane;
            _viewmodelCamera.farClipPlane = Mathf.Max(_viewmodelCamera.nearClipPlane + 0.01f, 10f);
            _viewmodelCamera.depth = mainCamera.depth + 1f;
            _viewmodelCamera.fieldOfView = mainCamera.fieldOfView;
            _viewmodelCamera.allowHDR = mainCamera.allowHDR;
            _viewmodelCamera.allowMSAA = mainCamera.allowMSAA;

            var audioListener = _viewmodelCamera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                Object.DestroyImmediate(audioListener);
            }

            mainCamera.cullingMask &= ~viewmodelMask;

            var viewmodelCameraData = _viewmodelCamera.GetUniversalAdditionalCameraData();
            mainCameraData.renderType = CameraRenderType.Base;
            viewmodelCameraData.renderType = CameraRenderType.Overlay;

            if (!mainCameraData.cameraStack.Contains(_viewmodelCamera))
            {
                mainCameraData.cameraStack.Add(_viewmodelCamera);
            }

            return _viewmodelCamera;
        }

        private Transform ResolveViewmodelCameraParent()
        {
            if (IsUsableHierarchyReference(_viewmodelCameraParent))
            {
                return _viewmodelCameraParent;
            }

            return null;
        }

        private Transform ResolveCameraLookTarget()
        {
            if (IsUsableHierarchyReference(_cameraLookTarget))
            {
                return _cameraLookTarget;
            }

            return null;
        }

        private Transform ResolveCameraPivot()
        {
            if (IsUsableHierarchyReference(_cameraPivot))
            {
                return _cameraPivot;
            }

            return null;
        }

        private Transform ResolvePlayerArmsRoot()
        {
            if (IsUsableHierarchyReference(_playerArmsRoot))
            {
                return _playerArmsRoot;
            }

            return null;
        }

        private Animator ResolvePlayerArmsAnimator()
        {
            if (_playerArmsAnimator == null || !IsUsableHierarchyReference(_playerArmsAnimator.transform))
            {
                return null;
            }

            return _playerArmsAnimator;
        }

        private Transform ResolveWeaponPresentationRoot()
        {
            var cameraPivot = ResolveCameraPivot();
            if (!IsUsableHierarchyReference(_weaponPresentationRoot) || _weaponPresentationRoot.parent != cameraPivot)
            {
                return null;
            }

            return _weaponPresentationRoot;
        }

        private float GetDesiredNearClipPlane()
        {
            return Mathf.Clamp(_nearClipPlane, 0.001f, 1f);
        }

        private float GetDesiredFarClipPlane(float desiredNearClipPlane)
        {
            return Mathf.Max(desiredNearClipPlane + 0.01f, _farClipPlane);
        }

        private bool IsUsableHierarchyReference(Transform candidate)
        {
            return candidate != null && (candidate == transform || candidate.IsChildOf(transform));
        }

    }
}
