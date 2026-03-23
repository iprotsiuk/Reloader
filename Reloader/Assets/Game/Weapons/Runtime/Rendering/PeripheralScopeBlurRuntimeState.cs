using UnityEngine;

namespace Reloader.Game.Weapons.Rendering
{
    public static class PeripheralScopeBlurRuntimeState
    {
        public static bool IsActive { get; private set; }
        public static float BlendAlpha { get; private set; }
        public static float BlurAmount { get; private set; }
        public static float CenterXNormalized { get; private set; } = 0.5f;
        public static float CenterYNormalized { get; private set; } = 0.5f;
        public static float CenterWidthNormalized { get; private set; }
        public static float CenterHeightNormalized { get; private set; }
        public static float SoftEdgeNormalized { get; private set; } = 0.04f;

        public static void UpdateState(
            bool isActive,
            float blendAlpha,
            float blurAmount)
        {
            IsActive = isActive;
            BlendAlpha = Mathf.Clamp01(blendAlpha);
            BlurAmount = Mathf.Clamp01(blurAmount);
        }

        public static void UpdateAperture(
            float centerXNormalized,
            float centerYNormalized,
            float centerWidthNormalized,
            float centerHeightNormalized,
            float softEdgeNormalized)
        {
            CenterXNormalized = Mathf.Clamp01(centerXNormalized);
            CenterYNormalized = Mathf.Clamp01(centerYNormalized);
            CenterWidthNormalized = Mathf.Clamp(centerWidthNormalized, 0f, 1f);
            CenterHeightNormalized = Mathf.Clamp(centerHeightNormalized, 0f, 1f);
            SoftEdgeNormalized = Mathf.Clamp(softEdgeNormalized, 0.001f, 0.25f);
        }

        public static void Update(
            bool isActive,
            float blendAlpha,
            float blurAmount,
            float centerXNormalized,
            float centerYNormalized,
            float centerWidthNormalized,
            float centerHeightNormalized,
            float softEdgeNormalized)
        {
            UpdateState(isActive, blendAlpha, blurAmount);
            UpdateAperture(
                centerXNormalized,
                centerYNormalized,
                centerWidthNormalized,
                centerHeightNormalized,
                softEdgeNormalized);
        }

        public static void ClearAperture()
        {
            UpdateAperture(0.5f, 0.5f, 0f, 0f, 0.04f);
        }

        public static void Reset()
        {
            Update(false, 0f, 0f, 0.5f, 0.5f, 0f, 0f, 0.04f);
        }
    }
}
