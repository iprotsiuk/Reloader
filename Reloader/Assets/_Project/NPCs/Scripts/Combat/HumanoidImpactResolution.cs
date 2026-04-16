using Reloader.Core.Runtime;

namespace Reloader.NPCs.Combat
{
    public static class HumanoidImpactResolution
    {
        private static readonly HumanoidZoneDamageRule HeadAndNeckDamageRule = new(0.50f, 100f, 200f);
        private static readonly HumanoidZoneDamageRule TorsoDamageRule = new(0.045f, 100f, 1800f);
        private static readonly HumanoidZoneDamageRule PelvisDamageRule = new(0.030f, 80f);
        private static readonly HumanoidZoneDamageRule LegDamageRule = new(0.015f, 45f);
        private static readonly HumanoidZoneDamageRule ArmDamageRule = new(0.010f, 35f);

        public static HumanoidImpactResolutionResult Resolve(HumanoidBodyZone bodyZone, float deliveredEnergyJoules)
        {
            var impactEnergyJoules = Clamp(deliveredEnergyJoules, 0f, float.MaxValue);
            var rule = ResolveZoneDamageRule(bodyZone);
            var isLethal = rule.HasInstantLethal && impactEnergyJoules >= rule.LethalEnergyJoules;
            var recommendedHealthDamage = isLethal
                ? rule.MaxDamage
                : Clamp(impactEnergyJoules * rule.DamagePerJoule, 0f, rule.MaxDamage);

            var severity = ResolveSeverity(recommendedHealthDamage, isLethal);
            var recommendedRagdollImpulseScalar = ResolveRagdollImpulseScalar(impactEnergyJoules, isLethal);

            return new HumanoidImpactResolutionResult(
                isLethal,
                severity,
                recommendedRagdollImpulseScalar,
                impactEnergyJoules,
                recommendedHealthDamage);
        }

        public static float ComputeDeliveredEnergyJoules(float impactSpeedMetersPerSecond, float projectileMassGrains)
        {
            return ImpactEnergyMath.ComputeDeliveredEnergyJoules(impactSpeedMetersPerSecond, projectileMassGrains);
        }

        private static HumanoidImpactSeverity ResolveSeverity(float recommendedHealthDamage, bool isLethal)
        {
            if (isLethal)
            {
                return HumanoidImpactSeverity.Lethal;
            }

            if (recommendedHealthDamage >= 80f)
            {
                return HumanoidImpactSeverity.Critical;
            }

            if (recommendedHealthDamage >= 35f)
            {
                return HumanoidImpactSeverity.Serious;
            }

            if (recommendedHealthDamage > 0f)
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

        private static HumanoidZoneDamageRule ResolveZoneDamageRule(HumanoidBodyZone bodyZone)
        {
            switch (bodyZone)
            {
                case HumanoidBodyZone.Head:
                case HumanoidBodyZone.Neck:
                    return HeadAndNeckDamageRule;
                case HumanoidBodyZone.Torso:
                    return TorsoDamageRule;
                case HumanoidBodyZone.Pelvis:
                    return PelvisDamageRule;
                case HumanoidBodyZone.LegL:
                case HumanoidBodyZone.LegR:
                    return LegDamageRule;
                case HumanoidBodyZone.ArmL:
                case HumanoidBodyZone.ArmR:
                    return ArmDamageRule;
                default:
                    return TorsoDamageRule;
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

        private readonly struct HumanoidZoneDamageRule
        {
            public HumanoidZoneDamageRule(float damagePerJoule, float maxDamage, float? lethalEnergyJoules = null)
            {
                DamagePerJoule = damagePerJoule;
                MaxDamage = maxDamage;
                LethalEnergyJoules = lethalEnergyJoules.GetValueOrDefault();
                HasInstantLethal = lethalEnergyJoules.HasValue;
            }

            public float DamagePerJoule { get; }
            public float MaxDamage { get; }
            public float LethalEnergyJoules { get; }
            public bool HasInstantLethal { get; }
        }
    }
}
