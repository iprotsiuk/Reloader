using NUnit.Framework;
using System.Reflection;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
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
            var capability = npc.gameObject.AddComponent<DialogueCapability>();
            var runtime = root.AddComponent<DialogueRuntimeController>();
            var definition = CreateDefinition(
                "dialogue.bridge",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Hello there.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ack", "Hi.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(capability, "_definition", definition);
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
                Object.DestroyImmediate(definition);
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

        [Test]
        public void RequestExecuteAction_WhenControllerAppearsAfterEnable_SubscribesAndPublishesResult()
        {
            var root = new GameObject("npc-ui-bridge-latebind");
            var bridge = root.AddComponent<PlayerNpcInteractionUiBridge>();
            bridge.enabled = true;

            var resolver = root.AddComponent<TestNpcResolver>();
            var controller = root.AddComponent<PlayerNpcInteractionController>();
            controller.Configure(null, resolver);

            var npc = new GameObject("npc-dialogue-late").AddComponent<NpcAgent>();
            var capability = npc.gameObject.AddComponent<DialogueCapability>();
            var runtime = root.AddComponent<DialogueRuntimeController>();
            var definition = CreateDefinition(
                "dialogue.late",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Hello there.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ack", "Hi.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(capability, "_definition", definition);
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
                bridge.RequestExecuteAction(DialogueCapability.ActionKey, "hello");
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(npc.gameObject);
            }

            Assert.That(raised, Is.True);
            Assert.That(captured.Success, Is.True);
            Assert.That(captured.ActionKey, Is.EqualTo(DialogueCapability.ActionKey));
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

        private static DialogueDefinition CreateDefinition(string dialogueId, string entryNodeId, params DialogueNodeDefinition[] nodes)
        {
            var definition = ScriptableObject.CreateInstance<DialogueDefinition>();
            SetField(definition, "_dialogueId", dialogueId);
            SetField(definition, "_entryNodeId", entryNodeId);
            SetField(definition, "_nodes", nodes);
            return definition;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
