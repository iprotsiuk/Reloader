using NUnit.Framework;
using Reloader.NPCs.Data;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class NpcContractsAndDataEditModeTests
    {
        [Test]
        public void CapabilityContracts_ExposeExpectedCoreSurface()
        {
            var action = new NpcActionDefinition("trade.open", "Open Trade", 10);
            var agentGo = new GameObject("npc-agent");
            var agent = agentGo.AddComponent<NpcAgent>();
            var provider = agentGo.AddComponent<TestActionCapability>();
            provider.Actions = new[] { action };

            try
            {
                agent.InitializeCapabilities();

                Assert.That(provider.InitializeCount, Is.EqualTo(1));
                Assert.That(provider.CapabilityKind, Is.EqualTo(NpcCapabilityKind.VendorTrade));
                Assert.That(provider.GetActions()[0].ActionId, Is.EqualTo("trade.open"));
            }
            finally
            {
                Object.DestroyImmediate(agentGo);
            }
        }

        [Test]
        public void DataDefinitions_ExposeRoleAndCapabilityConfiguration()
        {
            var rolePreset = ScriptableObject.CreateInstance<NpcRolePreset>();
            var vendorCapability = ScriptableObject.CreateInstance<NpcCapabilityConfig>();
            var npcDefinition = ScriptableObject.CreateInstance<NpcDefinition>();

            JsonUtility.FromJsonOverwrite("{\"_kind\":1}", vendorCapability);
            JsonUtility.FromJsonOverwrite("{\"_roleKind\":1}", rolePreset);
            JsonUtility.FromJsonOverwrite("{\"_npcId\":\"npc.vendor.001\"}", npcDefinition);

            var rolePresetField = typeof(NpcDefinition).GetField("_rolePreset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(rolePresetField, Is.Not.Null);
            rolePresetField.SetValue(npcDefinition, rolePreset);
            var capabilitiesField = typeof(NpcRolePreset).GetField("_capabilities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(capabilitiesField, Is.Not.Null);
            capabilitiesField.SetValue(rolePreset, new[] { vendorCapability });

            Assert.That(rolePreset.RoleKind, Is.EqualTo(NpcRoleKind.Shopkeeper));
            Assert.That(rolePreset.Capabilities.Length, Is.EqualTo(1));
            Assert.That(rolePreset.Capabilities[0].Kind, Is.EqualTo(NpcCapabilityKind.VendorTrade));
            Assert.That(npcDefinition.NpcId, Is.EqualTo("npc.vendor.001"));
            Assert.That(npcDefinition.RolePreset, Is.SameAs(rolePreset));

            Object.DestroyImmediate(rolePreset);
            Object.DestroyImmediate(vendorCapability);
            Object.DestroyImmediate(npcDefinition);
        }

        private sealed class TestActionCapability : MonoBehaviour, INpcCapability, INpcActionProvider
        {
            public NpcActionDefinition[] Actions { get; set; }
            public int InitializeCount { get; private set; }
            public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.VendorTrade;

            public void Initialize(NpcAgent agent)
            {
                InitializeCount++;
            }

            public void Shutdown()
            {
            }

            public NpcActionDefinition[] GetActions()
            {
                return Actions;
            }
        }
    }
}
