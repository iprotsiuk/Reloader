using NUnit.Framework;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class NpcDecisionProviderEditModeTests
    {
        [Test]
        public void Evaluate_DelegatesToInjectedDecisionProvider()
        {
            var go = new GameObject("npc-ai");
            var ai = go.AddComponent<NpcAiController>();
            var provider = new RecordingDecisionProvider();

            try
            {
                ai.SetDecisionProvider(provider);

                var decision = ai.Evaluate(new NpcDecisionContext("npc.vendor.001", 2f));

                Assert.That(provider.EvaluateCount, Is.EqualTo(1));
                Assert.That(decision.ActionId, Is.EqualTo("custom.action"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Evaluate_WithoutInjectedProvider_UsesDeterministicRuleBasedFallback()
        {
            var go = new GameObject("npc-ai");
            var ai = go.AddComponent<NpcAiController>();

            try
            {
                var decision = ai.Evaluate(new NpcDecisionContext("npc.vendor.001", 0f));

                Assert.That(decision.ActionId, Is.EqualTo("idle"));
                Assert.That(decision.Reason, Is.EqualTo("rule.fallback"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Evaluate_AfterClearingInjectedProvider_RevertsToFallback()
        {
            var go = new GameObject("npc-ai");
            var ai = go.AddComponent<NpcAiController>();
            var provider = new RecordingDecisionProvider();

            try
            {
                ai.SetDecisionProvider(provider);
                var injectedDecision = ai.Evaluate(new NpcDecisionContext("npc.vendor.001", 2f));
                ai.SetDecisionProvider(null);
                var fallbackDecision = ai.Evaluate(new NpcDecisionContext("npc.vendor.001", 0f));

                Assert.That(provider.EvaluateCount, Is.EqualTo(1));
                Assert.That(injectedDecision.ActionId, Is.EqualTo("custom.action"));
                Assert.That(fallbackDecision.ActionId, Is.EqualTo("idle"));
                Assert.That(fallbackDecision.Reason, Is.EqualTo("rule.fallback"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private sealed class RecordingDecisionProvider : INpcDecisionProvider
        {
            public int EvaluateCount { get; private set; }

            public NpcDecision Evaluate(in NpcDecisionContext context)
            {
                EvaluateCount++;
                return new NpcDecision("custom.action", "test.provider");
            }
        }
    }
}
