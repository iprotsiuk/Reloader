using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class NpcAiController : MonoBehaviour
    {
        private static readonly INpcDecisionProvider FallbackDecisionProvider = new RuleBasedDecisionProvider();
        private INpcDecisionProvider _decisionProvider;

        public void SetDecisionProvider(INpcDecisionProvider decisionProvider)
        {
            _decisionProvider = decisionProvider;
        }

        public NpcDecision Evaluate(in NpcDecisionContext context)
        {
            var provider = _decisionProvider ?? FallbackDecisionProvider;
            return provider.Evaluate(in context);
        }
    }
}
