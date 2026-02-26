using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Data
{
    [CreateAssetMenu(fileName = "NpcCapability", menuName = "Reloader/NPCs/Capability Config")]
    public sealed class NpcCapabilityConfig : ScriptableObject
    {
        [SerializeField] private NpcCapabilityKind _kind = NpcCapabilityKind.None;

        public NpcCapabilityKind Kind => _kind;
    }
}
