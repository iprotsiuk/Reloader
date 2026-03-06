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
        private RenderTexture _scopeRenderTexture;
        private ScopeLensDisplay _lastLensDisplay;
        private ScopeReticleController _lastReticleController;
        private int _lastResolution = -1;
        private float _lastMagnification = -1f;

        private void Awake()
        {
            if (_scopeCamera != null)
            {
                _defaultScopeCameraFov = _scopeCamera.fieldOfView;
            }

            ApplyState(false, _defaultScopeCameraFov);
        }

        private void OnDestroy()
        {
            ReleaseRenderTexture();
        }

        public void SetScopeActive(bool isActive, OpticDefinition optic, GameObject activeOpticInstance, float referenceFieldOfView, float magnification)
        {
            var requestedFov = ResolveRequestedFov(isActive, optic, referenceFieldOfView, magnification);
            var requestedResolution = ResolveRequestedResolution(optic);
            var lensDisplay = ResolveLensDisplay(activeOpticInstance);
            var reticleController = ResolveReticleController(activeOpticInstance);

            if (_initialized
                && _lastIsActive == isActive
                && Mathf.Approximately(_lastAppliedFov, requestedFov)
                && _lastResolution == requestedResolution
                && Mathf.Approximately(_lastMagnification, magnification)
                && ReferenceEquals(_lastLensDisplay, lensDisplay)
                && ReferenceEquals(_lastReticleController, reticleController))
            {
                return;
            }

            if (isActive)
            {
                EnsureRenderTexture(requestedResolution);
            }
            else
            {
                ReleaseRenderTexture();
            }

            BindLensDisplay(isActive, lensDisplay);
            BindReticle(isActive, reticleController, optic, magnification);
            ApplyState(isActive, requestedFov);
            _lastIsActive = isActive;
            _lastAppliedFov = requestedFov;
            _lastResolution = requestedResolution;
            _lastMagnification = magnification;
            _lastLensDisplay = lensDisplay;
            _lastReticleController = reticleController;
            _initialized = true;
        }

        private void ApplyState(bool isActive, float requestedFov)
        {
            if (_scopeCamera != null)
            {
                _scopeCamera.enabled = isActive;
                _scopeCamera.fieldOfView = isActive ? requestedFov : _defaultScopeCameraFov;
                _scopeCamera.targetTexture = isActive ? _scopeRenderTexture : null;
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

        private float ResolveRequestedFov(bool isActive, OpticDefinition optic, float referenceFieldOfView, float magnification)
        {
            if (!isActive || optic == null)
            {
                return _defaultScopeCameraFov;
            }

            if (optic.HasScopeRenderProfile)
            {
                return optic.RenderProfile.ScopeCameraFov;
            }

            return MagnificationToFieldOfView(referenceFieldOfView, magnification);
        }

        private int ResolveRequestedResolution(OpticDefinition optic)
        {
            if (optic != null && optic.HasScopeRenderProfile)
            {
                return optic.RenderProfile.RenderTextureResolution;
            }

            return 1024;
        }

        private ScopeLensDisplay ResolveLensDisplay(GameObject activeOpticInstance)
        {
            if (activeOpticInstance == null)
            {
                return null;
            }

            return activeOpticInstance.GetComponentInChildren<ScopeLensDisplay>(true);
        }

        private ScopeReticleController ResolveReticleController(GameObject activeOpticInstance)
        {
            if (activeOpticInstance == null)
            {
                return null;
            }

            return activeOpticInstance.GetComponentInChildren<ScopeReticleController>(true);
        }

        private void EnsureRenderTexture(int resolution)
        {
            var safeResolution = Mathf.Clamp(resolution, 128, 4096);
            if (_scopeRenderTexture != null
                && _scopeRenderTexture.width == safeResolution
                && _scopeRenderTexture.height == safeResolution)
            {
                return;
            }

            ReleaseRenderTexture();
            _scopeRenderTexture = new RenderTexture(safeResolution, safeResolution, 24, RenderTextureFormat.ARGB32)
            {
                name = $"ScopeRT_{safeResolution}"
            };
            _scopeRenderTexture.Create();
        }

        private void BindLensDisplay(bool isActive, ScopeLensDisplay lensDisplay)
        {
            if (!isActive)
            {
                if (_lastLensDisplay != null)
                {
                    _lastLensDisplay.TrySetTexture(null);
                }

                return;
            }

            if (lensDisplay == null)
            {
                Debug.LogWarning("RenderTextureScopeController: Active scoped optic is missing a ScopeLensDisplay binding.", this);
                return;
            }

            lensDisplay.TrySetTexture(_scopeRenderTexture);
        }

        private void BindReticle(bool isActive, ScopeReticleController reticleController, OpticDefinition optic, float magnification)
        {
            if (!isActive)
            {
                if (_lastReticleController != null)
                {
                    _lastReticleController.Clear();
                }

                return;
            }

            if (reticleController == null)
            {
                Debug.LogWarning("RenderTextureScopeController: Active scoped optic is missing a ScopeReticleController binding.", this);
                return;
            }

            if (optic == null || optic.ScopeReticleDefinition == null)
            {
                Debug.LogWarning("RenderTextureScopeController: Active scoped optic is missing a ScopeReticleDefinition binding.", this);
            }

            reticleController.ApplyReticle(optic != null ? optic.ScopeReticleDefinition : null, magnification);
        }

        private void ReleaseRenderTexture()
        {
            if (_scopeRenderTexture == null)
            {
                return;
            }

            if (_scopeRenderTexture.IsCreated())
            {
                _scopeRenderTexture.Release();
            }

            Destroy(_scopeRenderTexture);
            _scopeRenderTexture = null;
        }

        private static float MagnificationToFieldOfView(float referenceFieldOfView, float magnification)
        {
            var safeReferenceFov = Mathf.Clamp(referenceFieldOfView, 1f, 179f);
            var safeMagnification = Mathf.Max(1f, magnification);
            var referenceHalfAngle = safeReferenceFov * 0.5f * Mathf.Deg2Rad;
            var zoomedHalfAngle = Mathf.Atan(Mathf.Tan(referenceHalfAngle) / safeMagnification);
            return Mathf.Clamp(zoomedHalfAngle * 2f * Mathf.Rad2Deg, 1f, safeReferenceFov);
        }
    }
}
