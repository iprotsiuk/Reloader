using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class FrontDeskInteractionCapability : MonoBehaviour, INpcCapability, INpcActionProvider
    {
        public const string ActionKey = "npc.front-desk.interact";

        [SerializeField] private string _displayName = "Front Desk";
        [SerializeField] private int _priority = 10;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.FrontDeskInteraction;

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
