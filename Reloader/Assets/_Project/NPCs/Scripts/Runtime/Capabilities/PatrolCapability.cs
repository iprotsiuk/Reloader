using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class PatrolCapability : MonoBehaviour, INpcCapability
    {
        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.Patrol;

        public void Initialize(NpcAgent agent)
        {
        }

        public void Shutdown()
        {
        }
    }
}
