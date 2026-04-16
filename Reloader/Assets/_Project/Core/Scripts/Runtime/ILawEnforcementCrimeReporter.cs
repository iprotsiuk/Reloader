using Reloader.Core.Events;

namespace Reloader.Core.Runtime
{
    public interface ILawEnforcementCrimeReporter
    {
        void ReportCrime(CrimeType crimeType);
    }
}
