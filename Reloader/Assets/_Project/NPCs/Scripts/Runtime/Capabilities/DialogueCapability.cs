using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class DialogueCapability : MonoBehaviour, INpcCapability, INpcActionProvider
    {
        public const string ActionKey = "npc.dialogue.interact";

        [SerializeField] private string _displayName = "Talk";
        [SerializeField] private int _priority = 0;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.Dialogue;

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
