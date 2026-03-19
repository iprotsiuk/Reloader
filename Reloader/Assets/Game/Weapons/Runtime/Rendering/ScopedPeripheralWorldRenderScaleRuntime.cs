using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Reloader.Game.Weapons.Rendering
{
    internal static class ScopedPeripheralWorldRenderScaleRuntime
    {
        private const float MinScopedWorldRenderScale = 0.5f;

        private static UniversalRenderPipelineAsset s_boundAsset;
        private static float s_originalRenderScale = 1f;
        private static UpscalingFilterSelection s_originalUpscalingFilter = UpscalingFilterSelection.Auto;
        private static bool s_hasCapturedOriginalState;

        public static void Apply(bool isScopedPipActive, float normalizedPeripheralBlur)
        {
            var asset = ResolvePipelineAsset();
            if (asset == null)
            {
                return;
            }

            ApplyToAsset(asset, isScopedPipActive, normalizedPeripheralBlur);
        }

        private static void ApplyToAsset(UniversalRenderPipelineAsset asset, bool isScopedPipActive, float normalizedPeripheralBlur)
        {
            if (asset == null)
            {
                return;
            }

            if (!isScopedPipActive || normalizedPeripheralBlur <= 0.001f)
            {
                Reset();
                return;
            }

            CaptureOriginalStateIfNeeded(asset);
            asset.renderScale = ResolveScopedWorldRenderScale(normalizedPeripheralBlur);
            asset.upscalingFilter = UpscalingFilterSelection.Linear;
        }

        public static void Reset()
        {
            if (s_boundAsset == null || !s_hasCapturedOriginalState)
            {
                return;
            }

            s_boundAsset.renderScale = s_originalRenderScale;
            s_boundAsset.upscalingFilter = s_originalUpscalingFilter;
            s_boundAsset = null;
            s_hasCapturedOriginalState = false;
        }

        internal static float ResolveScopedWorldRenderScale(float normalizedPeripheralBlur)
        {
            var clampedBlur = Mathf.Clamp01(normalizedPeripheralBlur);
            var response = Mathf.Sqrt(clampedBlur);
            return Mathf.Lerp(1f, MinScopedWorldRenderScale, response);
        }

        private static void CaptureOriginalStateIfNeeded(UniversalRenderPipelineAsset asset)
        {
            if (ReferenceEquals(s_boundAsset, asset) && s_hasCapturedOriginalState)
            {
                return;
            }

            s_boundAsset = asset;
            s_originalRenderScale = asset.renderScale;
            s_originalUpscalingFilter = asset.upscalingFilter;
            s_hasCapturedOriginalState = true;
        }

        private static UniversalRenderPipelineAsset ResolvePipelineAsset()
        {
            return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset
                ?? GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        }
    }
}
