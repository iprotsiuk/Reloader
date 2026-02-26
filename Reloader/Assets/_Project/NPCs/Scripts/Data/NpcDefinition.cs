using UnityEngine;

namespace Reloader.NPCs.Data
{
    [CreateAssetMenu(fileName = "NpcDefinition", menuName = "Reloader/NPCs/NPC Definition")]
    public sealed class NpcDefinition : ScriptableObject
    {
        [SerializeField] private string _npcId = string.Empty;
        [SerializeField] private NpcRolePreset _rolePreset;

        public string NpcId => _npcId;
        public NpcRolePreset RolePreset => _rolePreset;
    }
}
