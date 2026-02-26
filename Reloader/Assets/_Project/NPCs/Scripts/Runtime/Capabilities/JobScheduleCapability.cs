using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class JobScheduleCapability : MonoBehaviour, INpcCapability
    {
        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.JobSchedule;

        public void Initialize(NpcAgent agent)
        {
        }

        public void Shutdown()
        {
        }
    }
}
