using UnityEngine;

namespace Reloader.DevTools.Runtime
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class DevTraceSegmentView : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private float _hideAt = -1f;
        private bool _isVisible;

        public bool IsVisible => _isVisible;
        public Vector3 StartPoint => _lineRenderer != null ? _lineRenderer.GetPosition(0) : Vector3.zero;
        public Vector3 EndPoint => _lineRenderer != null ? _lineRenderer.GetPosition(1) : Vector3.zero;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            ConfigureDefaults();
            Hide();
        }

        private void Update()
        {
            if (_isVisible && Time.unscaledTime >= _hideAt)
            {
                Hide();
            }
        }

        public void Show(Vector3 startPoint, Vector3 endPoint, Color color, float lifetimeSeconds)
        {
            EnsureInitialized();
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, startPoint);
            _lineRenderer.SetPosition(1, endPoint);
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
            _lineRenderer.enabled = true;
            _hideAt = Time.unscaledTime + Mathf.Max(0.01f, lifetimeSeconds);
            _isVisible = true;
        }

        public void Hide()
        {
            EnsureInitialized();
            _lineRenderer.enabled = false;
            _hideAt = -1f;
            _isVisible = false;
        }

        private void EnsureInitialized()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
                ConfigureDefaults();
            }
        }

        private void ConfigureDefaults()
        {
            if (_lineRenderer == null)
            {
                return;
            }

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = 0.03f;
            _lineRenderer.endWidth = 0.03f;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            if (_lineRenderer.sharedMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _lineRenderer.sharedMaterial = new Material(shader);
                }
            }
        }
    }
}
