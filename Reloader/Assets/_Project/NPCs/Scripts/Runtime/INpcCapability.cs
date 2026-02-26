namespace Reloader.NPCs.Runtime
{
    public interface INpcCapability
    {
        NpcCapabilityKind CapabilityKind { get; }
        void Initialize(NpcAgent agent);
        void Shutdown();
    }
}
