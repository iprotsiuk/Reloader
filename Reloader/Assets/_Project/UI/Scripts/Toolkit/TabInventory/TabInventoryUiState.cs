using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryUiState : UiRenderState
    {
        private const string NoTargetMarkedText = "No target marked";
        private const string ZeroValidationShotsText = "0 validation shots";
        private const string ReconHooksNotInstalledText = "Recon hooks are not installed.";

        public enum ContractPanelMode
        {
            None = 0,
            PostedOffer = 1,
            ActiveContract = 2
        }

        public readonly struct SlotState
        {
            public SlotState(int index, string itemId, bool isOccupied, int quantity = 1, int maxStack = 1)
            {
                Index = index;
                ItemId = itemId;
                IsOccupied = isOccupied;
                Quantity = quantity;
                MaxStack = maxStack;
            }

            public int Index { get; }
            public string ItemId { get; }
            public bool IsOccupied { get; }
            public int Quantity { get; }
            public int MaxStack { get; }
        }

        public readonly struct ContractPanelState
        {
            public ContractPanelState(
                ContractPanelMode mode,
                string statusText,
                string titleText,
                string summaryText,
                string targetText,
                string distanceText,
                string payoutText,
                string briefingText,
                bool canAccept,
                bool canCancel,
                bool canClaimReward)
            {
                Mode = mode;
                StatusText = statusText ?? string.Empty;
                TitleText = titleText ?? string.Empty;
                SummaryText = summaryText ?? string.Empty;
                TargetText = targetText ?? string.Empty;
                DistanceText = distanceText ?? string.Empty;
                PayoutText = payoutText ?? string.Empty;
                BriefingText = briefingText ?? string.Empty;
                CanAccept = canAccept;
                CanCancel = canCancel;
                CanClaimReward = canClaimReward;
            }

            public ContractPanelMode Mode { get; }
            public string StatusText { get; }
            public string TitleText { get; }
            public string SummaryText { get; }
            public string TargetText { get; }
            public string DistanceText { get; }
            public string PayoutText { get; }
            public string BriefingText { get; }
            public bool CanAccept { get; }
            public bool CanCancel { get; }
            public bool CanClaimReward { get; }

            public static ContractPanelState CreateDefault()
            {
                return new ContractPanelState(
                    mode: ContractPanelMode.None,
                    statusText: "No contracts currently posted",
                    titleText: "No posted contracts",
                    summaryText: "Check back later for fresh contract offers.",
                    targetText: "--",
                    distanceText: "Distance: --",
                    payoutText: "Payout: --",
                    briefingText: "Check back later for fresh contract offers.",
                    canAccept: false,
                    canCancel: false,
                    canClaimReward: false);
            }
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
            ContractPanelState contractPanel,
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
            string[] deviceSessionHistoryEntries,
            string attachmentsWeaponName,
            string attachmentsStatusText,
            string attachmentsSelectedSlot,
            string attachmentsSelectedItem,
            bool attachmentsCanApply,
            string[] attachmentSlotOptions,
            string[] attachmentItemOptions)
            : base("tab-inventory")
        {
            IsOpen = isOpen;
            _beltSlots = beltSlots ?? Array.Empty<SlotState>();
            _backpackSlots = backpackSlots ?? Array.Empty<SlotState>();
            TooltipTitle = tooltipTitle ?? string.Empty;
            TooltipVisible = tooltipVisible;
            ActiveSection = string.IsNullOrWhiteSpace(activeSection) ? "device" : activeSection;
            ContractPanel = contractPanel;
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
            AttachmentsWeaponName = attachmentsWeaponName ?? string.Empty;
            AttachmentsStatusText = attachmentsStatusText ?? string.Empty;
            AttachmentsSelectedSlot = attachmentsSelectedSlot ?? string.Empty;
            AttachmentsSelectedItem = attachmentsSelectedItem ?? string.Empty;
            AttachmentsCanApply = attachmentsCanApply;
            _attachmentSlotOptions = attachmentSlotOptions ?? Array.Empty<string>();
            _attachmentItemOptions = attachmentItemOptions ?? Array.Empty<string>();
        }

        public bool IsOpen { get; }
        public IReadOnlyList<SlotState> BeltSlots => _beltSlots;
        public IReadOnlyList<SlotState> BackpackSlots => _backpackSlots;
        public string TooltipTitle { get; }
        public bool TooltipVisible { get; }
        public string ActiveSection { get; }
        public ContractPanelState ContractPanel { get; }
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
        public string AttachmentsWeaponName { get; }
        public string AttachmentsStatusText { get; }
        public string AttachmentsSelectedSlot { get; }
        public string AttachmentsSelectedItem { get; }
        public bool AttachmentsCanApply { get; }
        private readonly string[] _attachmentSlotOptions;
        private readonly string[] _attachmentItemOptions;
        public IReadOnlyList<string> AttachmentSlotOptions => _attachmentSlotOptions;
        public IReadOnlyList<string> AttachmentItemOptions => _attachmentItemOptions;

        public static TabInventoryUiState Create(
            bool isOpen,
            IEnumerable<SlotState> beltSlots,
            IEnumerable<SlotState> backpackSlots,
            string tooltipTitle,
            bool tooltipVisible,
            string activeSection = "device",
            ContractPanelState? contractPanel = null,
            bool deviceNotesVisible = true,
            string deviceSelectedTargetText = NoTargetMarkedText,
            string deviceShotCountText = ZeroValidationShotsText,
            string deviceSpreadText = "--",
            string deviceMoaText = "--",
            string deviceSavedGroupsText = "0",
            bool deviceCanSaveGroup = false,
            bool deviceCanClearGroup = false,
            bool deviceCanInstallHooks = false,
            bool deviceCanUninstallHooks = false,
            string deviceInstallFeedbackText = ReconHooksNotInstalledText,
            IEnumerable<string> deviceSessionHistoryEntries = null,
            string attachmentsWeaponName = "",
            string attachmentsStatusText = "",
            string attachmentsSelectedSlot = "",
            string attachmentsSelectedItem = "",
            bool attachmentsCanApply = false,
            IEnumerable<string> attachmentSlotOptions = null,
            IEnumerable<string> attachmentItemOptions = null)
        {
            var belt = beltSlots == null ? Array.Empty<SlotState>() : new List<SlotState>(beltSlots).ToArray();
            var backpack = backpackSlots == null ? Array.Empty<SlotState>() : new List<SlotState>(backpackSlots).ToArray();
            var historyEntries = deviceSessionHistoryEntries == null
                ? Array.Empty<string>()
                : new List<string>(deviceSessionHistoryEntries).ToArray();
            var slotOptions = attachmentSlotOptions == null
                ? Array.Empty<string>()
                : new List<string>(attachmentSlotOptions).ToArray();
            var itemOptions = attachmentItemOptions == null
                ? Array.Empty<string>()
                : new List<string>(attachmentItemOptions).ToArray();
            return new TabInventoryUiState(
                isOpen,
                belt,
                backpack,
                tooltipTitle,
                tooltipVisible,
                activeSection,
                contractPanel ?? ContractPanelState.CreateDefault(),
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
                historyEntries,
                attachmentsWeaponName,
                attachmentsStatusText,
                attachmentsSelectedSlot,
                attachmentsSelectedItem,
                attachmentsCanApply,
                slotOptions,
                itemOptions);
        }
    }
}
