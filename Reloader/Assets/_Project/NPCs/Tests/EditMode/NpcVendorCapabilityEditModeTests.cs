using NUnit.Framework;
using Reloader.NPCs.Runtime;
using Reloader.NPCs.Runtime.Capabilities;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class NpcVendorCapabilityEditModeTests
    {
        [Test]
        public void CollectActions_ExposesVendorTradeActionWithVendorPayload()
        {
            var go = new GameObject("vendor-agent");
            var agent = go.AddComponent<NpcAgent>();
            var target = go.AddComponent<ShopVendorTarget>();
            go.AddComponent<VendorTradeCapability>();

            JsonUtility.FromJsonOverwrite("{\"_vendorId\":\"vendor-ammo-01\"}", target);

            try
            {
                var actions = agent.CollectActions();

                Assert.That(actions.Count, Is.EqualTo(1));
                Assert.That(actions[0].ActionKey, Is.EqualTo(VendorTradeCapability.ActionKey));
                Assert.That(actions[0].Payload, Is.EqualTo("vendor-ammo-01"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CollectActions_WhenVendorTargetReplaced_ReResolvesPayload()
        {
            var go = new GameObject("vendor-agent");
            var agent = go.AddComponent<NpcAgent>();
            var firstTarget = go.AddComponent<ShopVendorTarget>();
            go.AddComponent<VendorTradeCapability>();
            JsonUtility.FromJsonOverwrite("{\"_vendorId\":\"vendor-ammo-01\"}", firstTarget);

            try
            {
                var initialActions = agent.CollectActions();
                Object.DestroyImmediate(firstTarget);

                var replacementTarget = go.AddComponent<ShopVendorTarget>();
                JsonUtility.FromJsonOverwrite("{\"_vendorId\":\"vendor-ammo-02\"}", replacementTarget);

                var updatedActions = agent.CollectActions();

                Assert.That(initialActions.Count, Is.EqualTo(1));
                Assert.That(initialActions[0].Payload, Is.EqualTo("vendor-ammo-01"));
                Assert.That(updatedActions.Count, Is.EqualTo(1));
                Assert.That(updatedActions[0].Payload, Is.EqualTo("vendor-ammo-02"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
