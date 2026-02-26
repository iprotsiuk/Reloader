using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Data
{
    [CreateAssetMenu(fileName = "NpcRolePreset", menuName = "Reloader/NPCs/Role Preset")]
    public sealed class NpcRolePreset : ScriptableObject
    {
        [SerializeField] private NpcRoleKind _roleKind = NpcRoleKind.None;
        [SerializeField] private NpcCapabilityConfig[] _capabilities = new NpcCapabilityConfig[0];

        public NpcRoleKind RoleKind => _roleKind;
        public NpcCapabilityConfig[] Capabilities => _capabilities;
    }
}
