using Reloader.NPCs.Runtime;

namespace Reloader.NPCs.World
{
    public interface IPlayerNpcResolver
    {
        bool TryResolveNpcAgent(out NpcAgent target);
    }
}
