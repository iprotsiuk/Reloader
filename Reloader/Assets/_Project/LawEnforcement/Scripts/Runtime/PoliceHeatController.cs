using Reloader.Core.Events;
using Reloader.Core.Runtime;

namespace Reloader.LawEnforcement
{
    public sealed class PoliceHeatController
    {
        private readonly PoliceHeatRuntime _runtime;

        public PoliceHeatController(float searchDurationSeconds = 45f, ILawEnforcementEvents lawEnforcementEvents = null)
        {
            _runtime = new PoliceHeatRuntime(searchDurationSeconds, lawEnforcementEvents);
        }

        public PoliceHeatState CurrentState => _runtime.CurrentState;

        public void ReportCrime(CrimeType crimeType)
        {
            _runtime.ReportCrime(crimeType);
        }

        public void ReportLineOfSightAcquired()
        {
            _runtime.ReportLineOfSightAcquired();
        }

        public void ReportLineOfSightLost()
        {
            _runtime.ReportLineOfSightLost();
        }

        public void Advance(float deltaTimeSeconds)
        {
            _runtime.Advance(deltaTimeSeconds);
        }
    }
}
