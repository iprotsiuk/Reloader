using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class EntryFeeInteractionCapability : MonoBehaviour, INpcCapability, INpcActionProvider
    {
        public const string ActionKey = "npc.entry-fee.interact";

        [SerializeField] private string _displayName = "Pay Entry Fee";
        [SerializeField] private int _priority = 15;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.EntryFeeInteraction;

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
