using NUnit.Framework;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class PlayerNpcInteractionUiBridgeEditModeTests
    {
        [Test]
        public void PublishAndRequest_RoutesActionsResultAndExecuteEvent()
        {
            var go = new GameObject("npc-ui-bridge");
            var bridge = go.AddComponent<PlayerNpcInteractionUiBridge>();

            try
            {
                var actionsRaised = 0;
                var selectedActionKey = string.Empty;
                var executeRequestedKey = string.Empty;
                var executeRequestedPayload = string.Empty;
                NpcActionExecutionResult capturedResult = default;

                bridge.AvailableActionsChanged += (actions, selectedKey) =>
                {
                    actionsRaised++;
                    selectedActionKey = selectedKey;
                    Assert.That(actions.Count, Is.EqualTo(2));
                };

                bridge.ActionExecuted += result => capturedResult = result;
                bridge.ExecuteActionRequested += (actionKey, payload) =>
                {
                    executeRequestedKey = actionKey;
                    executeRequestedPayload = payload;
                };

                bridge.PublishAvailableActions(new[]
                {
                    new NpcActionDefinition("npc.action.dialogue", "Talk", 10),
                    new NpcActionDefinition("npc.action.trade", "Trade", 5, "vendor-1")
                }, "npc.action.trade");

                bridge.RequestExecuteAction("npc.action.trade", "vendor-1");
                bridge.PublishExecutionResult(new NpcActionExecutionResult("npc.action.trade", true, "trade.opened", "vendor-1"));

                Assert.That(actionsRaised, Is.EqualTo(1));
                Assert.That(selectedActionKey, Is.EqualTo("npc.action.trade"));
                Assert.That(executeRequestedKey, Is.EqualTo("npc.action.trade"));
                Assert.That(executeRequestedPayload, Is.EqualTo("vendor-1"));
                Assert.That(capturedResult.ActionKey, Is.EqualTo("npc.action.trade"));
                Assert.That(capturedResult.Success, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RequestExecuteAction_WithoutExplicitSource_UsesLocalInteractionController()
        {
            var root = new GameObject("npc-ui-bridge-root");
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            var resolver = root.AddComponent<TestNpcResolver>();
            var bridge = root.AddComponent<PlayerNpcInteractionUiBridge>();
            bridge.enabled = false;
            controller.Configure(null, resolver);

            var npc = new GameObject("npc-dialogue").AddComponent<NpcAgent>();
            npc.gameObject.AddComponent<DialogueCapability>();
            resolver.Target = npc;

            var raised = false;
            NpcActionExecutionResult captured = default;
            bridge.ActionExecuted += result =>
            {
                raised = true;
                captured = result;
            };

            try
            {
                bridge.enabled = true;
                bridge.RequestExecuteAction(DialogueCapability.ActionKey, "hello");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }

            Assert.That(resolver.ResolveCallCount, Is.GreaterThan(0));
            if (raised)
            {
                Assert.That(captured.Success, Is.True);
                Assert.That(captured.ActionKey, Is.EqualTo(DialogueCapability.ActionKey));
            }
        }

        private sealed class TestNpcResolver : MonoBehaviour, IPlayerNpcResolver
        {
            public NpcAgent Target { get; set; }
            public int ResolveCallCount { get; private set; }

            public bool TryResolveNpcAgent(out NpcAgent target)
            {
                ResolveCallCount++;
                target = Target;
                return target != null;
            }
        }
    }
}
