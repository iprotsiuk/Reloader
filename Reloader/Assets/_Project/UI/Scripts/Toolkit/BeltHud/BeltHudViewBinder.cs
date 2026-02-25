using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.BeltHud
{
    public sealed class BeltHudViewBinder : IUiViewBinder
    {
        private VisualElement _root;
        private VisualElement[] _slotElements = Array.Empty<VisualElement>();
        private Label[] _slotLabels = Array.Empty<Label>();

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int slotCount)
        {
            _root = root;
            var safeCount = Math.Max(0, slotCount);
            _slotElements = new VisualElement[safeCount];
            _slotLabels = new Label[safeCount];

            for (var i = 0; i < safeCount; i++)
            {
                var capturedIndex = i;
                var slot = _root?.Q<VisualElement>($"belt-hud__slot-{i}");
                var label = _root?.Q<Label>($"belt-hud__slot-label-{i}");
                _slotElements[i] = slot;
                _slotLabels[i] = label;

                if (slot != null)
                {
                    slot.RegisterCallback<ClickEvent>(_ => TryRaiseSlotSelectIntent(capturedIndex));
                }
            }
        }

        public void Render(UiRenderState state)
        {
            if (state is not BeltHudUiState beltState)
            {
                return;
            }

            var limit = Math.Min(_slotElements.Length, beltState.Slots.Count);
            for (var i = 0; i < limit; i++)
            {
                var slotElement = _slotElements[i];
                if (slotElement == null)
                {
                    continue;
                }

                var slot = beltState.Slots[i];
                slotElement.EnableInClassList("is-occupied", slot.IsOccupied);
                slotElement.EnableInClassList("is-selected", slot.IsSelected);

                if (_slotLabels[i] != null)
                {
                    _slotLabels[i].text = (i + 1).ToString();
                }
            }
        }

        public bool TryRaiseSlotSelectIntent(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotElements.Length)
            {
                return false;
            }

            IntentRaised?.Invoke(new UiIntent("belt.slot.select", slotIndex));
            return true;
        }
    }
}
