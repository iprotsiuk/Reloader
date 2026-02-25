using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryViewBinder : IUiViewBinder
    {
        private VisualElement _panel;
        private VisualElement[] _beltSlots = Array.Empty<VisualElement>();
        private VisualElement[] _backpackSlots = Array.Empty<VisualElement>();
        private VisualElement _tooltip;
        private Label _tooltipTitle;
        private VisualElement _inventorySection;
        private VisualElement _questsSection;
        private VisualElement _calendarSection;
        private VisualElement _eventsSection;
        private Button _tabInventory;
        private Button _tabQuests;
        private Button _tabCalendar;
        private Button _tabEvents;
        private string[] _beltSlotItemIds = Array.Empty<string>();
        private string[] _backpackSlotItemIds = Array.Empty<string>();
        private string _dragSourceContainer;
        private int _dragSourceIndex = -1;
        private string _dragSourceItemId;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int beltSlotCount, int backpackSlotCount)
        {
            _panel = root?.Q<VisualElement>("inventory__panel");
            _tooltip = root?.Q<VisualElement>("inventory__tooltip");
            _tooltipTitle = root?.Q<Label>("inventory__tooltip-title");
            _inventorySection = root?.Q<VisualElement>("inventory__section-inventory");
            _questsSection = root?.Q<VisualElement>("inventory__section-quests");
            _calendarSection = root?.Q<VisualElement>("inventory__section-calendar");
            _eventsSection = root?.Q<VisualElement>("inventory__section-events");
            _tabInventory = root?.Q<Button>("inventory__tab-inventory");
            _tabQuests = root?.Q<Button>("inventory__tab-quests");
            _tabCalendar = root?.Q<Button>("inventory__tab-calendar");
            _tabEvents = root?.Q<Button>("inventory__tab-events");

            RegisterTabIntents();

            _beltSlots = new VisualElement[Math.Max(0, beltSlotCount)];
            _beltSlotItemIds = new string[_beltSlots.Length];
            for (var i = 0; i < _beltSlots.Length; i++)
            {
                var slot = root?.Q<VisualElement>($"inventory__belt-slot-{i}");
                _beltSlots[i] = slot;
                if (slot != null)
                {
                    var capturedIndex = i;
                    slot.RegisterCallback<ClickEvent>(_ => TryInteractSlot("belt", capturedIndex));
                }
            }

            _backpackSlots = new VisualElement[Math.Max(0, backpackSlotCount)];
            _backpackSlotItemIds = new string[_backpackSlots.Length];
            for (var i = 0; i < _backpackSlots.Length; i++)
            {
                var slot = root?.Q<VisualElement>($"inventory__backpack-slot-{i}");
                _backpackSlots[i] = slot;
                if (slot != null)
                {
                    var capturedIndex = i;
                    slot.RegisterCallback<ClickEvent>(_ => TryInteractSlot("backpack", capturedIndex));
                }
            }
        }

        public void Render(UiRenderState state)
        {
            if (state is not TabInventoryUiState inventoryState)
            {
                return;
            }

            if (_panel != null)
            {
                _panel.style.display = inventoryState.IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }

            ApplyOccupancy(_beltSlots, inventoryState.BeltSlots);
            ApplyOccupancy(_backpackSlots, inventoryState.BackpackSlots);
            CacheSlotItemIds(_beltSlotItemIds, inventoryState.BeltSlots);
            CacheSlotItemIds(_backpackSlotItemIds, inventoryState.BackpackSlots);
            ApplySectionVisibility(inventoryState.ActiveSection);

            if (_tooltip != null)
            {
                _tooltip.style.display = inventoryState.TooltipVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_tooltipTitle != null)
            {
                _tooltipTitle.text = inventoryState.TooltipTitle;
            }
        }

        private void RegisterTabIntents()
        {
            if (_tabInventory != null)
            {
                _tabInventory.clicked += () => IntentRaised?.Invoke(new UiIntent("tab.menu.select", "inventory"));
            }

            if (_tabQuests != null)
            {
                _tabQuests.clicked += () => IntentRaised?.Invoke(new UiIntent("tab.menu.select", "quests"));
            }

            if (_tabCalendar != null)
            {
                _tabCalendar.clicked += () => IntentRaised?.Invoke(new UiIntent("tab.menu.select", "calendar"));
            }

            if (_tabEvents != null)
            {
                _tabEvents.clicked += () => IntentRaised?.Invoke(new UiIntent("tab.menu.select", "events"));
            }
        }

        private void ApplySectionVisibility(string activeSection)
        {
            var section = string.IsNullOrWhiteSpace(activeSection) ? "inventory" : activeSection;
            SetSectionVisibility(_inventorySection, section == "inventory");
            SetSectionVisibility(_questsSection, section == "quests");
            SetSectionVisibility(_calendarSection, section == "calendar");
            SetSectionVisibility(_eventsSection, section == "events");

            _tabInventory?.EnableInClassList("is-active", section == "inventory");
            _tabQuests?.EnableInClassList("is-active", section == "quests");
            _tabCalendar?.EnableInClassList("is-active", section == "calendar");
            _tabEvents?.EnableInClassList("is-active", section == "events");
        }

        private static void SetSectionVisibility(VisualElement section, bool isVisible)
        {
            if (section == null)
            {
                return;
            }

            section.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void ApplyOccupancy(VisualElement[] slotElements, System.Collections.Generic.IReadOnlyList<TabInventoryUiState.SlotState> slots)
        {
            var limit = Math.Min(slotElements.Length, slots.Count);
            for (var i = 0; i < limit; i++)
            {
                var element = slotElements[i];
                if (element == null)
                {
                    continue;
                }

                element.EnableInClassList("is-occupied", slots[i].IsOccupied);
            }
        }

        private static void CacheSlotItemIds(string[] targets, System.Collections.Generic.IReadOnlyList<TabInventoryUiState.SlotState> slots)
        {
            var limit = Math.Min(targets.Length, slots.Count);
            for (var i = 0; i < limit; i++)
            {
                targets[i] = slots[i].ItemId;
            }
        }

        public bool TryInteractSlotForTests(string container, int slotIndex)
        {
            return TryInteractSlot(container, slotIndex);
        }

        private bool TryInteractSlot(string container, int slotIndex)
        {
            if (slotIndex < 0)
            {
                return false;
            }

            var targetItemId = ResolveSlotItemId(container, slotIndex);
            if (_dragSourceIndex < 0)
            {
                if (string.IsNullOrWhiteSpace(targetItemId))
                {
                    return false;
                }

                _dragSourceContainer = NormalizeContainer(container);
                _dragSourceIndex = slotIndex;
                _dragSourceItemId = targetItemId;
                return true;
            }

            if (_dragSourceContainer == NormalizeContainer(container) && _dragSourceIndex == slotIndex)
            {
                ClearDragSource();
                return true;
            }

            var key = !string.IsNullOrWhiteSpace(_dragSourceItemId) && _dragSourceItemId == targetItemId
                ? "inventory.drag.merge"
                : "inventory.drag.swap";
            var payload = new TabInventoryDragController.DragIntentPayload(_dragSourceContainer, _dragSourceIndex, NormalizeContainer(container), slotIndex);
            IntentRaised?.Invoke(new UiIntent(key, payload));
            ClearDragSource();
            return true;
        }

        private string ResolveSlotItemId(string container, int slotIndex)
        {
            var normalized = NormalizeContainer(container);
            var source = normalized == "backpack" ? _backpackSlotItemIds : _beltSlotItemIds;
            return slotIndex >= 0 && slotIndex < source.Length ? source[slotIndex] : null;
        }

        private static string NormalizeContainer(string container)
        {
            return container == "backpack" ? "backpack" : "belt";
        }

        private void ClearDragSource()
        {
            _dragSourceContainer = null;
            _dragSourceIndex = -1;
            _dragSourceItemId = null;
        }
    }
}
