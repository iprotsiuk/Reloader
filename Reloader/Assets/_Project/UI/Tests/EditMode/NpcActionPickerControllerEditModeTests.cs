using System.Collections.Generic;
using NUnit.Framework;
using Reloader.NPCs.Runtime;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.NpcActionPicker;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.EditMode
{
    public class NpcActionPickerControllerEditModeTests
    {
        [Test]
        public void HandleIntent_SelectAndExecute_UpdatesSelectionAndRequestsExecution()
        {
            var go = new GameObject("npc-action-picker-controller");
            var controller = go.AddComponent<NpcActionPickerController>();
            var bridge = new FakeBridge();
            controller.SetBridge(bridge);

            var root = BuildRoot();
            var binder = new NpcActionPickerViewBinder();
            binder.Initialize(root);
            controller.SetViewBinder(binder);

            try
            {
                bridge.PublishActions(new[]
                {
                    new NpcActionDefinition("npc.action.dialogue", "Talk", 10),
                    new NpcActionDefinition("npc.action.trade", "Trade", 5, "vendor-001")
                }, string.Empty);

                controller.HandleIntent(new UiIntent("npc.action.select", 1));

                string requestedActionKey = null;
                string requestedPayload = null;
                bridge.ExecuteActionRequested += (actionKey, payload) =>
                {
                    requestedActionKey = actionKey;
                    requestedPayload = payload;
                };

                controller.HandleIntent(new UiIntent("npc.action.execute"));
                bridge.PublishResult(new NpcActionExecutionResult("npc.action.trade", true, "trade.opened", "vendor-001"));

                var selectedLabel = root.Q<Label>("npc-actions__selected");
                var resultLabel = root.Q<Label>("npc-actions__result");

                Assert.That(selectedLabel.text, Is.EqualTo("Trade"));
                Assert.That(resultLabel.text, Is.EqualTo("trade.opened"));
                Assert.That(requestedActionKey, Is.EqualTo("npc.action.trade"));
                Assert.That(requestedPayload, Is.EqualTo("vendor-001"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "npc-actions__root" };
            root.Add(new VisualElement { name = "npc-actions__list" });
            root.Add(new Label { name = "npc-actions__selected" });
            root.Add(new Label { name = "npc-actions__result" });
            root.Add(new Button { name = "npc-actions__execute", text = "Execute" });
            return root;
        }

        private sealed class FakeBridge : INpcActionPickerBridge
        {
            public event System.Action<IReadOnlyList<NpcActionDefinition>, string> AvailableActionsChanged;
            public event System.Action<NpcActionExecutionResult> ActionExecuted;
            public event System.Action<string, string> ExecuteActionRequested;

            public void RequestExecuteAction(string actionKey, string payload)
            {
                ExecuteActionRequested?.Invoke(actionKey, payload);
            }

            public void PublishActions(IReadOnlyList<NpcActionDefinition> actions, string selectedActionKey)
            {
                AvailableActionsChanged?.Invoke(actions, selectedActionKey);
            }

            public void PublishResult(NpcActionExecutionResult result)
            {
                ActionExecuted?.Invoke(result);
            }
        }
    }
}
