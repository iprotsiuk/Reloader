using NUnit.Framework;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class PlayerNpcInteractionUiBridgeEditModeTests
    {
        [Test]
        public void PublishAndRequest_RoutesActionsResultAndExecuteEvent()
        {
            var go = new GameObject("npc-ui-bridge");
            var bridge = go.AddComponent<PlayerNpcInteractionUiBridge>();

            try
            {
                var actionsRaised = 0;
                var selectedActionKey = string.Empty;
                var executeRequestedKey = string.Empty;
                var executeRequestedPayload = string.Empty;
                NpcActionExecutionResult capturedResult = default;

                bridge.AvailableActionsChanged += (actions, selectedKey) =>
                {
                    actionsRaised++;
                    selectedActionKey = selectedKey;
                    Assert.That(actions.Count, Is.EqualTo(2));
                };

                bridge.ActionExecuted += result => capturedResult = result;
                bridge.ExecuteActionRequested += (actionKey, payload) =>
                {
                    executeRequestedKey = actionKey;
                    executeRequestedPayload = payload;
                };

                bridge.PublishAvailableActions(new[]
                {
                    new NpcActionDefinition("npc.action.dialogue", "Talk", 10),
                    new NpcActionDefinition("npc.action.trade", "Trade", 5, "vendor-1")
                }, "npc.action.trade");

                bridge.RequestExecuteAction("npc.action.trade", "vendor-1");
                bridge.PublishExecutionResult(new NpcActionExecutionResult("npc.action.trade", true, "trade.opened", "vendor-1"));

                Assert.That(actionsRaised, Is.EqualTo(1));
                Assert.That(selectedActionKey, Is.EqualTo("npc.action.trade"));
                Assert.That(executeRequestedKey, Is.EqualTo("npc.action.trade"));
                Assert.That(executeRequestedPayload, Is.EqualTo("vendor-1"));
                Assert.That(capturedResult.ActionKey, Is.EqualTo("npc.action.trade"));
                Assert.That(capturedResult.Success, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
