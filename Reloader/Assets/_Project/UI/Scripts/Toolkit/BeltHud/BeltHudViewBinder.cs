using System;
using Reloader.Core.UI;
using UnityEngine;
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
                RenderSlotItemVisual(slotElement, slot, i);

                if (_slotLabels[i] != null)
                {
                    _slotLabels[i].text = (i + 1).ToString();
                }
            }
        }

        private static void RenderSlotItemVisual(VisualElement slotElement, BeltHudUiState.SlotState slot, int slotIndex)
        {
            var contentName = $"belt-hud__slot-item-{slotIndex}";
            var iconName = $"belt-hud__slot-item-icon-{slotIndex}";
            var labelName = $"belt-hud__slot-item-name-{slotIndex}";
            var quantityName = $"belt-hud__slot-item-quantity-{slotIndex}";
            var hasVisual = slot.IsOccupied && !string.IsNullOrWhiteSpace(slot.ItemId);
            var content = slotElement.Q<VisualElement>(contentName);

            if (!hasVisual)
            {
                content?.RemoveFromHierarchy();
                return;
            }

            if (content == null)
            {
                content = new VisualElement
                {
                    name = contentName,
                    pickingMode = PickingMode.Ignore
                };
                content.AddToClassList("belt-hud__slot-item");
                slotElement.Add(content);
            }

            var icon = content.Q<VisualElement>(iconName);
            if (icon == null)
            {
                icon = new VisualElement
                {
                    name = iconName,
                    pickingMode = PickingMode.Ignore
                };
                icon.AddToClassList("belt-hud__slot-item-icon");
                content.Add(icon);
            }

            var label = content.Q<Label>(labelName);
            if (label == null)
            {
                label = new Label
                {
                    name = labelName,
                    pickingMode = PickingMode.Ignore
                };
                label.AddToClassList("belt-hud__slot-item-name");
                content.Add(label);
            }

            var quantityLabel = content.Q<Label>(quantityName);
            if (quantityLabel == null)
            {
                quantityLabel = new Label
                {
                    name = quantityName,
                    pickingMode = PickingMode.Ignore
                };
                quantityLabel.AddToClassList("belt-hud__slot-item-quantity");
                content.Add(quantityLabel);
            }

            if (ItemIconCatalogProvider.Catalog != null && ItemIconCatalogProvider.Catalog.TryGetIcon(slot.ItemId, out var iconSprite))
            {
                icon.style.backgroundImage = new StyleBackground(iconSprite);
                icon.EnableInClassList("is-missing", false);
            }
            else
            {
                icon.style.backgroundImage = new StyleBackground((Texture2D)null);
                icon.EnableInClassList("is-missing", true);
            }

            label.text = ItemDisplayNameFormatter.Format(slot.ItemId);
            var showQuantity = slot.Quantity > 1;
            quantityLabel.style.display = showQuantity ? DisplayStyle.Flex : DisplayStyle.None;
            quantityLabel.text = showQuantity ? slot.Quantity.ToString() : string.Empty;
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
