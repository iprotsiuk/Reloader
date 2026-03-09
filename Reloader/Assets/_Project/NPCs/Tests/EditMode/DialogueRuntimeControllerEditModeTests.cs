using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class DialogueRuntimeControllerEditModeTests
    {
        [Test]
        public void TryOpenConversation_ActivatesEntryNodeAndSelectsFirstReply()
        {
            var controllerGo = new GameObject("dialogue-runtime");
            var speakerGo = new GameObject("speaker");
            var controller = controllerGo.AddComponent<DialogueRuntimeController>();
            var definition = CreateDefinition(
                "dialogue.greeting",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Welcome to town.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ack", "Thanks.", string.Empty, "journal.note", "intro"),
                        new DialogueReplyDefinition("reply.leave", "Later.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                var opened = controller.TryOpenConversation(definition, speakerGo.transform, out var reason);

                Assert.That(opened, Is.True);
                Assert.That(reason, Is.EqualTo("dialogue.started"));
                Assert.That(controller.HasActiveConversation, Is.True);
                Assert.That(controller.ActiveConversation.Definition, Is.SameAs(definition));
                Assert.That(controller.ActiveConversation.CurrentNode.NodeId, Is.EqualTo("entry"));
                Assert.That(controller.ActiveConversation.SelectedReplyIndex, Is.EqualTo(0));
                Assert.That(controller.ActiveConversation.SelectedReply.ReplyId, Is.EqualTo("reply.ack"));
                Assert.That(controller.ActiveConversation.SpeakerTransform, Is.SameAs(speakerGo.transform));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(speakerGo);
                Object.DestroyImmediate(controllerGo);
            }
        }

        [Test]
        public void TryOpenConversation_ReplacesAnyExistingConversation()
        {
            var controllerGo = new GameObject("dialogue-runtime");
            var firstSpeakerGo = new GameObject("speaker-a");
            var secondSpeakerGo = new GameObject("speaker-b");
            var controller = controllerGo.AddComponent<DialogueRuntimeController>();
            var first = CreateDefinition(
                "dialogue.first",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "First speaker.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.first", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));
            var second = CreateDefinition(
                "dialogue.second",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Second speaker.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.second", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                Assert.That(controller.TryOpenConversation(first, firstSpeakerGo.transform, out _), Is.True);

                var reopened = controller.TryOpenConversation(second, secondSpeakerGo.transform, out var reason);

                Assert.That(reopened, Is.True);
                Assert.That(reason, Is.EqualTo("dialogue.started"));
                Assert.That(controller.HasActiveConversation, Is.True);
                Assert.That(controller.ActiveConversation.Definition, Is.SameAs(second));
                Assert.That(controller.ActiveConversation.SpeakerTransform, Is.SameAs(secondSpeakerGo.transform));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(firstSpeakerGo);
                Object.DestroyImmediate(secondSpeakerGo);
                Object.DestroyImmediate(controllerGo);
            }
        }

        [Test]
        public void TryConfirmSelectedReply_ClosesTerminalConversationAndReturnsStructuredOutcome()
        {
            var controllerGo = new GameObject("dialogue-runtime");
            var speakerGo = new GameObject("speaker");
            var controller = controllerGo.AddComponent<DialogueRuntimeController>();
            var definition = CreateDefinition(
                "dialogue.contract",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Need work?",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.accept", "I am listening.", string.Empty, "contract.offer", "contract.mainTown.0001")
                    }));

            try
            {
                Assert.That(controller.TryOpenConversation(definition, speakerGo.transform, out _), Is.True);

                var confirmed = controller.TryConfirmSelectedReply(out var result);

                Assert.That(confirmed, Is.True);
                Assert.That(result.Success, Is.True);
                Assert.That(result.Reason, Is.EqualTo("dialogue.confirmed"));
                Assert.That(result.Outcome.ReplyId, Is.EqualTo("reply.accept"));
                Assert.That(result.Outcome.ActionId, Is.EqualTo("contract.offer"));
                Assert.That(result.Outcome.Payload, Is.EqualTo("contract.mainTown.0001"));
                Assert.That(result.Outcome.NextNodeId, Is.EqualTo(string.Empty));
                Assert.That(controller.HasActiveConversation, Is.False);
                Assert.That(controller.LastOutcome.ReplyId, Is.EqualTo("reply.accept"));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(speakerGo);
                Object.DestroyImmediate(controllerGo);
            }
        }

        [Test]
        public void RefreshActiveConversationState_ClosesWhenSpeakerIsDisabled()
        {
            var controllerGo = new GameObject("dialogue-runtime");
            var speakerGo = new GameObject("speaker");
            var controller = controllerGo.AddComponent<DialogueRuntimeController>();
            var definition = CreateDefinition(
                "dialogue.greeting",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Welcome.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ack", "Thanks.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                Assert.That(controller.TryOpenConversation(definition, speakerGo.transform, out _), Is.True);
                speakerGo.SetActive(false);

                controller.RefreshActiveConversationState();

                Assert.That(controller.HasActiveConversation, Is.False);
                Assert.That(controller.LastCloseReason, Is.EqualTo("dialogue.target-invalid"));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(speakerGo);
                Object.DestroyImmediate(controllerGo);
            }
        }

        [Test]
        public void TryOpenConversation_InvalidDefinitionFailsGracefully()
        {
            var controllerGo = new GameObject("dialogue-runtime");
            var speakerGo = new GameObject("speaker");
            var controller = controllerGo.AddComponent<DialogueRuntimeController>();
            var definition = CreateDefinition(
                "dialogue.invalid",
                "missing",
                new DialogueNodeDefinition(
                    "entry",
                    "Welcome.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ack", "Thanks.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                var opened = controller.TryOpenConversation(definition, speakerGo.transform, out var reason);

                Assert.That(opened, Is.False);
                Assert.That(reason, Is.EqualTo("dialogue.invalid-definition"));
                Assert.That(controller.HasActiveConversation, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(speakerGo);
                Object.DestroyImmediate(controllerGo);
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
