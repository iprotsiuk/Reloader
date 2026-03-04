using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class RenderTextureScopeController : MonoBehaviour
    {
        [SerializeField] private Camera _scopeCamera;
        [SerializeField] private Behaviour[] _expensiveScopeBehaviours;
        private float _defaultScopeCameraFov = 20f;

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
            ApplyState(isActive, optic);
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
