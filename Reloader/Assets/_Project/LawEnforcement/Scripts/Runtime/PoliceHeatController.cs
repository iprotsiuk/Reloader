using System;
using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.LawEnforcement
{
    public sealed class PoliceHeatController
    {
        private readonly float _searchDurationSeconds;
        private readonly ILawEnforcementEvents _lawEnforcementEvents;

        public PoliceHeatController(float searchDurationSeconds = 45f, ILawEnforcementEvents lawEnforcementEvents = null)
        {
            _searchDurationSeconds = Math.Max(0f, searchDurationSeconds);
            _lawEnforcementEvents = lawEnforcementEvents;
            CurrentState = new PoliceHeatState(PoliceHeatLevel.Clear, CrimeType.Murder, 0f, false);
        }

        public PoliceHeatState CurrentState { get; private set; }

        public void ReportCrime(CrimeType crimeType)
        {
            if (CurrentState.Level == PoliceHeatLevel.Clear)
            {
                SetState(PoliceHeatLevel.Alerted, crimeType, _searchDurationSeconds, false);
                return;
            }

            SetState(CurrentState.Level, crimeType, CurrentState.SearchTimeRemainingSeconds, CurrentState.HasLineOfSightToPlayer);
        }

        public void ReportLineOfSightAcquired()
        {
            if (CurrentState.Level == PoliceHeatLevel.Clear)
            {
                return;
            }

            SetState(PoliceHeatLevel.ActivePursuit, CurrentState.LastCrimeType, _searchDurationSeconds, true);
        }

        public void ReportLineOfSightLost()
        {
            if (CurrentState.Level == PoliceHeatLevel.Clear)
            {
                return;
            }

            SetState(PoliceHeatLevel.Search, CurrentState.LastCrimeType, _searchDurationSeconds, false);
        }

        public void Advance(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f
                || CurrentState.Level != PoliceHeatLevel.Search
                || CurrentState.HasLineOfSightToPlayer)
            {
                return;
            }

            var remaining = Math.Max(0f, CurrentState.SearchTimeRemainingSeconds - deltaTimeSeconds);
            if (remaining <= 0f)
            {
                SetState(PoliceHeatLevel.Clear, CurrentState.LastCrimeType, 0f, false);
                return;
            }

            SetState(PoliceHeatLevel.Search, CurrentState.LastCrimeType, remaining, false);
        }

        private void SetState(
            PoliceHeatLevel level,
            CrimeType lastCrimeType,
            float searchTimeRemainingSeconds,
            bool hasLineOfSightToPlayer)
        {
            var nextState = new PoliceHeatState(
                level,
                lastCrimeType,
                Math.Max(0f, searchTimeRemainingSeconds),
                hasLineOfSightToPlayer);

            CurrentState = nextState;
            _lawEnforcementEvents?.RaiseHeatChanged(nextState);
        }
    }
}
