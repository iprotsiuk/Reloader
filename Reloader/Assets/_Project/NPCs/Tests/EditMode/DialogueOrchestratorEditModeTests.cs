using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class DialogueOrchestratorEditModeTests
    {
        [Test]
        public void TryStartConversation_ValidRequest_OpensSharedRuntime()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var requester = playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();
            var speaker = new GameObject("speaker");
            var definition = CreateDefinition(
                "dialogue.valid",
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
                var request = new DialogueStartRequest(
                    definition,
                    speaker.transform,
                    DialogueStartSourceKind.PlayerInteract,
                    string.Empty,
                    DialogueInterruptPolicy.DenyIfActive);

                var started = DialogueOrchestrator.TryStartConversation(requester, request, out var result);

                Assert.That(started, Is.True);
                Assert.That(result.Success, Is.True);
                Assert.That(result.Reason, Is.EqualTo("dialogue.started"));
                Assert.That(result.SourceKind, Is.EqualTo(DialogueStartSourceKind.PlayerInteract));
                Assert.That(result.RuntimeController, Is.SameAs(runtime));
                Assert.That(runtime.HasActiveConversation, Is.True);
                Assert.That(runtime.ActiveConversation.SpeakerTransform, Is.SameAs(speaker.transform));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(speaker);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void TryStartConversation_InvalidDefinition_ReturnsExplicitFailure()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var requester = playerRoot.AddComponent<PlayerNpcInteractionController>();
            playerRoot.AddComponent<DialogueRuntimeController>();
            var speaker = new GameObject("speaker");
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
                var request = new DialogueStartRequest(
                    definition,
                    speaker.transform,
                    DialogueStartSourceKind.PlayerInteract);

                var started = DialogueOrchestrator.TryStartConversation(requester, request, out var result);

                Assert.That(started, Is.False);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Reason, Is.EqualTo("dialogue.invalid-definition"));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(speaker);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void TryStartConversation_WhileConversationActive_DefaultPolicyDeniesOverlap()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var requester = playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();
            var firstSpeaker = new GameObject("speaker-a");
            var secondSpeaker = new GameObject("speaker-b");
            var firstDefinition = CreateDefinition(
                "dialogue.first",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "First.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.first", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));
            var secondDefinition = CreateDefinition(
                "dialogue.second",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Second.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.second", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                var firstRequest = new DialogueStartRequest(
                    firstDefinition,
                    firstSpeaker.transform,
                    DialogueStartSourceKind.PlayerInteract);
                var secondRequest = new DialogueStartRequest(
                    secondDefinition,
                    secondSpeaker.transform,
                    DialogueStartSourceKind.NpcInitiated);

                Assert.That(DialogueOrchestrator.TryStartConversation(requester, firstRequest, out _), Is.True);

                var started = DialogueOrchestrator.TryStartConversation(requester, secondRequest, out var result);

                Assert.That(started, Is.False);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Reason, Is.EqualTo("dialogue.conversation-already-active"));
                Assert.That(runtime.HasActiveConversation, Is.True);
                Assert.That(runtime.ActiveConversation.Definition, Is.SameAs(firstDefinition));
            }
            finally
            {
                Object.DestroyImmediate(firstDefinition);
                Object.DestroyImmediate(secondDefinition);
                Object.DestroyImmediate(firstSpeaker);
                Object.DestroyImmediate(secondSpeaker);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void TryStartConversation_PlayerAndNpcSources_BothUseSharedStartApi()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var requester = playerRoot.AddComponent<PlayerNpcInteractionController>();
            playerRoot.AddComponent<DialogueRuntimeController>();
            var playerSpeaker = new GameObject("player-speaker");
            var npcSpeaker = new GameObject("npc-speaker");
            var definition = CreateDefinition(
                "dialogue.shared",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Shared.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                var playerRequest = new DialogueStartRequest(
                    definition,
                    playerSpeaker.transform,
                    DialogueStartSourceKind.PlayerInteract);
                var npcRequest = new DialogueStartRequest(
                    definition,
                    npcSpeaker.transform,
                    DialogueStartSourceKind.NpcInitiated,
                    string.Empty,
                    DialogueInterruptPolicy.ReplaceActive);

                Assert.That(DialogueOrchestrator.TryStartConversation(requester, playerRequest, out var playerResult), Is.True);
                Assert.That(playerResult.SourceKind, Is.EqualTo(DialogueStartSourceKind.PlayerInteract));

                var npcStarted = DialogueOrchestrator.TryStartConversation(requester, npcRequest, out var npcResult);

                Assert.That(npcStarted, Is.True);
                Assert.That(npcResult.Success, Is.True);
                Assert.That(npcResult.SourceKind, Is.EqualTo(DialogueStartSourceKind.NpcInitiated));
                Assert.That(npcResult.RuntimeController.ActiveConversation.SpeakerTransform, Is.SameAs(npcSpeaker.transform));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(playerSpeaker);
                Object.DestroyImmediate(npcSpeaker);
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
