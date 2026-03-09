using System;
using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.LawEnforcement
{
    public sealed class PoliceStopDialogueRuntime
    {
        private const string ComplyOutcomeActionId = "police.stop.comply";
        private const string QuestionOutcomeActionId = "police.stop.question";
        private const string LeaveOutcomeActionId = "police.stop.leave";

        private readonly PoliceHeatRuntime _policeHeatRuntime;

        public PoliceStopDialogueRuntime(float searchDurationSeconds = 45f, ILawEnforcementEvents lawEnforcementEvents = null)
        {
            _policeHeatRuntime = new PoliceHeatRuntime(searchDurationSeconds, lawEnforcementEvents);
        }

        public PoliceHeatState CurrentState => _policeHeatRuntime.CurrentState;

        public bool TryHandleOutcome(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            if (string.Equals(actionId, ComplyOutcomeActionId, StringComparison.Ordinal)
                || string.Equals(actionId, QuestionOutcomeActionId, StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.Equals(actionId, LeaveOutcomeActionId, StringComparison.Ordinal))
            {
                return false;
            }

            _policeHeatRuntime.ReportCrime(CrimeType.Fleeing);
            _policeHeatRuntime.ReportLineOfSightAcquired();
            _policeHeatRuntime.ReportLineOfSightLost();
            return true;
        }
    }
}
