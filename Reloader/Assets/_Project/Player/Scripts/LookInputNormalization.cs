using UnityEngine;

namespace Reloader.Player
{
    public static class LookInputNormalization
    {
        private const float MouseLookNormalizationScale = 0.05f;

        public static Vector2 NormalizeLookDelta(Vector2 lookInput, string activeControlPath)
        {
            if (!string.IsNullOrWhiteSpace(activeControlPath)
                && activeControlPath.EndsWith("/delta", System.StringComparison.OrdinalIgnoreCase))
            {
                return lookInput * MouseLookNormalizationScale;
            }

            return lookInput;
        }
    }
}
