using NUnit.Framework;
using Reloader.UI.Toolkit.Reloading;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class ReloadingWorkbenchUiToolkitPlayModeTests
    {
        [Test]
        public void Render_SetupMode_ShowsSetupPanelAndDiagnostics()
        {
            var root = BuildRoot();
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, 3);

            binder.Render(ReloadingWorkbenchUiState.Create(
                mode: ReloadingWorkbenchMode.Setup,
                operations: new[]
                {
                    new ReloadingWorkbenchUiState.OperationState(0, "Resize", true, true, string.Empty),
                    new ReloadingWorkbenchUiState.OperationState(1, "Prime", false, false, "Missing die"),
                    new ReloadingWorkbenchUiState.OperationState(2, "Seat", false, false, "Missing seat die")
                },
                setupSlotsText: "Press Slot: Empty",
                setupDiagnosticsText: "Install a press to continue.",
                operateDiagnosticsText: string.Empty,
                resultText: string.Empty));

            var setupPanel = root.Q<VisualElement>("reloading__setup-panel");
            var operatePanel = root.Q<VisualElement>("reloading__operate-panel");
            var setupDiagnostics = root.Q<Label>("reloading__setup-diagnostics");
            var setupButton = root.Q<Button>("reloading__mode-setup");
            var operateButton = root.Q<Button>("reloading__mode-operate");

            Assert.That(setupPanel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(operatePanel.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(setupDiagnostics.text, Is.EqualTo("Install a press to continue."));
            Assert.That(setupButton.ClassListContains("is-selected"), Is.True);
            Assert.That(operateButton.ClassListContains("is-selected"), Is.False);
        }

        [Test]
        public void Render_OperateMode_ShowsDisabledOperationDiagnosticsAndDisablesExecute()
        {
            var root = BuildRoot();
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, 3);

            binder.Render(ReloadingWorkbenchUiState.Create(
                mode: ReloadingWorkbenchMode.Operate,
                operations: new[]
                {
                    new ReloadingWorkbenchUiState.OperationState(0, "Resize", false, true, string.Empty),
                    new ReloadingWorkbenchUiState.OperationState(1, "Prime", true, false, "Missing die"),
                    new ReloadingWorkbenchUiState.OperationState(2, "Seat", false, false, "Missing seat die")
                },
                setupSlotsText: "Press Slot: Installed",
                setupDiagnosticsText: string.Empty,
                operateDiagnosticsText: "Prime blocked: Missing die",
                resultText: "Operation blocked"));

            var op1 = root.Q<VisualElement>("reloading__operation-1");
            var operatePanel = root.Q<VisualElement>("reloading__operate-panel");
            var executeButton = root.Q<Button>("reloading__execute");
            var diagnostics = root.Q<Label>("reloading__operate-diagnostics");
            var result = root.Q<Label>("reloading__result-label");
            var label = root.Q<Label>("reloading__operation-label-1");

            Assert.That(operatePanel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(op1.ClassListContains("is-selected"), Is.True);
            Assert.That(op1.ClassListContains("is-disabled"), Is.True);
            Assert.That(executeButton.enabledSelf, Is.False);
            Assert.That(diagnostics.text, Is.EqualTo("Prime blocked: Missing die"));
            Assert.That(result.text, Is.EqualTo("Operation blocked"));
            Assert.That(label.text, Is.EqualTo("Prime"));
        }

        [Test]
        public void TryRaiseSelectOperationIntent_EmitsOperationIndex()
        {
            var root = BuildRoot();
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, 3);

            var captured = -1;
            binder.IntentRaised += intent =>
            {
                if (intent.Payload is int index)
                {
                    captured = index;
                }
            };

            var raised = binder.TryRaiseSelectOperationIntent(2);

            Assert.That(raised, Is.True);
            Assert.That(captured, Is.EqualTo(2));
        }

        [Test]
        public void TryRaiseExecuteIntent_EmitsExecuteKey()
        {
            var root = BuildRoot();
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, 3);

            string captured = null;
            binder.IntentRaised += intent => captured = intent.Key;

            var raised = binder.TryRaiseExecuteIntent();

            Assert.That(raised, Is.True);
            Assert.That(captured, Is.EqualTo("reloading.operation.execute"));
        }

        [Test]
        public void TryRaiseSetupModeIntent_EmitsModeIntentKey()
        {
            var root = BuildRoot();
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, 3);

            string captured = null;
            binder.IntentRaised += intent => captured = intent.Key;

            var raised = binder.TryRaiseSetupModeIntent();

            Assert.That(raised, Is.True);
            Assert.That(captured, Is.EqualTo("reloading.mode.setup"));
        }

        [Test]
        public void TryRaiseOperateModeIntent_EmitsModeIntentKey()
        {
            var root = BuildRoot();
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, 3);

            string captured = null;
            binder.IntentRaised += intent => captured = intent.Key;

            var raised = binder.TryRaiseOperateModeIntent();

            Assert.That(raised, Is.True);
            Assert.That(captured, Is.EqualTo("reloading.mode.operate"));
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "reloading__root" };
            root.Add(new Button { name = "reloading__mode-setup", text = "Setup" });
            root.Add(new Button { name = "reloading__mode-operate", text = "Operate" });
            root.Add(new VisualElement { name = "reloading__setup-panel" });
            root.Add(new VisualElement { name = "reloading__operate-panel" });
            root.Add(new Label { name = "reloading__setup-slots" });
            root.Add(new Label { name = "reloading__setup-diagnostics" });
            root.Add(new Label { name = "reloading__operate-diagnostics" });
            for (var i = 0; i < 3; i++)
            {
                root.Add(new VisualElement { name = $"reloading__operation-{i}" });
                root.Add(new Label { name = $"reloading__operation-label-{i}" });
            }

            root.Add(new Button { name = "reloading__execute", text = "Execute" });
            root.Add(new Label { name = "reloading__result-label" });
            return root;
        }
    }
}
