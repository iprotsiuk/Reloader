using NUnit.Framework;
using Reloader.UI.Toolkit.Runtime;
using UnityEditor;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.EditMode
{
    public class DialogueOverlayUxmlCopyEditModeTests
    {
        private const string DialogueOverlayUxmlPath = "Assets/_Project/UI/Toolkit/UXML/DialogueOverlay.uxml";

        [Test]
        public void DialogueOverlay_AuthorsSpeakerLineAndRepliesShell()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DialogueOverlayUxmlPath);
            Assert.That(asset, Is.Not.Null, $"Expected UXML asset at '{DialogueOverlayUxmlPath}'.");

            var root = asset.CloneTree();
            Assert.That(root.Q<VisualElement>("dialogue-overlay__root"), Is.Not.Null);
            Assert.That(root.Q<Label>("dialogue-overlay__speaker"), Is.Not.Null);
            Assert.That(root.Q<Label>("dialogue-overlay__line"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("dialogue-overlay__replies"), Is.Not.Null);
        }

        [Test]
        public void DialogueOverlay_CompositionAndActionMapContainDialogueScreen()
        {
            var composition = UiScreenCompositionConfig.CreateWithDefaults();
            var resolvedScreen = composition.TryGetComponents(UiRuntimeCompositionIds.ScreenIds.DialogueOverlay, out var components);

            Assert.That(resolvedScreen, Is.True);
            Assert.That(components, Is.Not.Null);
            Assert.That(components.Count, Is.GreaterThan(0));

            var actionMap = UiActionMapConfig.CreateWithDefaults();
            Assert.That(actionMap.TryResolve(UiRuntimeCompositionIds.IntentKeys.DialogueReplyPrevious, out var previousCommand), Is.True);
            Assert.That(actionMap.TryResolve(UiRuntimeCompositionIds.IntentKeys.DialogueReplyNext, out var nextCommand), Is.True);
            Assert.That(actionMap.TryResolve(UiRuntimeCompositionIds.IntentKeys.DialogueReplySubmit, out var submitCommand), Is.True);
            Assert.That(actionMap.TryResolve(UiRuntimeCompositionIds.IntentKeys.DialogueReplySelect, out var selectCommand), Is.True);
            Assert.That(string.IsNullOrWhiteSpace(previousCommand), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(nextCommand), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(submitCommand), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(selectCommand), Is.False);
        }
    }
}
