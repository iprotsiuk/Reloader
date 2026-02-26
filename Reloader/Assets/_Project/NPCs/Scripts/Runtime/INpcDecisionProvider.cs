namespace Reloader.NPCs.Runtime
{
    public interface INpcDecisionProvider
    {
        NpcDecision Evaluate(in NpcDecisionContext context);
    }
}
