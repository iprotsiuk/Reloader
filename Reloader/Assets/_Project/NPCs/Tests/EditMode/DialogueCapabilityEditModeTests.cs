using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public sealed class DialogueCapabilityEditModeTests
    {
        [Test]
        public void CollectActions_HidesPoliceAction_WhenDialogueDefinitionIsMissing()
        {
            var go = new GameObject("police-agent");
            var agent = go.AddComponent<NpcAgent>();
            go.AddComponent<LawEnforcementInteractionCapability>();

            try
            {
                var actions = agent.CollectActions();

                Assert.That(actions.Count, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectActions_ExposesPoliceAction_WhenDialogueDefinitionIsValid()
        {
            var go = new GameObject("police-agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<LawEnforcementInteractionCapability>();
            var definition = CreateDefinition(
                "dialogue.police.stop",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Hands where I can see them.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.comply", "Easy.", string.Empty, "police.stop.comply", "comply")
                    }));
            SetField(capability, "_definition", definition);

            try
            {
                var actions = agent.CollectActions();

                Assert.That(actions.Count, Is.EqualTo(1));
                Assert.That(actions[0].ActionId, Is.EqualTo(LawEnforcementInteractionCapability.ActionKey));
                Assert.That(actions[0].DisplayName, Is.EqualTo("Question"));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectActions_HidesTalkAction_WhenDialogueDefinitionIsInvalid()
        {
            var go = new GameObject("dialogue-agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<DialogueCapability>();
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
            SetField(capability, "_definition", definition);

            try
            {
                var actions = agent.CollectActions();

                Assert.That(actions.Count, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectActions_ExposesTalkAction_WhenDialogueDefinitionIsValid()
        {
            var go = new GameObject("dialogue-agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<DialogueCapability>();
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
            SetField(capability, "_definition", definition);

            try
            {
                var actions = agent.CollectActions();

                Assert.That(actions.Count, Is.EqualTo(1));
                Assert.That(actions[0].ActionId, Is.EqualTo(DialogueCapability.ActionKey));
                Assert.That(actions[0].DisplayName, Is.EqualTo("Talk"));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TryExecuteAction_InvalidDefinitionReturnsFailureWithoutOpeningConversation()
        {
            var controllerGo = new GameObject("dialogue-runtime");
            var go = new GameObject("dialogue-agent");
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<DialogueCapability>();
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
            SetField(capability, "_definition", definition);

            try
            {
                var executed = agent.TryExecuteAction(DialogueCapability.ActionKey, "topic:greeting", out var result);

                Assert.That(executed, Is.False);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Reason, Is.EqualTo("dialogue.invalid-definition"));
                Assert.That(controller.HasActiveConversation, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(controllerGo);
            }
        }

        [Test]
        public void TryExecuteAction_WhenConversationAlreadyActive_ReturnsExplicitOverlapFailure()
        {
            var playerRoot = new GameObject("PlayerRoot");
            var requester = playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();

            var activeSpeaker = new GameObject("speaker-active");
            var activeDefinition = CreateDefinition(
                "dialogue.active",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Already talking.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.active", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));

            var go = new GameObject("dialogue-agent");
            go.transform.SetParent(playerRoot.transform);
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<DialogueCapability>();
            var definition = CreateDefinition(
                "dialogue.next",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Need something?",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.next", "Talk.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(capability, "_definition", definition);

            try
            {
                Assert.That(runtime.TryOpenConversation(activeDefinition, activeSpeaker.transform, out _), Is.True);

                var executed = agent.TryExecuteAction(DialogueCapability.ActionKey, string.Empty, out var result);

                Assert.That(executed, Is.False);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Reason, Is.EqualTo("dialogue.conversation-already-active"));
                Assert.That(runtime.ActiveConversation.Definition, Is.SameAs(activeDefinition));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(activeDefinition);
                Object.DestroyImmediate(activeSpeaker);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void TryExecuteAction_PoliceConversationAlreadyActive_ReturnsExplicitOverlapFailure()
        {
            var playerRoot = new GameObject("PlayerRoot");
            playerRoot.AddComponent<PlayerNpcInteractionController>();
            var runtime = playerRoot.AddComponent<DialogueRuntimeController>();

            var activeSpeaker = new GameObject("speaker-active");
            var activeDefinition = CreateDefinition(
                "dialogue.active",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Already talking.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.active", "Continue.", string.Empty, string.Empty, string.Empty)
                    }));

            var go = new GameObject("police-agent");
            go.transform.SetParent(playerRoot.transform);
            var agent = go.AddComponent<NpcAgent>();
            var capability = go.AddComponent<LawEnforcementInteractionCapability>();
            var definition = CreateDefinition(
                "dialogue.police.stop",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Hold it.",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.comply", "Easy.", string.Empty, "police.stop.comply", "comply")
                    }));
            SetField(capability, "_definition", definition);

            try
            {
                Assert.That(runtime.TryOpenConversation(activeDefinition, activeSpeaker.transform, out _), Is.True);

                var executed = agent.TryExecuteAction(LawEnforcementInteractionCapability.ActionKey, string.Empty, out var result);

                Assert.That(executed, Is.False);
                Assert.That(result.Success, Is.False);
                Assert.That(result.Reason, Is.EqualTo("dialogue.conversation-already-active"));
                Assert.That(runtime.ActiveConversation.Definition, Is.SameAs(activeDefinition));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(activeDefinition);
                Object.DestroyImmediate(activeSpeaker);
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
