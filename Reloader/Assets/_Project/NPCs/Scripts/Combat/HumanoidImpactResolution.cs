using Reloader.Core.Runtime;

namespace Reloader.NPCs.Combat
{
    public static class HumanoidImpactResolution
    {
        private const float CriticalEnergyThresholdJoules = 450f;
        private const float LethalEnergyThresholdJoules = 650f;

        public static HumanoidImpactResolutionResult Resolve(HumanoidBodyZone bodyZone, float deliveredEnergyJoules)
        {
            var zoneMultiplier = ResolveZoneMultiplier(bodyZone);
            var effectiveEnergyJoules = Clamp(deliveredEnergyJoules, 0f, float.MaxValue) * zoneMultiplier;
            var isLethal = effectiveEnergyJoules >= LethalEnergyThresholdJoules;

            var severity = ResolveSeverity(effectiveEnergyJoules, isLethal);
            var recommendedRagdollImpulseScalar = ResolveRagdollImpulseScalar(effectiveEnergyJoules, isLethal);

            return new HumanoidImpactResolutionResult(
                isLethal,
                severity,
                recommendedRagdollImpulseScalar,
                effectiveEnergyJoules);
        }

        public static float ComputeDeliveredEnergyJoules(float impactSpeedMetersPerSecond, float projectileMassGrains)
        {
            return ImpactEnergyMath.ComputeDeliveredEnergyJoules(impactSpeedMetersPerSecond, projectileMassGrains);
        }

        private static HumanoidImpactSeverity ResolveSeverity(float effectiveEnergyJoules, bool isLethal)
        {
            if (isLethal)
            {
                return HumanoidImpactSeverity.Lethal;
            }

            if (effectiveEnergyJoules >= CriticalEnergyThresholdJoules)
            {
                return HumanoidImpactSeverity.Critical;
            }

            if (effectiveEnergyJoules >= 220f)
            {
                return HumanoidImpactSeverity.Serious;
            }

            if (effectiveEnergyJoules >= 60f)
            {
                return HumanoidImpactSeverity.Light;
            }

            return HumanoidImpactSeverity.Negligible;
        }

        private static float ResolveRagdollImpulseScalar(float effectiveEnergyJoules, bool isLethal)
        {
            var baselineScalar = effectiveEnergyJoules * 0.00125f;
            var lethalBonus = isLethal ? 0.35f : 0f;
            return Clamp(0.2f + baselineScalar + lethalBonus, 0.2f, 2.25f);
        }

        private static float ResolveZoneMultiplier(HumanoidBodyZone bodyZone)
        {
            switch (bodyZone)
            {
                case HumanoidBodyZone.Head:
                    return 2.2f;
                case HumanoidBodyZone.Neck:
                    return 2f;
                case HumanoidBodyZone.Torso:
                    return 1f;
                case HumanoidBodyZone.Pelvis:
                    return 0.8f;
                case HumanoidBodyZone.LegL:
                case HumanoidBodyZone.LegR:
                    return 0.55f;
                case HumanoidBodyZone.ArmL:
                case HumanoidBodyZone.ArmR:
                    return 0.35f;
                default:
                    return 1f;
            }
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
