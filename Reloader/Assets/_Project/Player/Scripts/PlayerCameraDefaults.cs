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

            var viewmodelParent = ResolveViewmodelCameraParent();
            if (viewmodelParent == null)
            {
                return;
            }

            _viewmodelCamera ??= ResolveViewmodelCamera(viewmodelParent, false);
            if (_viewmodelCamera == null || !TryGetEffectiveFieldOfView(out var fieldOfView))
            {
                return;
            }

            if (_viewmodelCamera.transform.parent != viewmodelParent)
            {
                _viewmodelCamera.transform.SetParent(viewmodelParent, false);
            }

            _viewmodelCamera.nearClipPlane = _mainCamera.nearClipPlane;
            _viewmodelCamera.farClipPlane = Mathf.Max(_viewmodelCamera.nearClipPlane + 0.01f, 10f);
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
            var viewmodelCamera = ResolveViewmodelCamera(viewmodelParent, true);
            if (viewmodelCamera == null)
            {
                return null;
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

        private Transform ResolveViewmodelCameraParent()
        {
            if (_cameraFollowTarget != null)
            {
                return _cameraFollowTarget;
            }

            if (_mainCamera != null && _mainCamera.transform.parent != null)
            {
                return _mainCamera.transform.parent;
            }

            return _mainCamera != null ? _mainCamera.transform : null;
        }

        private Camera ResolveViewmodelCamera(Transform viewmodelParent, bool createIfMissing)
        {
            if (_mainCamera == null || viewmodelParent == null)
            {
                return null;
            }

            var legacyCamera = _mainCamera.transform != viewmodelParent
                ? _mainCamera.transform.Find(ViewmodelCameraName)?.GetComponent<Camera>()
                : null;

            if (_viewmodelCamera != null)
            {
                if (_viewmodelCamera.transform.parent != viewmodelParent)
                {
                    _viewmodelCamera.transform.SetParent(viewmodelParent, false);
                }

                if (legacyCamera != null && legacyCamera != _viewmodelCamera)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(legacyCamera.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(legacyCamera.gameObject);
                    }
                }

                return _viewmodelCamera;
            }

            var viewmodelCamera = viewmodelParent.Find(ViewmodelCameraName)?.GetComponent<Camera>();

            if (viewmodelCamera == null && legacyCamera != null)
            {
                legacyCamera.transform.SetParent(viewmodelParent, false);
                viewmodelCamera = legacyCamera;
            }
            else if (viewmodelCamera != null && legacyCamera != null && legacyCamera != viewmodelCamera)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(legacyCamera.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(legacyCamera.gameObject);
                }
            }

            if (viewmodelCamera == null && createIfMissing)
            {
                var viewmodelCameraGo = new GameObject(ViewmodelCameraName);
                viewmodelCameraGo.transform.SetParent(viewmodelParent, false);
                viewmodelCamera = viewmodelCameraGo.AddComponent<Camera>();
            }

            _viewmodelCamera = viewmodelCamera;
            return _viewmodelCamera;
        }
    }
}
