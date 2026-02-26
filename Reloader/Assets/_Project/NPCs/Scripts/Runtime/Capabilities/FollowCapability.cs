using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class FollowCapability : MonoBehaviour, INpcCapability, INpcActionProvider
    {
        public const string ActionKey = "npc.follow.interact";

        [SerializeField] private string _displayName = "Follow";
        [SerializeField] private int _priority = 5;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.Follow;

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
