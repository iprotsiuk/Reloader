using NUnit.Framework;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
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
            go.AddComponent<DialogueCapability>();
            go.AddComponent<QuestGiverCapability>();
            go.AddComponent<LawEnforcementInteractionCapability>();
            go.AddComponent<FrontDeskInteractionCapability>();

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
    }
}
