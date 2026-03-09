using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class DialogueDefinitionEditModeTests
    {
        [Test]
        public void IsValid_ReturnsTrue_ForSingleNodeTerminalConversation()
        {
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

            try
            {
                var valid = definition.IsValid(out var reason);

                Assert.That(valid, Is.True);
                Assert.That(reason, Is.EqualTo(string.Empty));
                Assert.That(definition.TryGetEntryNode(out var entryNode), Is.True);
                Assert.That(entryNode.NodeId, Is.EqualTo("entry"));
                Assert.That(entryNode.Replies.Length, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void IsValid_ReturnsFalse_WhenEntryNodeIsMissing()
        {
            var definition = CreateDefinition(
                "dialogue.invalid-entry",
                "missing",
                new DialogueNodeDefinition(
                    "entry",
                    "You should not see this.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.leave", "Leave.", string.Empty, string.Empty, string.Empty)
                    }));

            try
            {
                var valid = definition.IsValid(out var reason);

                Assert.That(valid, Is.False);
                Assert.That(reason, Is.EqualTo("dialogue.definition.missing-entry-node"));
                Assert.That(definition.TryGetEntryNode(out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void IsValid_ReturnsFalse_WhenReplyReferencesUnknownNextNode()
        {
            var definition = CreateDefinition(
                "dialogue.invalid-next",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Pick one.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.next", "Continue.", "missing", string.Empty, string.Empty)
                    }));

            try
            {
                var valid = definition.IsValid(out var reason);

                Assert.That(valid, Is.False);
                Assert.That(reason, Is.EqualTo("dialogue.definition.invalid-next-node"));
            }
            finally
            {
                Object.DestroyImmediate(definition);
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
