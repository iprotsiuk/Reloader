using UnityEngine;
using Reloader.Game.Weapons.Rendering;

namespace Reloader.Game.Weapons
{
    public interface IPeripheralScopeEffectReceiver
    {
        void SetScopedState(bool isActive, float alpha);
    }

    public sealed class PeripheralScopeScreenMask : MonoBehaviour, IPeripheralScopeEffectReceiver, IPeripheralScopeBlurReceiver, IPeripheralScopeBlurRuntimeStateSource
    {
        [SerializeField, Range(0f, 1f)] private float _centerWidthNormalized = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _centerHeightNormalized = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _maxPeripheralAlpha = 0.82f;
        [SerializeField, Range(0f, 1f)] private float _maxPeripheralBlurAlpha = 0.95f;
        [SerializeField, Range(0f, 1f)] private float _minCenterNormalizedScale = 0.55f;
        [SerializeField, Range(0.001f, 0.25f)] private float _softEdgeNormalized = 0.04f;
        [SerializeField] private Color _maskColor = Color.black;

        private static Texture2D s_fillTexture;
        private bool _isActive;
        private float _alpha;
        private float _peripheralBlurPercent;

        private void Reset()
        {
            enabled = false;
        }

        private void Awake()
        {
            enabled = false;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EnsureFillTexture();
        }

        public void SetScopedState(bool isActive, float alpha)
        {
            _isActive = isActive;
            _alpha = Mathf.Clamp01(alpha);
            enabled = isActive;
        }

        public void SetPeripheralBlur(float blurPercent)
        {
            _peripheralBlurPercent = Mathf.Clamp01(blurPercent);
        }

        public void UpdateBlurRuntimeState(bool isActive, float alpha, float blurPercent)
        {
            var clampedBlur = Mathf.Clamp01(blurPercent);
            var effectiveActive = isActive && alpha > 0.001f && clampedBlur > 0.001f;
            var blurScale = Mathf.Lerp(1f, _minCenterNormalizedScale, clampedBlur);
            var centerWidthNormalized = Mathf.Clamp01(_centerWidthNormalized * blurScale);
            var centerHeightNormalized = Mathf.Clamp01(_centerHeightNormalized * blurScale);

            PeripheralScopeBlurRuntimeState.Update(
                effectiveActive,
                alpha,
                clampedBlur,
                0.5f,
                0.5f,
                centerWidthNormalized,
                centerHeightNormalized,
                Mathf.Clamp(_softEdgeNormalized, 0.001f, 0.25f));
        }

        private void OnGUI()
        {
            if (!_isActive || _alpha <= 0.001f || Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureFillTexture();

            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            var blurScale = Mathf.Lerp(1f, _minCenterNormalizedScale, _peripheralBlurPercent);
            var centerWidthNormalized = Mathf.Clamp01(_centerWidthNormalized * blurScale);
            var centerHeightNormalized = Mathf.Clamp01(_centerHeightNormalized * blurScale);
            var centerWidth = Mathf.Clamp(screenWidth * centerWidthNormalized, 1f, screenWidth);
            var centerHeight = Mathf.Clamp(screenHeight * centerHeightNormalized, 1f, screenHeight);
            var centerX = (screenWidth - centerWidth) * 0.5f;
            var centerY = (screenHeight - centerHeight) * 0.5f;

            var color = _maskColor;
            var blurAlpha = _alpha * Mathf.Lerp(_maxPeripheralAlpha, _maxPeripheralBlurAlpha, _peripheralBlurPercent);
            color.a = Mathf.Clamp01(blurAlpha);

            DrawRect(new Rect(0f, 0f, screenWidth, centerY), color);
            DrawRect(new Rect(0f, centerY + centerHeight, screenWidth, screenHeight - (centerY + centerHeight)), color);
            DrawRect(new Rect(0f, centerY, centerX, centerHeight), color);
            DrawRect(new Rect(centerX + centerWidth, centerY, screenWidth - (centerX + centerWidth), centerHeight), color);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, s_fillTexture);
            GUI.color = previousColor;
        }

        private static void EnsureFillTexture()
        {
            if (s_fillTexture != null)
            {
                return;
            }

            s_fillTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "PeripheralScopeScreenMask_Fill",
                hideFlags = HideFlags.HideAndDontSave
            };
            s_fillTexture.SetPixel(0, 0, Color.white);
            s_fillTexture.Apply(false, true);
        }
    }
}
