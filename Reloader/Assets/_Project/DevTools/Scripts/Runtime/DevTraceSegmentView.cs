using UnityEngine;
using System.Collections.Generic;

namespace Reloader.DevTools.Runtime
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class DevTraceSegmentView : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private float _hideAt = -1f;
        private bool _isVisible;

        public bool IsVisible => _isVisible;
        public int PointCount => _lineRenderer != null ? _lineRenderer.positionCount : 0;
        public Vector3 StartPoint => PointCount > 0 ? _lineRenderer.GetPosition(0) : Vector3.zero;
        public Vector3 EndPoint => PointCount > 0 ? _lineRenderer.GetPosition(PointCount - 1) : Vector3.zero;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            ConfigureDefaults();
            Hide();
        }

        private void Update()
        {
            if (_isVisible && _hideAt >= 0f && Time.unscaledTime >= _hideAt)
            {
                Hide();
            }
        }

        public void Show(Vector3 startPoint, Vector3 endPoint, Color color, float lifetimeSeconds)
        {
            ShowPath(new[] { startPoint, endPoint }, color, lifetimeSeconds);
        }

        public void ShowPath(IReadOnlyList<Vector3> points, Color color, float lifetimeSeconds)
        {
            EnsureInitialized();
            var pointCount = points?.Count ?? 0;
            if (pointCount < 2)
            {
                Hide();
                return;
            }

            _lineRenderer.positionCount = pointCount;
            for (var i = 0; i < pointCount; i++)
            {
                _lineRenderer.SetPosition(i, points[i]);
            }

            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
            _lineRenderer.enabled = true;
            _hideAt = lifetimeSeconds < 0f
                ? -1f
                : Time.unscaledTime + Mathf.Max(0.01f, lifetimeSeconds);
            _isVisible = true;
        }

        public Vector3 GetPoint(int index)
        {
            EnsureInitialized();
            return _lineRenderer.GetPosition(index);
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
