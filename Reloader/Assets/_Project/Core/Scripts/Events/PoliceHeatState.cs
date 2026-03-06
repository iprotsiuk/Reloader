namespace Reloader.Core.Events
{
    public readonly struct PoliceHeatState
    {
        public PoliceHeatState(
            PoliceHeatLevel level,
            CrimeType lastCrimeType,
            float searchTimeRemainingSeconds,
            bool hasLineOfSightToPlayer)
        {
            Level = level;
            LastCrimeType = lastCrimeType;
            SearchTimeRemainingSeconds = searchTimeRemainingSeconds;
            HasLineOfSightToPlayer = hasLineOfSightToPlayer;
        }

        public PoliceHeatLevel Level { get; }
        public CrimeType LastCrimeType { get; }
        public float SearchTimeRemainingSeconds { get; }
        public bool HasLineOfSightToPlayer { get; }
    }
}
