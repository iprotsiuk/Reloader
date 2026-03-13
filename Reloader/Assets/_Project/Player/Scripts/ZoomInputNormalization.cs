using UnityEngine;

namespace Reloader.Player
{
    public static class ZoomInputNormalization
    {
        public static float NormalizeScrollDelta(float scrollDelta)
        {
            var abs = Mathf.Abs(scrollDelta);
            if (abs <= 0.0001f)
            {
                return 0f;
            }

            if (abs >= 60f)
            {
                return scrollDelta / 120f;
            }

            if (abs >= 8f)
            {
                return scrollDelta / 12f;
            }

            if (abs < 1f)
            {
                var scaled = 0.2f + (abs * 0.8f);
                return Mathf.Sign(scrollDelta) * Mathf.Clamp(scaled, 0.2f, 1f);
            }

            return scrollDelta;
        }
    }
}
