using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class LawEnforcementInteractionCapability : MonoBehaviour, INpcCapability, INpcActionProvider
    {
        public const string ActionKey = "npc.law-enforcement.interact";

        [SerializeField] private string _displayName = "Interact";
        [SerializeField] private int _priority = 20;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.LawEnforcementInteraction;

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
