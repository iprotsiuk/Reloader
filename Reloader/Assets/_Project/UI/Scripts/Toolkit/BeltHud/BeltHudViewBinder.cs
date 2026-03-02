using System;
using Reloader.Core.UI;
using Reloader.UI;
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
        private float _resolvedSlotSize = 46f;

        private const float SlotGap = 8f;
        private const float MinSlotSize = 22.5f;
        private const float MaxSlotSize = 40.5f;
        private const float MinHudWidth = 220f;
        private const float MaxHudWidth = 420f;

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

            RegisterResponsiveCallbacks();
            ApplyResponsiveLayout();
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

            ApplyResponsiveLayout();
        }

        private void RegisterResponsiveCallbacks()
        {
            _root?.RegisterCallback<AttachToPanelEvent>(_ => ApplyResponsiveLayout());
            _root?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _root?.schedule.Execute(ApplyResponsiveLayout).Every(250);
        }

        private void ApplyResponsiveLayout()
        {
            if (_root == null || _slotElements.Length == 0)
            {
                return;
            }

            var viewportWidth = _root.panel?.visualTree?.contentRect.width ?? 0f;
            var viewportHeight = _root.panel?.visualTree?.contentRect.height ?? 0f;
            if (viewportWidth <= 0f || viewportHeight <= 0f)
            {
                viewportWidth = Screen.width;
                viewportHeight = Screen.height;
            }

            if (viewportWidth <= 0f || viewportHeight <= 0f)
            {
                return;
            }

            var targetHudWidth = Mathf.Clamp(
                Mathf.Min(viewportWidth * 0.26f, viewportHeight * 0.56f),
                MinHudWidth,
                MaxHudWidth);
            var available = Mathf.Max(0f, targetHudWidth - 16f - ((_slotElements.Length - 1) * SlotGap));
            _resolvedSlotSize = Mathf.Clamp(available / Mathf.Max(1, _slotElements.Length), MinSlotSize, MaxSlotSize);

            var hudPadding = Mathf.Clamp(_resolvedSlotSize * 0.18f, 6f, 10f);
            var hudGap = Mathf.Clamp(_resolvedSlotSize * 0.15f, 4f, SlotGap);
            var hudOffset = Mathf.Clamp(viewportWidth * 0.015f, 12f, 28f);
            _root.style.left = hudOffset;
            _root.style.bottom = hudOffset;
            _root.style.paddingLeft = hudPadding;
            _root.style.paddingRight = hudPadding;
            _root.style.paddingTop = hudPadding;
            _root.style.paddingBottom = hudPadding;
            _root.style.width = (_resolvedSlotSize * _slotElements.Length) + ((_slotElements.Length - 1) * hudGap) + (hudPadding * 2f);
            for (var i = 0; i < _slotElements.Length; i++)
            {
                var slot = _slotElements[i];
                if (slot == null)
                {
                    continue;
                }

                slot.style.width = _resolvedSlotSize;
                slot.style.height = _resolvedSlotSize;
                slot.style.marginRight = i < _slotElements.Length - 1 ? hudGap : 0f;
            }

            var labelSize = Mathf.Clamp(_resolvedSlotSize * 0.3f, 10f, 14f);
            for (var i = 0; i < _slotLabels.Length; i++)
            {
                if (_slotLabels[i] != null)
                {
                    _slotLabels[i].style.fontSize = labelSize;
                }
            }
        }

        private static void RenderSlotItemVisual(VisualElement slotElement, BeltHudUiState.SlotState slot, int slotIndex)
        {
            var contentName = $"belt-hud__slot-item-{slotIndex}";
            var iconName = $"belt-hud__slot-item-icon-{slotIndex}";
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

            icon.style.backgroundImage = ItemIconResolver.ResolveBackground(slot.ItemId);
            icon.EnableInClassList("is-missing", false);
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
