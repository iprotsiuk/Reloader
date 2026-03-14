namespace Reloader.Core.Runtime
{
    public static class ImpactEnergyMath
    {
        private const float GrainsToKilograms = 0.00006479891f;

        public static float ComputeDeliveredEnergyJoules(float impactSpeedMetersPerSecond, float projectileMassGrains)
        {
            var safeSpeed = Clamp(impactSpeedMetersPerSecond, 0f, float.MaxValue);
            var safeMassGrains = Clamp(projectileMassGrains, 0f, float.MaxValue);
            var projectileMassKilograms = safeMassGrains * GrainsToKilograms;
            return 0.5f * projectileMassKilograms * safeSpeed * safeSpeed;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
