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

        [Test]
        public void CollectActions_IgnoresDisabledCapabilities()
        {
            var go = new GameObject("agent");
            var agent = go.AddComponent<NpcAgent>();
            var enabledCapability = go.AddComponent<TestActionCapability>();
            enabledCapability.Actions = new[]
            {
                new NpcActionDefinition("dialogue.open", "Talk", 0)
            };

            var disabledCapability = go.AddComponent<TestActionCapability>();
            disabledCapability.Actions = new[]
            {
                new NpcActionDefinition("trade.open", "Trade", 10)
            };
            disabledCapability.enabled = false;

            try
            {
                var actions = agent.CollectActions();
                Assert.That(actions.Count, Is.EqualTo(1));
                Assert.That(actions[0].ActionId, Is.EqualTo("dialogue.open"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectActions_WhenCapabilityDisabledAfterInitialization_RemovesActions()
        {
            var go = new GameObject("agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<TestActionCapability>();
            capability.Actions = new[]
            {
                new NpcActionDefinition("dialogue.open", "Talk", 0)
            };

            try
            {
                var initialActions = agent.CollectActions();
                capability.enabled = false;
                var updatedActions = agent.CollectActions();

                Assert.That(initialActions.Count, Is.EqualTo(1));
                Assert.That(updatedActions.Count, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectActions_WhenCapabilityEnabledAfterInitialization_AddsActions()
        {
            var go = new GameObject("agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<TestActionCapability>();
            capability.Actions = new[]
            {
                new NpcActionDefinition("dialogue.open", "Talk", 0)
            };
            capability.enabled = false;

            try
            {
                var initialActions = agent.CollectActions();
                capability.enabled = true;
                var updatedActions = agent.CollectActions();

                Assert.That(initialActions.Count, Is.EqualTo(0));
                Assert.That(updatedActions.Count, Is.EqualTo(1));
                Assert.That(updatedActions[0].ActionId, Is.EqualTo("dialogue.open"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnDisable_WhenCapabilityDestroyed_ContinuesShutdownForRemainingCapabilities()
        {
            var go = new GameObject("agent");
            var agent = go.AddComponent<NpcAgent>();
            var destroyedCapability = go.AddComponent<TestLifecycleCapability>();
            var activeCapability = go.AddComponent<TestLifecycleCapability>();

            try
            {
                agent.InitializeCapabilities();
                Object.DestroyImmediate(destroyedCapability);

                Assert.DoesNotThrow(() => go.SetActive(false));
                Assert.That(activeCapability.ShutdownCount, Is.EqualTo(1));
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
            public int ShutdownCount { get; private set; }

            public void Initialize(NpcAgent agent)
            {
                InitializeCount++;
            }

            public void Shutdown()
            {
                ShutdownCount++;
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
