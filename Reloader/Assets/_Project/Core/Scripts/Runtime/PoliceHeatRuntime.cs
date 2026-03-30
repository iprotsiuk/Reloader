using System;
using Reloader.Core.Events;

namespace Reloader.Core.Runtime
{
    public sealed class PoliceHeatRuntime
    {
        private const int ImmediateIdentificationWantedLevel = 2;

        private readonly float _searchDurationSeconds;
        private readonly float _identificationDurationSeconds;
        private readonly ILawEnforcementEvents _lawEnforcementEvents;
        private float _identificationProgressSeconds;

        public PoliceHeatRuntime(
            float searchDurationSeconds = 45f,
            ILawEnforcementEvents lawEnforcementEvents = null,
            float identificationDurationSeconds = 3f)
        {
            _searchDurationSeconds = Math.Max(0f, searchDurationSeconds);
            _identificationDurationSeconds = Math.Max(0f, identificationDurationSeconds);
            _lawEnforcementEvents = lawEnforcementEvents;
            CurrentState = new PoliceHeatState(PoliceHeatLevel.Clear, CrimeType.Murder, 0f, false, 0, false, 0f);
        }

        public PoliceHeatState CurrentState { get; private set; }

        public void ReportCrime(CrimeType crimeType)
        {
            var wantedLevel = Math.Max(CurrentState.WantedLevel, DetermineWantedLevel(crimeType));
            if (CurrentState.Level == PoliceHeatLevel.Clear)
            {
                _identificationProgressSeconds = 0f;
                SetState(PoliceHeatLevel.Alerted, crimeType, _searchDurationSeconds, false, wantedLevel, false);
                return;
            }

            if (CurrentState.HasLineOfSightToPlayer
                && (CurrentState.IsPlayerIdentified || ShouldIdentifyImmediately(wantedLevel)))
            {
                MarkPlayerIdentified(crimeType, wantedLevel);
                return;
            }

            var nextLevel = CurrentState.IsPlayerIdentified
                ? (CurrentState.HasLineOfSightToPlayer ? PoliceHeatLevel.ActivePursuit : PoliceHeatLevel.Search)
                : PoliceHeatLevel.Alerted;

            SetState(
                nextLevel,
                crimeType,
                _searchDurationSeconds,
                CurrentState.HasLineOfSightToPlayer,
                wantedLevel,
                CurrentState.IsPlayerIdentified);
        }

        public void ReportLineOfSightAcquired()
        {
            if (CurrentState.Level == PoliceHeatLevel.Clear)
            {
                return;
            }

            if (CurrentState.IsPlayerIdentified || ShouldIdentifyImmediately(CurrentState.WantedLevel))
            {
                MarkPlayerIdentified(CurrentState.LastCrimeType, CurrentState.WantedLevel);
                return;
            }

            SetState(
                PoliceHeatLevel.Alerted,
                CurrentState.LastCrimeType,
                CurrentState.SearchTimeRemainingSeconds,
                true,
                CurrentState.WantedLevel,
                false);
        }

        public void ReportLineOfSightLost()
        {
            if (CurrentState.Level == PoliceHeatLevel.Clear)
            {
                return;
            }

            if (!CurrentState.HasLineOfSightToPlayer)
            {
                return;
            }

            if (!CurrentState.IsPlayerIdentified)
            {
                _identificationProgressSeconds = 0f;
                SetState(
                    PoliceHeatLevel.Alerted,
                    CurrentState.LastCrimeType,
                    CurrentState.SearchTimeRemainingSeconds,
                    false,
                    CurrentState.WantedLevel,
                    false);
                return;
            }

            SetState(
                PoliceHeatLevel.Search,
                CurrentState.LastCrimeType,
                CurrentState.SearchTimeRemainingSeconds,
                false,
                CurrentState.WantedLevel,
                true);
        }

        public void Advance(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f
                || CurrentState.Level == PoliceHeatLevel.Clear)
            {
                return;
            }

