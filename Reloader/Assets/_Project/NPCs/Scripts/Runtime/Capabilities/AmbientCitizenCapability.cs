using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class AmbientCitizenCapability : MonoBehaviour, INpcCapability
    {
        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.AmbientCitizen;

        public void Initialize(NpcAgent agent)
        {
        }

        public void Shutdown()
        {
        }
    }
}
