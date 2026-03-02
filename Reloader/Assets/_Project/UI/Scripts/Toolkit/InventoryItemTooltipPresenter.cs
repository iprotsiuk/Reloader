using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit
{
    public sealed class InventoryItemTooltipPresenter
    {
        private const float OffsetX = 22f; // Matches interaction-hint horizontal nudge
        private const float OffsetY = 18f; // Matches interaction-hint vertical nudge

        private readonly VisualElement _tooltip;
        private readonly Label _title;
        private readonly Label _specs;
        private readonly VisualElement _positionRoot;
        private readonly InventoryTooltipService _tooltipService;

        private InventoryItemTooltipPresenter(
            VisualElement tooltip,
            Label title,
            Label specs,
            VisualElement positionRoot,
            InventoryTooltipService tooltipService)
        {
            _tooltip = tooltip;
            _title = title;
            _specs = specs;
            _positionRoot = positionRoot;
            _tooltipService = tooltipService;
        }

        public static InventoryItemTooltipPresenter CreateOrBind(
            VisualElement root,
            VisualElement positionRoot,
            string blockPrefix)
        {
            if (root == null)
            {
                return null;
            }

            var tooltipName = $"{blockPrefix}__tooltip";
            var titleName = $"{blockPrefix}__tooltip-title";
            var specsName = $"{blockPrefix}__tooltip-specs";
            var host = positionRoot ?? root;

            var tooltip = root.Q<VisualElement>(tooltipName);

            if (tooltip == null)
            {
                tooltip = new VisualElement { name = tooltipName };
                tooltip.AddToClassList($"{blockPrefix}__tooltip");
                host.Add(tooltip);
            }
            else if (tooltip.parent != host)
            {
                tooltip.RemoveFromHierarchy();
                host.Add(tooltip);
            }

            tooltip.pickingMode = PickingMode.Ignore;
            var title = tooltip.Q<Label>(titleName);
            var specs = tooltip.Q<Label>(specsName);

            if (title == null)
            {
                title = new Label { name = titleName };
                title.AddToClassList($"{blockPrefix}__tooltip-title");
                tooltip.Add(title);
            }

            if (specs == null)
            {
                specs = new Label { name = specsName };
                specs.AddToClassList($"{blockPrefix}__tooltip-specs");
                tooltip.Add(specs);
            }

            tooltip.style.display = DisplayStyle.None;
            return new InventoryItemTooltipPresenter(
                tooltip,
                title,
                specs,
                host,
                new InventoryTooltipService());
        }

        public bool TryShowItem(
            string itemId,
            int quantity,
            int maxStack,
            Vector2 pointerWorldPosition,
            string specsOverride = null)
        {
            if (_tooltip == null || _title == null || _specs == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                Hide();
                return false;
            }

            if (!_tooltipService.TryBuild(itemId, quantity, maxStack, out var model))
            {
                Hide();
                return false;
            }

            var coordinateRoot = _tooltip.parent ?? _positionRoot;
            var localPointer = coordinateRoot != null
                ? coordinateRoot.WorldToLocal(pointerWorldPosition)
                : pointerWorldPosition;
            _title.text = model.DisplayName;
            _specs.text = string.IsNullOrWhiteSpace(specsOverride)
                ? string.Concat(model.Category, "\n", model.ShortStats)
                : string.Concat(model.Category, "\n", specsOverride);
            _tooltip.style.left = localPointer.x + OffsetX;
            _tooltip.style.top = localPointer.y + OffsetY;
            _tooltip.style.display = DisplayStyle.Flex;
            return true;
        }

        public void Hide()
        {
            if (_tooltip == null)
            {
                return;
            }

            _tooltip.style.display = DisplayStyle.None;
        }
    }
}
