using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryUiState : UiRenderState
    {
        public readonly struct SlotState
        {
            public SlotState(int index, string itemId, bool isOccupied, int quantity = 1)
            {
                Index = index;
                ItemId = itemId;
                IsOccupied = isOccupied;
                Quantity = quantity;
            }

            public int Index { get; }
            public string ItemId { get; }
            public bool IsOccupied { get; }
            public int Quantity { get; }
        }

        private readonly SlotState[] _beltSlots;
        private readonly SlotState[] _backpackSlots;

        private TabInventoryUiState(
            bool isOpen,
            SlotState[] beltSlots,
            SlotState[] backpackSlots,
            string tooltipTitle,
            bool tooltipVisible,
            string activeSection,
            bool deviceNotesVisible,
            string deviceSelectedTargetText,
            string deviceShotCountText,
            string deviceSpreadText,
            string deviceMoaText,
            string deviceSavedGroupsText,
            bool deviceCanSaveGroup,
            bool deviceCanClearGroup,
            bool deviceCanInstallHooks,
            bool deviceCanUninstallHooks,
            string deviceInstallFeedbackText,
            string[] deviceSessionHistoryEntries)
            : base("tab-inventory")
        {
            IsOpen = isOpen;
            _beltSlots = beltSlots ?? Array.Empty<SlotState>();
            _backpackSlots = backpackSlots ?? Array.Empty<SlotState>();
            TooltipTitle = tooltipTitle ?? string.Empty;
            TooltipVisible = tooltipVisible;
            ActiveSection = string.IsNullOrWhiteSpace(activeSection) ? "device" : activeSection;
            DeviceNotesVisible = deviceNotesVisible;
            DeviceSelectedTargetText = deviceSelectedTargetText ?? string.Empty;
            DeviceShotCountText = deviceShotCountText ?? string.Empty;
            DeviceSpreadText = deviceSpreadText ?? string.Empty;
            DeviceMoaText = deviceMoaText ?? string.Empty;
            DeviceSavedGroupsText = deviceSavedGroupsText ?? string.Empty;
            DeviceCanSaveGroup = deviceCanSaveGroup;
            DeviceCanClearGroup = deviceCanClearGroup;
            DeviceCanInstallHooks = deviceCanInstallHooks;
            DeviceCanUninstallHooks = deviceCanUninstallHooks;
            DeviceInstallFeedbackText = deviceInstallFeedbackText ?? string.Empty;
            _deviceSessionHistoryEntries = deviceSessionHistoryEntries ?? Array.Empty<string>();
        }

        public bool IsOpen { get; }
        public IReadOnlyList<SlotState> BeltSlots => _beltSlots;
        public IReadOnlyList<SlotState> BackpackSlots => _backpackSlots;
        public string TooltipTitle { get; }
        public bool TooltipVisible { get; }
        public string ActiveSection { get; }
        public bool DeviceNotesVisible { get; }
        public string DeviceSelectedTargetText { get; }
        public string DeviceShotCountText { get; }
        public string DeviceSpreadText { get; }
        public string DeviceMoaText { get; }
        public string DeviceSavedGroupsText { get; }
        public bool DeviceCanSaveGroup { get; }
        public bool DeviceCanClearGroup { get; }
        public bool DeviceCanInstallHooks { get; }
        public bool DeviceCanUninstallHooks { get; }
        public string DeviceInstallFeedbackText { get; }
        private readonly string[] _deviceSessionHistoryEntries;
        public IReadOnlyList<string> DeviceSessionHistoryEntries => _deviceSessionHistoryEntries;

        public static TabInventoryUiState Create(
            bool isOpen,
            IEnumerable<SlotState> beltSlots,
            IEnumerable<SlotState> backpackSlots,
            string tooltipTitle,
            bool tooltipVisible,
            string activeSection = "device",
            bool deviceNotesVisible = true,
            string deviceSelectedTargetText = "No target selected",
            string deviceShotCountText = "0 shots",
            string deviceSpreadText = "--",
            string deviceMoaText = "--",
            string deviceSavedGroupsText = "0",
            bool deviceCanSaveGroup = false,
            bool deviceCanClearGroup = false,
            bool deviceCanInstallHooks = false,
            bool deviceCanUninstallHooks = false,
            string deviceInstallFeedbackText = "Hooks are not installed.",
            IEnumerable<string> deviceSessionHistoryEntries = null)
        {
            var belt = beltSlots == null ? Array.Empty<SlotState>() : new List<SlotState>(beltSlots).ToArray();
            var backpack = backpackSlots == null ? Array.Empty<SlotState>() : new List<SlotState>(backpackSlots).ToArray();
            var historyEntries = deviceSessionHistoryEntries == null
                ? Array.Empty<string>()
                : new List<string>(deviceSessionHistoryEntries).ToArray();
            return new TabInventoryUiState(
                isOpen,
                belt,
                backpack,
                tooltipTitle,
                tooltipVisible,
                activeSection,
                deviceNotesVisible,
                deviceSelectedTargetText,
                deviceShotCountText,
                deviceSpreadText,
                deviceMoaText,
                deviceSavedGroupsText,
                deviceCanSaveGroup,
                deviceCanClearGroup,
                deviceCanInstallHooks,
                deviceCanUninstallHooks,
                deviceInstallFeedbackText,
                historyEntries);
        }
    }
}
