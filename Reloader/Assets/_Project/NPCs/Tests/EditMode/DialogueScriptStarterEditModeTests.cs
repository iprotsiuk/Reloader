using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class DialogueScriptStarterEditModeTests
    {
        [Test]
        public void TryStartConversation_ValidScriptRequest_UsesSharedOrchestrator()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var requester = playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();
            var speaker = new GameObject("speaker");
            var definition = CreateDefinition(
                "dialogue.script",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Scripted hello.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                var started = DialogueScriptStarter.TryStartConversation(
                    requester,
                    definition,
                    speaker.transform,
                    out var result,
                    payload: "quest:intro");

                Assert.That(started, Is.True);
                Assert.That(result.Success, Is.True);
                Assert.That(result.SourceKind, Is.EqualTo(DialogueStartSourceKind.Script));
                Assert.That(result.RuntimeController, Is.SameAs(runtime));
                Assert.That(runtime.HasActiveConversation, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(speaker);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void TryStartConversation_WhileConversationAlreadyActive_RespectsOverlapDenial()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var requester = playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();
            var firstSpeaker = new GameObject("speaker-a");
            var secondSpeaker = new GameObject("speaker-b");
            var definition = CreateDefinition(
                "dialogue.script",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Scripted hello.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                Assert.That(runtime.TryOpenConversation(definition, firstSpeaker.transform, out _), Is.True);

                var started = DialogueScriptStarter.TryStartConversation(
                    requester,
                    definition,
                    secondSpeaker.transform,
                    out var result);

                Assert.That(started, Is.False);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Reason, Is.EqualTo("dialogue.conversation-already-active"));
                Assert.That(runtime.ActiveConversation.SpeakerTransform, Is.SameAs(firstSpeaker.transform));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(firstSpeaker);
                Object.DestroyImmediate(secondSpeaker);
                Object.DestroyImmediate(playerRoot);
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
