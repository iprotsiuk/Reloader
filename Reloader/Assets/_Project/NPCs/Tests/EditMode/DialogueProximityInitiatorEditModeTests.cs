using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class DialogueProximityInitiatorEditModeTests
    {
        [Test]
        public void Tick_PlayerWithinRange_StartsNpcInitiatedConversationOnSharedRuntime()
        {
            var playerRoot = new GameObject("PlayerRoot");
            playerRoot.transform.position = Vector3.zero;
            playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();

            var npc = new GameObject("NpcSpeaker");
            npc.transform.position = new Vector3(1.5f, 0f, 0f);
            var initiator = npc.AddComponent<DialogueProximityInitiator>();
            var definition = CreateDefinition(
                "dialogue.nearby",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Hey.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Talk.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(initiator, "_definition", definition);
            SetField(initiator, "_playerTransformOverride", playerRoot.transform);
            SetField(initiator, "_triggerDistanceMeters", 2f);

            try
            {
                initiator.Tick();

                Assert.That(runtime.HasActiveConversation, Is.True);
                Assert.That(runtime.ActiveConversation.SpeakerTransform, Is.SameAs(npc.transform));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(npc);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void Tick_PlayerOutsideRange_DoesNotStartConversation()
        {
            var playerRoot = new GameObject("PlayerRoot");
            playerRoot.transform.position = Vector3.zero;
            playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();

            var npc = new GameObject("NpcSpeaker");
            npc.transform.position = new Vector3(4f, 0f, 0f);
            var initiator = npc.AddComponent<DialogueProximityInitiator>();
            var definition = CreateDefinition(
                "dialogue.far",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Too far.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ok", "Talk.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(initiator, "_definition", definition);
            SetField(initiator, "_playerTransformOverride", playerRoot.transform);
            SetField(initiator, "_triggerDistanceMeters", 2f);

            try
            {
                initiator.Tick();

                Assert.That(runtime.HasActiveConversation, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(npc);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void Tick_WhileConversationActive_DoesNotReopenConversation()
        {
            var playerRoot = new GameObject("PlayerRoot");
            playerRoot.transform.position = Vector3.zero;
            playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();

            var npc = new GameObject("NpcSpeaker");
            npc.transform.position = new Vector3(1.5f, 0f, 0f);
            var initiator = npc.AddComponent<DialogueProximityInitiator>();
            var definition = CreateDefinition(
                "dialogue.repeat",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Stay on this line.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.first", "First.", string.Empty, string.Empty, string.Empty),
                        new DialogueReplyDefinition("reply.second", "Second.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(initiator, "_definition", definition);
            SetField(initiator, "_playerTransformOverride", playerRoot.transform);
            SetField(initiator, "_triggerDistanceMeters", 2f);

            try
            {
                initiator.Tick();
                Assert.That(runtime.HasActiveConversation, Is.True);
                Assert.That(runtime.TrySelectReply(1), Is.True);

                initiator.Tick();

                Assert.That(runtime.ActiveConversation.SelectedReplyIndex, Is.EqualTo(1));
                Assert.That(runtime.ActiveConversation.SpeakerTransform, Is.SameAs(npc.transform));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(npc);
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
