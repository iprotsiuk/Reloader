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
            if (_enableVSync)
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }

            if (_brain != null)
            {
                _brain.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
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
                var cameraData = _mainCamera.GetUniversalAdditionalCameraData();
                cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
            }
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
