using NUnit.Framework;
using Reloader.UI.Toolkit.CompassHud;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.EditMode
{
    public class CompassHudViewBinderEditModeTests
    {
        [Test]
        public void Render_WhenVisible_CreatesCardinalEntriesInSignedOrder()
        {
            var root = BuildRoot();
            var binder = new CompassHudViewBinder();
            binder.Initialize(root);

            binder.Render(CompassHudUiState.Create(
                new[]
                {
                    new CompassHudUiState.EntryState("W:-90", CompassHudUiState.EntryKind.Cardinal, "W", -90f),
                    new CompassHudUiState.EntryState("N:0", CompassHudUiState.EntryKind.Cardinal, "N", 0f),
                    new CompassHudUiState.EntryState("E:90", CompassHudUiState.EntryKind.Cardinal, "E", 90f)
                },
                visibleHalfAngleDegrees: 100f,
                isVisible: true));

            var entries = root.Q<VisualElement>("compass-hud__entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.childCount, Is.EqualTo(3));
            Assert.That(((Label)entries[0]).text, Is.EqualTo("W"));
            Assert.That(((Label)entries[1]).text, Is.EqualTo("N"));
            Assert.That(((Label)entries[2]).text, Is.EqualTo("E"));
            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void Render_WhenRepeatedCardinalsPresent_UsesEntryKeysInElementNames()
        {
            var root = BuildRoot();
            var binder = new CompassHudViewBinder();
            binder.Initialize(root);

            binder.Render(CompassHudUiState.Create(
                new[]
                {
                    new CompassHudUiState.EntryState("N:-360", CompassHudUiState.EntryKind.Cardinal, "N", -360f),
                    new CompassHudUiState.EntryState("N:0", CompassHudUiState.EntryKind.Cardinal, "N", 0f),
                    new CompassHudUiState.EntryState("N:360", CompassHudUiState.EntryKind.Cardinal, "N", 360f)
                },
                visibleHalfAngleDegrees: 400f,
                isVisible: true));

            var entries = root.Q<VisualElement>("compass-hud__entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.childCount, Is.EqualTo(3));
            Assert.That(entries[0].viewDataKey, Is.EqualTo("N:-360"));
            Assert.That(entries[1].viewDataKey, Is.EqualTo("N:0"));
            Assert.That(entries[2].viewDataKey, Is.EqualTo("N:360"));
        }

        [Test]
        public void Render_WhenMarkerEntryPresent_AddsMarkerVisualAtExpectedOffset()
        {
            var root = BuildRoot();
            var binder = new CompassHudViewBinder();
            binder.Initialize(root);

            binder.Render(CompassHudUiState.Create(
                new[]
                {
                    new CompassHudUiState.EntryState("N:0", CompassHudUiState.EntryKind.Cardinal, "N", 0f),
                    new CompassHudUiState.EntryState("marker:target", CompassHudUiState.EntryKind.Marker, "TARGET", 45f)
                },
                visibleHalfAngleDegrees: 90f,
                isVisible: true));

            var entries = root.Q<VisualElement>("compass-hud__entries");
            var marker = entries[1];
            Assert.That(marker, Is.Not.Null);
            Assert.That(marker.viewDataKey, Is.EqualTo("marker:target"));
            Assert.That(marker.ClassListContains("compass-hud__entry--marker"), Is.True);
            Assert.That(marker.style.left.value.value, Is.GreaterThan(130f));
        }

        [Test]
        public void Render_WhenMarkerEntryNotVisible_OmitsHiddenMarkerVisual()
        {
            var root = BuildRoot();
            var binder = new CompassHudViewBinder();
            binder.Initialize(root);

            binder.Render(CompassHudUiState.Create(
                new[]
                {
                    new CompassHudUiState.EntryState("N:0", CompassHudUiState.EntryKind.Cardinal, "N", 0f),
                    new CompassHudUiState.EntryState("marker:target", CompassHudUiState.EntryKind.Marker, "TARGET", 120f, isVisible: false)
                },
                visibleHalfAngleDegrees: 90f,
                isVisible: true));

            var entries = root.Q<VisualElement>("compass-hud__entries");
            Assert.That(entries.childCount, Is.EqualTo(1));
            Assert.That(entries[0].viewDataKey, Is.EqualTo("N:0"));
        }

        [Test]
        public void Render_WhenHidden_CollapsesRoot()
        {
            var root = BuildRoot();
            var binder = new CompassHudViewBinder();
            binder.Initialize(root);

            binder.Render(CompassHudUiState.Create(
                new[]
                {
                    new CompassHudUiState.EntryState("N:0", CompassHudUiState.EntryKind.Cardinal, "N", 0f)
                },
                visibleHalfAngleDegrees: 90f,
                isVisible: false));

            Assert.That(root.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "compass-hud__root" };
            var frame = new VisualElement { name = "compass-hud__frame" };
            var lane = new VisualElement { name = "compass-hud__lane" };
            var entries = new VisualElement { name = "compass-hud__entries" };
            lane.Add(entries);
            frame.Add(lane);
            root.Add(frame);
            return root;
        }
    }
}
