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
            var ai = CreateNpcAiController();
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
                DestroyNpcAiController(ai);
            }
        }

        [Test]
        public void Evaluate_WithoutInjectedProvider_UsesDeterministicRuleBasedFallback()
        {
            var ai = CreateNpcAiController();

            try
            {
                var decision = ai.Evaluate(new NpcDecisionContext("npc.vendor.001", 0f));

                Assert.That(decision.ActionId, Is.EqualTo("idle"));
                Assert.That(decision.Reason, Is.EqualTo("rule.fallback"));
            }
            finally
            {
                DestroyNpcAiController(ai);
            }
        }

        [Test]
        public void Evaluate_AfterClearingInjectedProvider_RevertsToFallback()
        {
            var ai = CreateNpcAiController();
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
                DestroyNpcAiController(ai);
            }
        }

        private static NpcAiController CreateNpcAiController()
        {
            var gameObject = new GameObject("npc-ai");
            return gameObject.AddComponent<NpcAiController>();
        }

        private static void DestroyNpcAiController(NpcAiController controller)
        {
            if (controller != null)
            {
                Object.DestroyImmediate(controller.gameObject);
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
