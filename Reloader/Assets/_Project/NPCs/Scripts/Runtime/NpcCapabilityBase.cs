using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public abstract class NpcCapabilityBase : MonoBehaviour, INpcCapability
    {
        [SerializeField] private NpcCapabilityKind _capabilityKind = NpcCapabilityKind.None;

        public NpcCapabilityKind CapabilityKind => _capabilityKind;

        public virtual void Initialize(NpcAgent agent)
        {
        }

        public virtual void Shutdown()
        {
        }
    }
}
