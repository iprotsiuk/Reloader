using NUnit.Framework;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class NpcAgentEditModeTests
    {
        [Test]
        public void InitializeCapabilities_InitializesEachCapabilityOnce()
        {
            var go = new GameObject("agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<TestLifecycleCapability>();

            try
            {
                agent.InitializeCapabilities();
                agent.InitializeCapabilities();

                Assert.That(capability.InitializeCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectActions_AggregatesAcrossInstalledProviders()
        {
            var go = new GameObject("agent");
            var agent = go.AddComponent<NpcAgent>();

            var first = go.AddComponent<TestActionCapability>();
            first.Actions = new[]
            {
                new NpcActionDefinition("trade.open", "Open Trade", 10)
            };

            var second = go.AddComponent<TestActionCapability>();
            second.Actions = new[]
            {
                new NpcActionDefinition("dialogue.open", "Talk", 0)
            };

            try
            {
                agent.InitializeCapabilities();
                var actions = agent.CollectActions();

                Assert.That(actions.Count, Is.EqualTo(2));
                Assert.That(actions[0].ActionId, Is.EqualTo("trade.open"));
                Assert.That(actions[1].ActionId, Is.EqualTo("dialogue.open"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private sealed class TestLifecycleCapability : MonoBehaviour, INpcCapability
        {
            public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.None;
            public int InitializeCount { get; private set; }

            public void Initialize(NpcAgent agent)
            {
                InitializeCount++;
            }

            public void Shutdown()
            {
            }
        }

        private sealed class TestActionCapability : MonoBehaviour, INpcCapability, INpcActionProvider
        {
            public NpcActionDefinition[] Actions { get; set; }
            public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.None;

            public void Initialize(NpcAgent agent)
            {
            }

            public void Shutdown()
            {
            }

            public NpcActionDefinition[] GetActions()
            {
                return Actions;
            }
        }
    }
}
