using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Reloader.Player
{
    public sealed class PlayerCameraDefaults : MonoBehaviour
    {
        [SerializeField] private bool _applyOnAwake = true;
        [SerializeField] private bool _enableVSync = true;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private CinemachineBrain _brain;
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraFollowTarget;
        [SerializeField] private Transform _cameraLookTarget;
        [SerializeField] private float _nearClipPlane = 0.001f;
        [SerializeField] private float _farClipPlane = 2828f;
        private Camera _viewmodelCamera;

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
            if (_mainCamera != null && _brain == null)
            {
                _brain = _mainCamera.GetComponent<CinemachineBrain>();
                if (_brain == null)
                {
                    _brain = _mainCamera.gameObject.AddComponent<CinemachineBrain>();
                }
            }

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

            if (_cinemachineCamera != null && _cameraFollowTarget != null)
            {
                var lookTarget = _cameraLookTarget != null ? _cameraLookTarget : _cameraFollowTarget;
                _cinemachineCamera.Follow = _cameraFollowTarget;
                _cinemachineCamera.LookAt = lookTarget;
                EnsurePipelineComponents(_cinemachineCamera);
            }

            if (_mainCamera != null)
            {
                _mainCamera.nearClipPlane = Mathf.Clamp(_nearClipPlane, 0.001f, 1f);
                _mainCamera.farClipPlane = Mathf.Max(_mainCamera.nearClipPlane + 0.01f, _farClipPlane);
                var cameraData = _mainCamera.GetUniversalAdditionalCameraData();
                cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
                _viewmodelCamera = ConfigureViewmodelCamera(_mainCamera, cameraData);
                SyncViewmodelCameraLens();
            }
        }

        public bool TryGetMainCamera(out Camera mainCamera)
        {
            mainCamera = _mainCamera != null ? _mainCamera : Camera.main;
            return mainCamera != null;
        }

        public bool TryGetPresentationCamera(out Camera presentationCamera)
        {
            presentationCamera = ShotCameraGameplayState.PresentationCamera;
            if (presentationCamera != null)
            {
                return true;
            }

            return TryGetMainCamera(out presentationCamera);
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

        private void SyncViewmodelCameraLens()
        {
            if (_mainCamera == null)
            {
                return;
            }

            _viewmodelCamera ??= _mainCamera.transform.Find("ViewmodelCamera")?.GetComponent<Camera>();
            if (_viewmodelCamera == null || !TryGetEffectiveFieldOfView(out var fieldOfView))
            {
                return;
            }

            _viewmodelCamera.nearClipPlane = _mainCamera.nearClipPlane;
            _viewmodelCamera.farClipPlane = Mathf.Max(_viewmodelCamera.nearClipPlane + 0.01f, 10f);
            _viewmodelCamera.depth = _mainCamera.depth + 1f;
            _viewmodelCamera.fieldOfView = fieldOfView;
            _viewmodelCamera.allowHDR = _mainCamera.allowHDR;
            _viewmodelCamera.allowMSAA = _mainCamera.allowMSAA;
        }

        private static Camera ConfigureViewmodelCamera(Camera mainCamera, UniversalAdditionalCameraData mainCameraData)
        {
            var viewmodelLayer = LayerMask.NameToLayer("Viewmodel");
            if (mainCamera == null || viewmodelLayer < 0)
            {
                return null;
            }

            var viewmodelCamera = mainCamera.transform.Find("ViewmodelCamera")?.GetComponent<Camera>();
            if (viewmodelCamera == null)
            {
                var viewmodelCameraGo = new GameObject("ViewmodelCamera");
                viewmodelCameraGo.transform.SetParent(mainCamera.transform, false);
                viewmodelCamera = viewmodelCameraGo.AddComponent<Camera>();
            }

            var viewmodelMask = 1 << viewmodelLayer;
            viewmodelCamera.transform.localPosition = Vector3.zero;
            viewmodelCamera.transform.localRotation = Quaternion.identity;
            viewmodelCamera.transform.localScale = Vector3.one;
            viewmodelCamera.clearFlags = CameraClearFlags.Depth;
            viewmodelCamera.cullingMask = viewmodelMask;
            viewmodelCamera.nearClipPlane = mainCamera.nearClipPlane;
            viewmodelCamera.farClipPlane = Mathf.Max(viewmodelCamera.nearClipPlane + 0.01f, 10f);
            viewmodelCamera.depth = mainCamera.depth + 1f;
            viewmodelCamera.fieldOfView = mainCamera.fieldOfView;
            viewmodelCamera.allowHDR = mainCamera.allowHDR;
            viewmodelCamera.allowMSAA = mainCamera.allowMSAA;

            var audioListener = viewmodelCamera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                Object.DestroyImmediate(audioListener);
            }

            mainCamera.cullingMask &= ~viewmodelMask;

            var viewmodelCameraData = viewmodelCamera.GetUniversalAdditionalCameraData();
            mainCameraData.renderType = CameraRenderType.Base;
            viewmodelCameraData.renderType = CameraRenderType.Overlay;

            if (!mainCameraData.cameraStack.Contains(viewmodelCamera))
            {
                mainCameraData.cameraStack.Add(viewmodelCamera);
            }

            return viewmodelCamera;
        }
    }
}
