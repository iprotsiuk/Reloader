namespace Reloader.NPCs.Combat
{
    public enum HumanoidImpactSeverity
    {
        Negligible = 0,
        Light = 1,
        Serious = 2,
        Critical = 3,
        Lethal = 4
    }

    public readonly struct HumanoidImpactResolutionResult
    {
        public HumanoidImpactResolutionResult(
            bool isLethal,
            HumanoidImpactSeverity severity,
            float recommendedRagdollImpulseScalar,
            float effectiveEnergyJoules)
        {
            IsLethal = isLethal;
            Severity = severity;
            RecommendedRagdollImpulseScalar = recommendedRagdollImpulseScalar;
            EffectiveEnergyJoules = effectiveEnergyJoules;
        }

        public bool IsLethal { get; }
        public HumanoidImpactSeverity Severity { get; }
        public float RecommendedRagdollImpulseScalar { get; }
        public float EffectiveEnergyJoules { get; }
    }
}
