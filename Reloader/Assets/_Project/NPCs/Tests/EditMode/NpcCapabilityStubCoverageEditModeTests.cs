using NUnit.Framework;
using System.Reflection;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.Runtime.Dialogue;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class NpcCapabilityStubCoverageEditModeTests
    {
        [Test]
        public void CollectActions_AggregatesStubCapabilityActions()
        {
            var go = new GameObject("stub-agent");
            var agent = go.AddComponent<NpcAgent>();
            var dialogueCapability = go.AddComponent<DialogueCapability>();
            go.AddComponent<QuestGiverCapability>();
            var lawEnforcementCapability = go.AddComponent<LawEnforcementInteractionCapability>();
            go.AddComponent<FrontDeskInteractionCapability>();
            var definition = CreateDefinition(
                "dialogue.stub",
                "entry",
                new DialogueNodeDefinition(
                    "entry",
                    "Need something?",
                    new[]
                    {
                        new DialogueReplyDefinition("reply.ack", "No.", string.Empty, string.Empty, string.Empty)
                    }));
            SetField(dialogueCapability, "_definition", definition);
            SetField(lawEnforcementCapability, "_definition", definition);

            try
            {
                var actions = agent.CollectActions();

                Assert.That(actions.Count, Is.EqualTo(4));
                Assert.That(actions[0].ActionKey, Is.EqualTo(DialogueCapability.ActionKey));
                Assert.That(actions[1].ActionKey, Is.EqualTo(QuestGiverCapability.ActionKey));
                Assert.That(actions[2].ActionKey, Is.EqualTo(LawEnforcementInteractionCapability.ActionKey));
                Assert.That(actions[3].ActionKey, Is.EqualTo(FrontDeskInteractionCapability.ActionKey));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InitializeAndDisable_DoesNotThrowForStubCapabilities()
        {
            var go = new GameObject("stub-agent-lifecycle");
            var agent = go.AddComponent<NpcAgent>();
            go.AddComponent<EntryFeeInteractionCapability>();
            go.AddComponent<PatrolCapability>();
            go.AddComponent<FollowCapability>();
            go.AddComponent<AmbientCitizenCapability>();
            go.AddComponent<JobScheduleCapability>();

            try
            {
                Assert.DoesNotThrow(() =>
                {
                    agent.InitializeCapabilities();
                    go.SetActive(false);
                });
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