            if (CurrentState.HasLineOfSightToPlayer)
            {
                if (CurrentState.IsPlayerIdentified)
                {
                    return;
                }

                if (ShouldIdentifyImmediately(CurrentState.WantedLevel))
                {
                    MarkPlayerIdentified(CurrentState.LastCrimeType, CurrentState.WantedLevel);
                    return;
                }

                _identificationProgressSeconds = Math.Min(
                    _identificationDurationSeconds,
                    _identificationProgressSeconds + deltaTimeSeconds);
                if (_identificationProgressSeconds >= _identificationDurationSeconds)
                {
                    MarkPlayerIdentified(CurrentState.LastCrimeType, CurrentState.WantedLevel);
                    return;
                }

                CurrentState = new PoliceHeatState(
                    CurrentState.Level,
                    CurrentState.LastCrimeType,
                    CurrentState.SearchTimeRemainingSeconds,
                    CurrentState.HasLineOfSightToPlayer,
                    CurrentState.WantedLevel,
                    false,
                    _identificationProgressSeconds);
                return;
            }

            if (CurrentState.Level != PoliceHeatLevel.Alerted
                && CurrentState.Level != PoliceHeatLevel.Search)
            {
                return;
            }

            var remaining = Math.Max(0f, CurrentState.SearchTimeRemainingSeconds - deltaTimeSeconds);
            if (remaining <= 0f)
            {
                ForceClear();
                return;
            }

            SetState(
                CurrentState.IsPlayerIdentified ? PoliceHeatLevel.Search : PoliceHeatLevel.Alerted,
                CurrentState.LastCrimeType,
                remaining,
                false,
                CurrentState.WantedLevel,
                CurrentState.IsPlayerIdentified);
        }

        public void ForceClear()
        {
            _identificationProgressSeconds = 0f;
            SetState(PoliceHeatLevel.Clear, CurrentState.LastCrimeType, 0f, false, 0, false);
        }

        internal void RestoreState(PoliceHeatState state)
        {
            _identificationProgressSeconds = state.IsPlayerIdentified
                ? _identificationDurationSeconds
                : Math.Min(_identificationDurationSeconds, Math.Max(0f, state.IdentificationProgressSeconds));
            CurrentState = new PoliceHeatState(
                state.Level,
                state.LastCrimeType,
                state.SearchTimeRemainingSeconds,
                state.HasLineOfSightToPlayer,
                state.WantedLevel,
                state.IsPlayerIdentified,
                _identificationProgressSeconds);
        }

        private void SetState(
            PoliceHeatLevel level,
            CrimeType lastCrimeType,
            float searchTimeRemainingSeconds,
            bool hasLineOfSightToPlayer,
            int wantedLevel,
            bool isPlayerIdentified)
        {
            var nextState = new PoliceHeatState(
                level,
                lastCrimeType,
                Math.Max(0f, searchTimeRemainingSeconds),
                hasLineOfSightToPlayer,
                wantedLevel,
                isPlayerIdentified,
                _identificationProgressSeconds);

            CurrentState = nextState;
            _lawEnforcementEvents?.RaiseHeatChanged(nextState);
        }

        private void MarkPlayerIdentified(CrimeType lastCrimeType, int wantedLevel)
        {
            _identificationProgressSeconds = _identificationDurationSeconds;
            SetState(
                PoliceHeatLevel.ActivePursuit,
                lastCrimeType,
                _searchDurationSeconds,
                true,
                wantedLevel,
                true);
        }

        private bool ShouldIdentifyImmediately(int wantedLevel)
        {
            return _identificationDurationSeconds <= 0f || wantedLevel >= ImmediateIdentificationWantedLevel;
        }

        private static int DetermineWantedLevel(CrimeType crimeType)
        {
            return crimeType switch
            {
                CrimeType.Murder => 3,
                CrimeType.AttemptedMurder => 3,
                CrimeType.Resisting => 2,
                CrimeType.Fleeing => 2,
                _ => 1
            };
        }
    }
}
