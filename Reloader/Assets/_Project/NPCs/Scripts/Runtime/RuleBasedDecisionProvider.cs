namespace Reloader.NPCs.Runtime
{
    public sealed class RuleBasedDecisionProvider : INpcDecisionProvider
    {
        public NpcDecision Evaluate(in NpcDecisionContext context)
        {
            if (context.TimeSinceLastActionSeconds > 1f)
            {
                return new NpcDecision("observe", "rule.observe");
            }

            return new NpcDecision("idle", "rule.fallback");
        }
    }
}
