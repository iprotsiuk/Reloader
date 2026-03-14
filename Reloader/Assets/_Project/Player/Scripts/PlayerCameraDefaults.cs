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

        private void Awake()
        {
            if (_applyOnAwake)
            {
                ApplyDefaults();
            }
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

                var viewmodelCamera = _mainCamera.transform.Find("ViewmodelCamera")?.GetComponent<Camera>();
                if (viewmodelCamera != null)
                {
                    viewmodelCamera.nearClipPlane = _mainCamera.nearClipPlane;
                    viewmodelCamera.farClipPlane = Mathf.Max(viewmodelCamera.nearClipPlane + 0.01f, 10f);
                }
            }
        }

        public bool TryGetMainCamera(out Camera mainCamera)
        {
            mainCamera = _mainCamera != null ? _mainCamera : Camera.main;
            return mainCamera != null;
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
                return true;
            }

            if (_mainCamera != null)
            {
                _mainCamera.fieldOfView = fieldOfView;
                return true;
            }

            return false;
        }

        public void RestoreGameplayView()
        {
            if (_mainCamera == null || _cameraFollowTarget == null)
            {
                return;
            }

            var lookTarget = _cameraLookTarget != null ? _cameraLookTarget : _cameraFollowTarget;
            var lookDirection = lookTarget.position - _cameraFollowTarget.position;
            var rotation = lookDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : _cameraFollowTarget.rotation;

            _mainCamera.transform.SetPositionAndRotation(_cameraFollowTarget.position, rotation);
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
