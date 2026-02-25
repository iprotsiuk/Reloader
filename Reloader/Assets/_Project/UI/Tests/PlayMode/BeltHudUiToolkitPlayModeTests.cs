using NUnit.Framework;
using Reloader.UI.Toolkit.BeltHud;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class BeltHudUiToolkitPlayModeTests
    {
        [Test]
        public void Render_AppliesOccupiedAndSelectedClasses()
        {
            var root = BuildRoot();
            var binder = new BeltHudViewBinder();
            binder.Initialize(root, 5);

            var state = BeltHudUiState.Create(new[]
            {
                new BeltHudUiState.SlotState(0, "item-a", true, false),
                new BeltHudUiState.SlotState(1, null, false, true),
                new BeltHudUiState.SlotState(2, null, false, false),
                new BeltHudUiState.SlotState(3, null, false, false),
                new BeltHudUiState.SlotState(4, null, false, false)
            });

            binder.Render(state);

            var slot0 = root.Q<VisualElement>("belt-hud__slot-0");
            var slot1 = root.Q<VisualElement>("belt-hud__slot-1");

            Assert.That(slot0.ClassListContains("is-occupied"), Is.True);
            Assert.That(slot0.ClassListContains("is-selected"), Is.False);
            Assert.That(slot1.ClassListContains("is-selected"), Is.True);
        }

        [Test]
        public void Render_UpdatesSelectionWhenStateChanges()
        {
            var root = BuildRoot();
            var binder = new BeltHudViewBinder();
            binder.Initialize(root, 5);

            binder.Render(BeltHudUiState.Create(new[]
            {
                new BeltHudUiState.SlotState(0, null, false, true),
                new BeltHudUiState.SlotState(1, null, false, false),
                new BeltHudUiState.SlotState(2, null, false, false),
                new BeltHudUiState.SlotState(3, null, false, false),
                new BeltHudUiState.SlotState(4, null, false, false)
            }));

            binder.Render(BeltHudUiState.Create(new[]
            {
                new BeltHudUiState.SlotState(0, null, false, false),
                new BeltHudUiState.SlotState(1, null, false, true),
                new BeltHudUiState.SlotState(2, null, false, false),
                new BeltHudUiState.SlotState(3, null, false, false),
                new BeltHudUiState.SlotState(4, null, false, false)
            }));

            var slot0 = root.Q<VisualElement>("belt-hud__slot-0");
            var slot1 = root.Q<VisualElement>("belt-hud__slot-1");

            Assert.That(slot0.ClassListContains("is-selected"), Is.False);
            Assert.That(slot1.ClassListContains("is-selected"), Is.True);
        }

        [Test]
        public void TryRaiseSlotSelectIntent_EmitsIntentWithSlotIndex()
        {
            var root = BuildRoot();
            var binder = new BeltHudViewBinder();
            binder.Initialize(root, 5);

            var captured = -1;
            binder.IntentRaised += intent =>
            {
                if (intent.Payload is int index)
                {
                    captured = index;
                }
            };

            var raised = binder.TryRaiseSlotSelectIntent(3);

            Assert.That(raised, Is.True);
            Assert.That(captured, Is.EqualTo(3));
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "belt-hud__root" };
            for (var i = 0; i < 5; i++)
            {
                var slot = new VisualElement { name = $"belt-hud__slot-{i}" };
                var label = new Label((i + 1).ToString()) { name = $"belt-hud__slot-label-{i}" };
                slot.Add(label);
                root.Add(slot);
            }

            return root;
        }
    }
}
