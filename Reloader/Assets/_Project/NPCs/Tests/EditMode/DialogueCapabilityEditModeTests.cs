using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
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
