using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryViewBinder : IUiViewBinder
    {
        private VisualElement _panel;
        private VisualElement _tabBar;
        private VisualElement _backpackGrid;
        private VisualElement _beltGrid;
        private VisualElement[] _beltSlots = Array.Empty<VisualElement>();
        private VisualElement[] _backpackSlots = Array.Empty<VisualElement>();
        private readonly List<VisualElement> _backpackSlotElements = new List<VisualElement>();
        private readonly List<VisualElement> _beltSlotElements = new List<VisualElement>();
        private readonly List<Button> _tabs = new List<Button>(4);
        private VisualElement _tooltip;
        private Label _tooltipTitle;
        private VisualElement _inventorySection;
        private VisualElement _questsSection;
        private VisualElement _journalSection;
        private VisualElement _calendarSection;
        private Button _tabInventory;
        private Button _tabQuests;
        private Button _tabJournal;
        private Button _tabCalendar;
        private string[] _beltSlotItemIds = Array.Empty<string>();
        private string[] _backpackSlotItemIds = Array.Empty<string>();
        private string _dragSourceContainer;
        private int _dragSourceIndex = -1;
        private string _dragSourceItemId;
        private float _resolvedSlotSize = 46f;

        private const float MinSlotSize = 16f;
        private const float MaxSlotSize = 46f;
        private const float SlotGap = 4f;
        private const float MinTabWidth = 56f;
        private const float MaxTabWidth = 180f;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int beltSlotCount, int backpackSlotCount)
        {
            _panel = root?.Q<VisualElement>("inventory__panel");
            _tabBar = root?.Q<VisualElement>("inventory__tabbar");
            _backpackGrid = root?.Q<VisualElement>("inventory__backpack-grid");
            _beltGrid = root?.Q<VisualElement>("inventory__grid-row--belt");
            _tooltip = root?.Q<VisualElement>("inventory__tooltip");
            _tooltipTitle = root?.Q<Label>("inventory__tooltip-title");
            _inventorySection = root?.Q<VisualElement>("inventory__section-inventory");
            _questsSection = root?.Q<VisualElement>("inventory__section-quests");
            _journalSection = root?.Q<VisualElement>("inventory__section-journal");
            _calendarSection = root?.Q<VisualElement>("inventory__section-calendar");
            _tabInventory = root?.Q<Button>("inventory__tab-inventory");
            _tabQuests = root?.Q<Button>("inventory__tab-quests");
            _tabJournal = root?.Q<Button>("inventory__tab-journal");
            _tabCalendar = root?.Q<Button>("inventory__tab-calendar");
            _tabs.Clear();
            AddTab(_tabInventory);
            AddTab(_tabQuests);
            AddTab(_tabJournal);
            AddTab(_tabCalendar);

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

            CacheSlotElements();
            RegisterResponsiveCallbacks();
            ApplyResponsiveLayout();
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

            if (_tabJournal != null)
            {
                _tabJournal.clicked += () => IntentRaised?.Invoke(new UiIntent("tab.menu.select", "journal"));
            }
        }

        private void RegisterResponsiveCallbacks()
        {
            _panel?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _tabBar?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _backpackGrid?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _beltGrid?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
        }

        private void CacheSlotElements()
        {
            _backpackSlotElements.Clear();
            _beltSlotElements.Clear();
            CollectSlotElements(_backpackGrid, _backpackSlotElements);
            CollectSlotElements(_beltGrid, _beltSlotElements);
        }

        private void ApplyResponsiveLayout()
        {
            ApplyResponsiveSlots(_backpackGrid, _backpackSlotElements, preferredColumns: 8);
            ApplyResponsiveBeltSlots();
            ApplyResponsiveTabs();
        }

        private void ApplyResponsiveBeltSlots()
        {
            if (_beltSlotElements.Count == 0)
            {
                return;
            }

            var beltWidth = _beltGrid?.contentRect.width ?? 0f;
            if (beltWidth <= 0f)
            {
                return;
            }

            var availableWidth = Mathf.Max(0f, beltWidth - ((_beltSlotElements.Count - 1) * SlotGap));
            var preferredSlotSize = availableWidth / Math.Max(1, _beltSlotElements.Count);
            var resolved = Mathf.Clamp(Mathf.Min(_resolvedSlotSize, preferredSlotSize), MinSlotSize, MaxSlotSize);
            for (var i = 0; i < _beltSlotElements.Count; i++)
            {
                var slot = _beltSlotElements[i];
                slot.style.width = resolved;
                slot.style.height = resolved;
            }
        }

        private void ApplyResponsiveSlots(VisualElement container, List<VisualElement> slots, int preferredColumns)
        {
            if (container == null || slots.Count == 0)
            {
                return;
            }

            var width = container.contentRect.width;
            if (width <= 0f)
            {
                return;
            }

            var cappedColumns = Mathf.Max(1, preferredColumns);
            var maxWidthPerSlot = (width - ((cappedColumns - 1) * SlotGap)) / cappedColumns;
            _resolvedSlotSize = Mathf.Clamp(maxWidthPerSlot, MinSlotSize, MaxSlotSize);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.style.width = _resolvedSlotSize;
                slot.style.height = _resolvedSlotSize;
            }
        }

        private void ApplyResponsiveTabs()
        {
            if (_tabBar == null || _tabs.Count == 0)
            {
                return;
            }

            var barWidth = _tabBar.contentRect.width;
            if (barWidth <= 0f)
            {
                return;
            }

            var totalGap = (_tabs.Count - 1) * 6f;
            var perTabWidth = (barWidth - totalGap) / _tabs.Count;
            var resolvedWidth = Mathf.Clamp(perTabWidth, MinTabWidth, MaxTabWidth);
            var resolvedHeight = Mathf.Clamp(_resolvedSlotSize * 0.56f, 20f, 28f);
            var resolvedFontSize = Mathf.Clamp(_resolvedSlotSize * 0.24f, 9f, 12f);

            for (var i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];
                tab.style.width = resolvedWidth;
                tab.style.height = resolvedHeight;
                tab.style.fontSize = resolvedFontSize;
            }
        }

        private static void CollectSlotElements(VisualElement container, List<VisualElement> target)
        {
            if (container == null)
            {
                return;
            }

            var query = container.Query<VisualElement>(className: "inventory__slot");
            var slots = query.ToList();
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null)
                {
                    target.Add(slots[i]);
                }
            }
        }

        private void AddTab(Button tab)
        {
            if (tab != null)
            {
                _tabs.Add(tab);
            }
        }

        private void ApplySectionVisibility(string activeSection)
        {
            var section = string.IsNullOrWhiteSpace(activeSection) ? "inventory" : activeSection;
            SetSectionVisibility(_inventorySection, section == "inventory");
            SetSectionVisibility(_questsSection, section == "quests");
            SetSectionVisibility(_journalSection, section == "journal");
            SetSectionVisibility(_calendarSection, section == "calendar");

            _tabInventory?.EnableInClassList("is-active", section == "inventory");
            _tabQuests?.EnableInClassList("is-active", section == "quests");
            _tabJournal?.EnableInClassList("is-active", section == "journal");
            _tabCalendar?.EnableInClassList("is-active", section == "calendar");
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
