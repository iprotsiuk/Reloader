using NUnit.Framework;
using Reloader.UI.Toolkit.Reloading;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class ReloadingWorkbenchUiToolkitPlayModeTests
    {
        [Test]
        public void Render_UpdatesOperationSelectionAndResultText()
        {
            var root = BuildRoot();
            var binder = new ReloadingWorkbenchViewBinder();
            binder.Initialize(root, 3);

            binder.Render(ReloadingWorkbenchUiState.Create(new[]
            {
                new ReloadingWorkbenchUiState.OperationState(0, "Resize", true),
                new ReloadingWorkbenchUiState.OperationState(1, "Prime", false),
                new ReloadingWorkbenchUiState.OperationState(2, "Seat", false)
            }, "Operation complete"));

            var op0 = root.Q<VisualElement>("reloading__operation-0");
            var result = root.Q<Label>("reloading__result-label");

            Assert.That(op0.ClassListContains("is-selected"), Is.True);
            Assert.That(result.text, Is.EqualTo("Operation complete"));
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

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "reloading__root" };
            for (var i = 0; i < 3; i++)
            {
                root.Add(new VisualElement { name = $"reloading__operation-{i}" });
            }

            root.Add(new Button { name = "reloading__execute", text = "Execute" });
            root.Add(new Label { name = "reloading__result-label" });
            return root;
        }
    }
}
