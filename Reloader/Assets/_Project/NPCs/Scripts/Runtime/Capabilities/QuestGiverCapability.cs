using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class QuestGiverCapability : MonoBehaviour, INpcCapability, INpcActionProvider
    {
        public const string ActionKey = "npc.quest-giver.interact";

        [SerializeField] private string _displayName = "Quests";
        [SerializeField] private int _priority = 5;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.QuestGiver;

        public void Initialize(NpcAgent agent)
        {
        }

        public void Shutdown()
        {
        }

        public NpcActionDefinition[] GetActions()
        {
            return new[]
            {
                new NpcActionDefinition(ActionKey, _displayName, _priority)
            };
        }
    }
}
