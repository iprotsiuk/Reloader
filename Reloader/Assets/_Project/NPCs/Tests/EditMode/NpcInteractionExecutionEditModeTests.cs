using NUnit.Framework;
using System.Reflection;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class NpcInteractionExecutionEditModeTests
    {
        [Test]
        public void TryExecuteAction_ExecutesDialogueCapabilityAndCapturesPayload()
        {
            var runtimeGo = new GameObject("dialogue-runtime");
            var go = new GameObject("dialogue-agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<DialogueCapability>();
            var runtime = runtimeGo.AddComponent<DialogueRuntimeController>();
            var definition = CreateDefinition(
                "dialogue.greeting",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Welcome to town.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ack", "Thanks.", string.Empty, "journal.note", "intro")
                    }));
            SetField(capability, "_definition", definition);

            try
            {
                var executed = agent.TryExecuteAction(
                    DialogueCapability.ActionKey,
                    payload: "topic:greeting",
                    out var result);

                Assert.That(executed, Is.True);
                Assert.That(result.Success, Is.True);
                Assert.That(capability.InteractionCount, Is.EqualTo(1));
                Assert.That(capability.LastPayload, Is.EqualTo("topic:greeting"));
                Assert.That(result.Reason, Is.EqualTo("dialogue.started"));
                Assert.That(runtime.HasActiveConversation, Is.True);
                Assert.That(runtime.ActiveConversation.Definition, Is.SameAs(definition));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(runtimeGo);
            }
        }

        [Test]
        public void TryExecuteAction_ExecutesFrontDeskCapabilityAndPublishesResultPayload()
        {
            var go = new GameObject("frontdesk-agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<FrontDeskInteractionCapability>();

            try
            {
                var executed = agent.TryExecuteAction(
                    FrontDeskInteractionCapability.ActionKey,
                    payload: "permit:range-day-pass",
                    out var result);

                Assert.That(executed, Is.True);
                Assert.That(result.Success, Is.True);
                Assert.That(result.Payload, Is.EqualTo("request.accepted:permit:range-day-pass"));
                Assert.That(capability.RequestCount, Is.EqualTo(1));
                Assert.That(capability.LastPayload, Is.EqualTo("permit:range-day-pass"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryExecuteAction_EntryFeeTracksPaidStateAndInvokesCallback()
        {
            var go = new GameObject("entry-fee-agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<EntryFeeInteractionCapability>();
            NpcActionExecutionResult callbackResult = default;
            var callbackInvoked = false;

            capability.EntryFeeProcessed += result =>
            {
                callbackInvoked = true;
                callbackResult = result;
            };

            try
            {
                var executed = agent.TryExecuteAction(
                    EntryFeeInteractionCapability.ActionKey,
                    payload: "amount:45",
                    out var result);

                Assert.That(executed, Is.True);
                Assert.That(result.Success, Is.True);
                Assert.That(result.Payload, Is.EqualTo("entry-fee.paid:45"));
                Assert.That(capability.HasPaidEntryFee, Is.True);
                Assert.That(capability.LastPaidAmount, Is.EqualTo(45));
                Assert.That(callbackInvoked, Is.True);
                Assert.That(callbackResult.Success, Is.True);
                Assert.That(callbackResult.Payload, Is.EqualTo("entry-fee.paid:45"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryExecuteAction_UnknownActionKeyFailsGracefully()
        {
            var go = new GameObject("unknown-action-agent");
            var agent = go.AddComponent<NpcAgent>();
            go.AddComponent<DialogueCapability>();

            try
            {
                var executed = agent.TryExecuteAction("npc.action.unknown", payload: "noop", out var result);

                Assert.That(executed, Is.False);
                Assert.That(result.Success, Is.False);
                Assert.That(result.ActionKey, Is.EqualTo("npc.action.unknown"));
                Assert.That(result.Reason, Is.EqualTo("npc.action.unhandled"));
            }
            finally
            {
                Object.DestroyImmediate(go);
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
