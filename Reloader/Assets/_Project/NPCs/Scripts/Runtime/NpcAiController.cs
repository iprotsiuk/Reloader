using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class NpcAiController : MonoBehaviour
    {
        private INpcDecisionProvider _decisionProvider;

        public void SetDecisionProvider(INpcDecisionProvider decisionProvider)
        {
            _decisionProvider = decisionProvider;
        }

        public NpcDecision Evaluate(in NpcDecisionContext context)
        {
            var provider = _decisionProvider ?? new RuleBasedDecisionProvider();
            return provider.Evaluate(in context);
        }
    }
}
