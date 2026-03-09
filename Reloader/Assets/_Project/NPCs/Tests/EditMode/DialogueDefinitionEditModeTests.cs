using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class DialogueDefinitionEditModeTests
    {
        private const string FrontDeskDialogueAssetPath = "Assets/_Project/NPCs/Data/Definitions/Dialogue_FrontDeskClerk.asset";
        private const string FrontDeskClerkPrefabPath = "Assets/_Project/NPCs/Prefabs/Roles/Npc_FrontDeskClerk.prefab";
        private const string PoliceStopDialogueAssetPath = "Assets/_Project/NPCs/Data/Definitions/Dialogue_PoliceStop.asset";
        private const string PolicePrefabPath = "Assets/_Project/NPCs/Prefabs/Roles/Npc_Police.prefab";

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

#if UNITY_EDITOR
        [Test]
        public void FrontDeskDialogueAsset_IsValidAndAuthoredForSingleNodeGreeting()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(FrontDeskDialogueAssetPath);

            Assert.That(asset, Is.Not.Null, $"Expected dialogue asset at {FrontDeskDialogueAssetPath}.");
            Assert.That(asset.IsValid(out var reason), Is.True, reason);
            Assert.That(asset.DialogueId, Is.EqualTo("dialogue.frontdesk.greeting"));
            Assert.That(asset.TryGetEntryNode(out var entryNode), Is.True);
            Assert.That(entryNode, Is.Not.Null);
            Assert.That(entryNode.SpeakerText, Does.Contain("range"));
            Assert.That(entryNode.Replies.Length, Is.EqualTo(3));
        }

        [Test]
        public void FrontDeskClerkPrefab_HasDialogueCapabilityBoundToAuthoredDialogueAsset()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FrontDeskClerkPrefabPath);
            var dialogueAsset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(FrontDeskDialogueAssetPath);

            Assert.That(prefab, Is.Not.Null, $"Expected prefab at {FrontDeskClerkPrefabPath}.");
            Assert.That(dialogueAsset, Is.Not.Null, $"Expected dialogue asset at {FrontDeskDialogueAssetPath}.");

            var capability = prefab.GetComponent<DialogueCapability>();
            Assert.That(capability, Is.Not.Null, "Front desk clerk should expose the shared dialogue capability.");
            Assert.That(capability.Definition, Is.SameAs(dialogueAsset));
        }

        [Test]
        public void PoliceStopDialogueAsset_IsValidAndExposesExpectedOutcomeIds()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(PoliceStopDialogueAssetPath);

            Assert.That(asset, Is.Not.Null, $"Expected dialogue asset at {PoliceStopDialogueAssetPath}.");
            Assert.That(asset.IsValid(out var reason), Is.True, reason);
            Assert.That(asset.DialogueId, Is.EqualTo("dialogue.police.stop"));
            Assert.That(asset.TryGetEntryNode(out var entryNode), Is.True);
            Assert.That(entryNode, Is.Not.Null);
            Assert.That(entryNode.Replies.Length, Is.EqualTo(3));
            Assert.That(entryNode.Replies[0].OutcomeActionId, Is.EqualTo("police.stop.comply"));
            Assert.That(entryNode.Replies[1].OutcomeActionId, Is.EqualTo("police.stop.question"));
            Assert.That(entryNode.Replies[2].OutcomeActionId, Is.EqualTo("police.stop.leave"));
        }

        [Test]
        public void PolicePrefab_HasLawEnforcementCapabilityBoundToPoliceStopDialogueAsset()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PolicePrefabPath);
            var dialogueAsset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(PoliceStopDialogueAssetPath);

            Assert.That(prefab, Is.Not.Null, $"Expected prefab at {PolicePrefabPath}.");
            Assert.That(dialogueAsset, Is.Not.Null, $"Expected dialogue asset at {PoliceStopDialogueAssetPath}.");

            var capability = prefab.GetComponent<LawEnforcementInteractionCapability>();
            Assert.That(capability, Is.Not.Null, "Police prefab should expose the law-enforcement interaction capability.");

            var definitionField = typeof(LawEnforcementInteractionCapability).GetField("_definition", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(definitionField, Is.Not.Null, "Expected private dialogue definition field on LawEnforcementInteractionCapability.");
            Assert.That(definitionField.GetValue(capability), Is.SameAs(dialogueAsset));
        }
#endif

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
