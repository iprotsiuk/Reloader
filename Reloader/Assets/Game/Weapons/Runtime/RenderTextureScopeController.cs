using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class RenderTextureScopeController : MonoBehaviour
    {
        [SerializeField] private Camera _scopeCamera;
        [SerializeField] private Behaviour[] _expensiveScopeBehaviours;
        private float _defaultScopeCameraFov = 20f;
        private bool _lastIsActive;
        private float _lastAppliedFov = -1f;
        private bool _initialized;

        private void Awake()
        {
            if (_scopeCamera != null)
            {
                _defaultScopeCameraFov = _scopeCamera.fieldOfView;
            }

            ApplyState(false, null);
        }

        public void SetScopeActive(bool isActive, OpticDefinition optic)
        {
            var requestedFov = _defaultScopeCameraFov;
            if (isActive && optic != null && optic.HasScopeRenderProfile)
            {
                requestedFov = optic.RenderProfile.ScopeCameraFov;
            }

            if (_initialized && _lastIsActive == isActive && Mathf.Approximately(_lastAppliedFov, requestedFov))
            {
                return;
            }

            ApplyState(isActive, optic);
            _lastIsActive = isActive;
            _lastAppliedFov = requestedFov;
            _initialized = true;
        }

        private void ApplyState(bool isActive, OpticDefinition optic)
        {
            if (_scopeCamera != null)
            {
                _scopeCamera.enabled = isActive;

                if (isActive && optic != null && optic.HasScopeRenderProfile)
                {
                    _scopeCamera.fieldOfView = optic.RenderProfile.ScopeCameraFov;
                }
                else
                {
                    _scopeCamera.fieldOfView = _defaultScopeCameraFov;
                }
            }

            if (_expensiveScopeBehaviours != null)
            {
                for (var i = 0; i < _expensiveScopeBehaviours.Length; i++)
                {
                    if (_expensiveScopeBehaviours[i] != null)
                    {
                        _expensiveScopeBehaviours[i].enabled = isActive;
                    }
                }
            }
        }
    }
}
