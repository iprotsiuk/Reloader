using System;

namespace Reloader.Core.Events
{
    public readonly struct PoliceHeatState
    {
        public PoliceHeatState(
            PoliceHeatLevel level,
            CrimeType lastCrimeType,
            float searchTimeRemainingSeconds,
            bool hasLineOfSightToPlayer)
            : this(
                level,
                lastCrimeType,
                searchTimeRemainingSeconds,
                hasLineOfSightToPlayer,
                DeriveWantedLevel(level, lastCrimeType),
                level == PoliceHeatLevel.ActivePursuit || level == PoliceHeatLevel.Search,
                0f)
        {
        }

        public PoliceHeatState(
            PoliceHeatLevel level,
            CrimeType lastCrimeType,
            float searchTimeRemainingSeconds,
            bool hasLineOfSightToPlayer,
            int wantedLevel,
            bool isPlayerIdentified)
            : this(
                level,
                lastCrimeType,
                searchTimeRemainingSeconds,
                hasLineOfSightToPlayer,
                wantedLevel,
                isPlayerIdentified,
                0f)
        {
        }

        public PoliceHeatState(
            PoliceHeatLevel level,
            CrimeType lastCrimeType,
            float searchTimeRemainingSeconds,
            bool hasLineOfSightToPlayer,
            int wantedLevel,
            bool isPlayerIdentified,
            float identificationProgressSeconds)
        {
            Level = level;
            LastCrimeType = lastCrimeType;
            SearchTimeRemainingSeconds = Math.Max(0f, searchTimeRemainingSeconds);
            HasLineOfSightToPlayer = level == PoliceHeatLevel.Clear ? false : hasLineOfSightToPlayer;
            WantedLevel = level == PoliceHeatLevel.Clear ? 0 : Math.Max(0, wantedLevel);
            IsPlayerIdentified = level != PoliceHeatLevel.Clear
                                 && (isPlayerIdentified
                                     || level == PoliceHeatLevel.ActivePursuit
                                     || level == PoliceHeatLevel.Search);
            IdentificationProgressSeconds = ShouldTrackIdentificationProgress(level, HasLineOfSightToPlayer, IsPlayerIdentified)
                ? Math.Max(0f, identificationProgressSeconds)
                : 0f;
        }

        public PoliceHeatLevel Level { get; }
        public CrimeType LastCrimeType { get; }
        public float SearchTimeRemainingSeconds { get; }
        public bool HasLineOfSightToPlayer { get; }
        public int WantedLevel { get; }
        public bool IsPlayerIdentified { get; }
        public float IdentificationProgressSeconds { get; }

        private static int DeriveWantedLevel(PoliceHeatLevel level, CrimeType lastCrimeType)
        {
            if (level == PoliceHeatLevel.Clear)
            {
                return 0;
            }

            return lastCrimeType switch
            {
                CrimeType.Murder => 3,
                CrimeType.AttemptedMurder => 3,
                CrimeType.Resisting => 2,
                CrimeType.Fleeing => 2,
                _ => 1
            };
        }

        private static bool ShouldTrackIdentificationProgress(
            PoliceHeatLevel level,
            bool hasLineOfSightToPlayer,
            bool isPlayerIdentified)
        {
            return level != PoliceHeatLevel.Clear
                   && hasLineOfSightToPlayer
                   && !isPlayerIdentified;
        }
    }
}
