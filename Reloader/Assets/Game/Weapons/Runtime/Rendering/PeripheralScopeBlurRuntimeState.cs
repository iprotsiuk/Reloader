using UnityEngine;

namespace Reloader.Game.Weapons.Rendering
{
    public static class PeripheralScopeBlurRuntimeState
    {
        public static bool IsActive { get; private set; }
        public static float BlendAlpha { get; private set; }
        public static float BlurAmount { get; private set; }
        public static float CenterWidthNormalized { get; private set; } = 0.3f;
        public static float CenterHeightNormalized { get; private set; } = 0.3f;
        public static float SoftEdgeNormalized { get; private set; } = 0.04f;

        public static void Update(
            bool isActive,
            float blendAlpha,
            float blurAmount,
            float centerWidthNormalized,
            float centerHeightNormalized,
            float softEdgeNormalized)
        {
            IsActive = isActive;
            BlendAlpha = Mathf.Clamp01(blendAlpha);
            BlurAmount = Mathf.Clamp01(blurAmount);
            CenterWidthNormalized = Mathf.Clamp(centerWidthNormalized, 0.01f, 1f);
            CenterHeightNormalized = Mathf.Clamp(centerHeightNormalized, 0.01f, 1f);
            SoftEdgeNormalized = Mathf.Clamp(softEdgeNormalized, 0.001f, 0.25f);
        }

        public static void Reset()
        {
            Update(false, 0f, 0f, 0.3f, 0.3f, 0.04f);
        }
    }
}
