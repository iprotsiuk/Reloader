using System;
using System.Collections.Generic;
using System.Globalization;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryController : MonoBehaviour, IUiController
    {
        private const int MinimumBackpackUiSlots = 16;
        private const string RemoveAttachmentOption = "Remove";

        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _deviceControllerBehaviour;
        [SerializeField] private PlayerWeaponController _weaponController;
        [SerializeField] private WeaponRegistry _weaponRegistry;

        private TabInventoryViewBinder _viewBinder;
        private TabInventoryDragController _dragController;
        private IPlayerInputSource _inputSource;
        private IDeviceController _deviceController;
        private IInventoryEvents _inventoryEvents;
        private IInventoryEvents _subscribedInventoryEvents;
        private bool _useRuntimeKernelInventoryEvents = true;
        private IUiStateEvents _uiStateEvents;
        private bool _useRuntimeKernelUiStateEvents = true;
        private bool _isOpen;
        private string _activeSection = "inventory";
        private string _attachmentsWeaponItemId;
        private string _attachmentsWeaponDisplayName = string.Empty;
        private WeaponDefinition _attachmentsWeaponDefinition;
        private string _attachmentsSelectedSlotName = string.Empty;
        private string _attachmentsSelectedAttachmentItemId = string.Empty;
        private string _attachmentsStatusText = string.Empty;
        private readonly List<string> _attachmentsSlotOptions = new List<string>();
        private readonly List<string> _attachmentsItemOptions = new List<string>();

        public interface IDeviceController
        {
            bool TryGetStatus(out DeviceStatus status);
            void ChooseTarget();
            void SaveGroup();
            void ClearGroup();
            bool InstallHooks();
            bool UninstallHooks();
        }

        public readonly struct DeviceSavedGroupEntry
        {
            public DeviceSavedGroupEntry(int shotCount, bool isMoaAvailable, double moa, double spreadMeters)
            {
                ShotCount = shotCount;
                IsMoaAvailable = isMoaAvailable;
                Moa = moa;
                SpreadMeters = spreadMeters;
            }

            public int ShotCount { get; }
            public bool IsMoaAvailable { get; }
            public double Moa { get; }
            public double SpreadMeters { get; }
        }

        public readonly struct DeviceStatus
        {
            public DeviceStatus(
                bool hasTarget,
                string targetDisplayName,
                float targetDistanceMeters,
                int shotCount,
                bool isMoaAvailable,
                double moa,
                double spreadMeters,
                int savedGroupCount,
                bool canSaveGroup,
                bool canClearGroup,
                bool canInstallHooks,
                bool canUninstallHooks,
                string attachmentFeedbackText,
                IReadOnlyList<DeviceSavedGroupEntry> savedGroups)
            {
                HasTarget = hasTarget;
                TargetDisplayName = targetDisplayName ?? string.Empty;
                TargetDistanceMeters = targetDistanceMeters;
                ShotCount = shotCount;
                IsMoaAvailable = isMoaAvailable;
                Moa = moa;
                SpreadMeters = spreadMeters;
                SavedGroupCount = savedGroupCount;
                CanSaveGroup = canSaveGroup;
                CanClearGroup = canClearGroup;
                CanInstallHooks = canInstallHooks;
                CanUninstallHooks = canUninstallHooks;
                AttachmentFeedbackText = attachmentFeedbackText ?? string.Empty;
                SavedGroups = savedGroups ?? Array.Empty<DeviceSavedGroupEntry>();
            }

            public bool HasTarget { get; }
            public string TargetDisplayName { get; }
            public float TargetDistanceMeters { get; }
            public int ShotCount { get; }
            public bool IsMoaAvailable { get; }
            public double Moa { get; }
            public double SpreadMeters { get; }
            public int SavedGroupCount { get; }
            public bool CanSaveGroup { get; }
            public bool CanClearGroup { get; }
            public bool CanInstallHooks { get; }
            public bool CanUninstallHooks { get; }
            public string AttachmentFeedbackText { get; }
            public IReadOnlyList<DeviceSavedGroupEntry> SavedGroups { get; }
        }

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            SubscribeToInventoryEvents(ResolveInventoryEvents());
            Refresh();
        }

        private void Update()
        {
            Tick();
        }

        public void Configure(TabInventoryViewBinder viewBinder, TabInventoryDragController dragController)
        {
            _viewBinder = viewBinder;
            if (_dragController != null)
            {
                _dragController.IntentRaised -= HandleIntent;
            }

            _dragController = dragController;
            if (_dragController != null)
            {
                _dragController.IntentRaised += HandleIntent;
            }

            Refresh();
        }

        public void Configure(IUiStateEvents uiStateEvents = null)
        {
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
        }

        public void Configure(IInventoryEvents inventoryEvents = null)
        {
            _useRuntimeKernelInventoryEvents = inventoryEvents == null;
            _inventoryEvents = inventoryEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToInventoryEvents(ResolveInventoryEvents());
            }
        }

        public void SetInputSource(IPlayerInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public void SetInventoryController(PlayerInventoryController inventoryController)
        {
            _inventoryController = inventoryController;
            Refresh();
        }

        public void SetDeviceController(IDeviceController deviceController)
        {
            _deviceController = deviceController;
        }

        public void SetWeaponController(PlayerWeaponController weaponController)
        {
            _weaponController = weaponController;
        }

        public void Tick()
        {
            ResolveInputSource();

            if (_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetMenuOpen(false);
                Refresh();
                return;
            }

            if (_inputSource == null)
            {
                return;
            }

            if (_inputSource.ConsumeMenuTogglePressed())
            {
                var uiStateEvents = ResolveUiStateEvents();
                var externalMenuOpen = StorageUiSession.IsOpen
                                       || (uiStateEvents != null
                                           && (uiStateEvents.IsShopTradeMenuOpen || uiStateEvents.IsWorkbenchMenuVisible));

                if (_isOpen || externalMenuOpen)
                {
                    CloseAllOpenMenus();
                    Refresh();
                    return;
                }

                SetMenuOpen(true);
                _activeSection = "inventory";
                Refresh();
            }
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key == "tab.menu.select" && intent.Payload is string sectionId)
            {
                _activeSection = NormalizeSection(sectionId);
                Refresh();
                return;
            }

            if (TryHandleDeviceIntent(intent))
            {
                return;
            }

            if (TryHandleAttachmentIntent(intent))
            {
                return;
            }

            if (_inventoryController?.Runtime == null)
            {
                return;
            }

            if ((intent.Key == "inventory.drag.swap" || intent.Key == "inventory.drag.merge")
                && intent.Payload is TabInventoryDragController.DragIntentPayload dragPayload)
            {
                var sourceArea = ResolveArea(dragPayload.SourceContainer);
                var targetArea = ResolveArea(dragPayload.TargetContainer);
                var targetIndex = NormalizeTargetIndex(sourceArea, targetArea, dragPayload.TargetIndex);
                var moved = _inventoryController.TryMoveItem(
                    sourceArea,
                    dragPayload.SourceIndex,
                    targetArea,
                    targetIndex);

                if (moved)
                {
                    Refresh();
                }
                else
                {
                    Debug.LogWarning(
                        $"TabInventory drag failed ({intent.Key}) src={dragPayload.SourceContainer}[{dragPayload.SourceIndex}] -> dst={dragPayload.TargetContainer}[{dragPayload.TargetIndex}]");
                }

                return;
            }

            if (intent.Key == "inventory.drag.drop"
                && intent.Payload is TabInventoryDragController.DragIntentPayload dropPayload)
            {
                var sourceArea = ResolveArea(dropPayload.SourceContainer);
                var dropped = _inventoryController.TryDropItemFromSlot(sourceArea, dropPayload.SourceIndex);
                if (dropped)
                {
                    Refresh();
                }
                else
                {
                    Debug.LogWarning(
                        $"TabInventory drag drop failed src={dropPayload.SourceContainer}[{dropPayload.SourceIndex}]");
                }
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromInventoryEvents();
            if (_isOpen)
            {
                SetMenuOpen(false);
            }

            if (_dragController != null)
            {
                _dragController.IntentRaised -= HandleIntent;
            }
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            if (_inventoryController?.Runtime == null)
            {
                BuildDevicePanelFields(out var emptyPanelFields);
                _viewBinder.Render(TabInventoryUiState.Create(
                    _isOpen,
                    new TabInventoryUiState.SlotState[PlayerInventoryRuntime.BeltSlotCount],
                    new TabInventoryUiState.SlotState[MinimumBackpackUiSlots],
                    string.Empty,
                    false,
                    _activeSection,
                    deviceNotesVisible: true,
                    deviceSelectedTargetText: emptyPanelFields.SelectedTargetText,
                    deviceShotCountText: emptyPanelFields.ShotCountText,
                    deviceSpreadText: emptyPanelFields.SpreadText,
                    deviceMoaText: emptyPanelFields.MoaText,
                    deviceSavedGroupsText: emptyPanelFields.SavedGroupsText,
                    deviceCanSaveGroup: emptyPanelFields.CanSaveGroup,
                    deviceCanClearGroup: emptyPanelFields.CanClearGroup,
                    deviceCanInstallHooks: emptyPanelFields.CanInstallHooks,
                    deviceCanUninstallHooks: emptyPanelFields.CanUninstallHooks,
                    deviceInstallFeedbackText: emptyPanelFields.InstallFeedbackText,
                    deviceSessionHistoryEntries: emptyPanelFields.SessionHistoryEntries,
                    attachmentsWeaponName: _attachmentsWeaponDisplayName,
                    attachmentsStatusText: _attachmentsStatusText,
                    attachmentsSelectedSlot: _attachmentsSelectedSlotName,
                    attachmentsSelectedItem: _attachmentsSelectedAttachmentItemId,
                    attachmentsCanApply: CanApplyAttachmentSwap(),
                    attachmentSlotOptions: _attachmentsSlotOptions,
                    attachmentItemOptions: _attachmentsItemOptions));
                return;
            }

            var runtime = _inventoryController.Runtime;
            var belt = new List<TabInventoryUiState.SlotState>(PlayerInventoryRuntime.BeltSlotCount);
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var itemId = runtime.BeltSlotItemIds[i];
                var occupied = !string.IsNullOrWhiteSpace(itemId);
                var quantity = occupied ? runtime.GetSlotQuantity(InventoryArea.Belt, i) : 0;
                var maxStack = occupied ? runtime.GetSlotMaxStack(InventoryArea.Belt, i) : 1;
                belt.Add(new TabInventoryUiState.SlotState(i, itemId, occupied, quantity, maxStack));
            }

            var backpackSlots = Mathf.Max(MinimumBackpackUiSlots, runtime.BackpackCapacity);
            var backpack = new List<TabInventoryUiState.SlotState>(backpackSlots);
            for (var i = 0; i < backpackSlots; i++)
            {
                var itemId = i < runtime.BackpackItemIds.Count ? runtime.BackpackItemIds[i] : null;
                var occupied = !string.IsNullOrWhiteSpace(itemId);
                var quantity = occupied ? runtime.GetSlotQuantity(InventoryArea.Backpack, i) : 0;
                var maxStack = occupied ? runtime.GetSlotMaxStack(InventoryArea.Backpack, i) : 1;
                backpack.Add(new TabInventoryUiState.SlotState(i, itemId, occupied, quantity, maxStack));
            }

            BuildDevicePanelFields(out var panelFields);
            _viewBinder.Render(TabInventoryUiState.Create(
                _isOpen,
                belt,
                backpack,
                string.Empty,
                false,
                _activeSection,
                deviceNotesVisible: true,
                deviceSelectedTargetText: panelFields.SelectedTargetText,
                deviceShotCountText: panelFields.ShotCountText,
                deviceSpreadText: panelFields.SpreadText,
                deviceMoaText: panelFields.MoaText,
                deviceSavedGroupsText: panelFields.SavedGroupsText,
                deviceCanSaveGroup: panelFields.CanSaveGroup,
                deviceCanClearGroup: panelFields.CanClearGroup,
                deviceCanInstallHooks: panelFields.CanInstallHooks,
                deviceCanUninstallHooks: panelFields.CanUninstallHooks,
                deviceInstallFeedbackText: panelFields.InstallFeedbackText,
                deviceSessionHistoryEntries: panelFields.SessionHistoryEntries,
                attachmentsWeaponName: _attachmentsWeaponDisplayName,
                attachmentsStatusText: _attachmentsStatusText,
                attachmentsSelectedSlot: _attachmentsSelectedSlotName,
                attachmentsSelectedItem: _attachmentsSelectedAttachmentItemId,
                attachmentsCanApply: CanApplyAttachmentSwap(),
                attachmentSlotOptions: _attachmentsSlotOptions,
                attachmentItemOptions: _attachmentsItemOptions));
        }

        private void BuildDevicePanelFields(out DevicePanelFields panelFields)
        {
            var deviceController = ResolveDeviceController();
            if (deviceController == null || !deviceController.TryGetStatus(out var status))
            {
                panelFields = DevicePanelFields.CreateDefault();
                return;
            }

            var targetText = status.HasTarget
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1:0.0} m)",
                    string.IsNullOrWhiteSpace(status.TargetDisplayName) ? "Unnamed Target" : status.TargetDisplayName,
                    Mathf.Max(0f, status.TargetDistanceMeters))
                : "No target selected";

            var shotCount = Mathf.Max(0, status.ShotCount);
            var shotCountText = shotCount == 1
                ? "1 shot"
                : string.Format(CultureInfo.InvariantCulture, "{0} shots", shotCount);
            var spreadText = status.IsMoaAvailable
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.0} cm", Math.Max(0d, status.SpreadMeters) * 100d)
                : "--";
            var moaText = status.IsMoaAvailable
                ? string.Format(CultureInfo.InvariantCulture, "{0:0.00} MOA", Math.Max(0d, status.Moa))
                : "--";
            var savedGroupCount = Mathf.Max(0, status.SavedGroupCount);
            var sessionHistoryEntries = BuildSessionHistoryEntries(status.SavedGroups);

            panelFields = new DevicePanelFields(
                targetText,
                shotCountText,
                spreadText,
                moaText,
                savedGroupCount.ToString(CultureInfo.InvariantCulture),
                status.CanSaveGroup,
                status.CanClearGroup,
                status.CanInstallHooks,
                status.CanUninstallHooks,
                string.IsNullOrWhiteSpace(status.AttachmentFeedbackText)
                    ? (status.CanUninstallHooks ? "Hooks installed." : "Hooks are not installed.")
                    : status.AttachmentFeedbackText,
                sessionHistoryEntries);
        }

        private void HandleInventoryChanged()
        {
            Refresh();
        }

        private static InventoryArea ResolveArea(string container)
        {
            return container == "backpack" ? InventoryArea.Backpack : InventoryArea.Belt;
        }

        private static string NormalizeSection(string sectionId)
        {
            return sectionId switch
            {
                "inventory" => "inventory",
                "quests" => "quests",
                "journal" => "journal",
                "calendar" => "calendar",
                "device" => "device",
                "attachments" => "attachments",
                _ => "device"
            };
        }

        private bool TryHandleDeviceIntent(UiIntent intent)
        {
            var deviceController = ResolveDeviceController();
            if (deviceController == null)
            {
                return false;
            }

            deviceController.TryGetStatus(out var status);

            switch (intent.Key)
            {
                case "tab.inventory.device.choose-target":
                    SetMenuOpen(false);
                    deviceController.ChooseTarget();
                    Refresh();
                    return true;
                case "tab.inventory.device.save-group":
                    if (status.CanSaveGroup)
                    {
                        deviceController.SaveGroup();
                    }

                    Refresh();
                    return true;
                case "tab.inventory.device.clear-group":
                    if (status.CanClearGroup)
                    {
                        deviceController.ClearGroup();
                    }

                    Refresh();
                    return true;
                case "tab.inventory.device.install-hooks":
                    if (status.CanInstallHooks)
                    {
                        deviceController.InstallHooks();
                    }

                    Refresh();
                    return true;
                case "tab.inventory.device.uninstall-hooks":
                    if (status.CanUninstallHooks)
                    {
                        deviceController.UninstallHooks();
                    }

                    Refresh();
                    return true;
                default:
                    return false;
            }
        }

        private bool TryHandleAttachmentIntent(UiIntent intent)
        {
            switch (intent.Key)
            {
                case "tab.inventory.item.context.attachments":
                    if (intent.Payload is TabInventoryAttachmentContextIntentPayload payload)
                    {
                        OpenAttachmentsPanel(payload);
                        Refresh();
                    }

                    return true;
                case "tab.inventory.attachments.slot-selected":
                    if (intent.Payload is string slotName)
                    {
                        HandleAttachmentSlotSelected(slotName);
                        Refresh();
                    }

                    return true;
                case "tab.inventory.attachments.item-selected":
                    if (intent.Payload is string itemId)
                    {
                        HandleAttachmentItemSelected(itemId);
                        Refresh();
                    }

                    return true;
                case "tab.inventory.attachments.apply":
                    ApplyAttachmentSelection();
                    Refresh();
                    return true;
                case "tab.inventory.attachments.back":
                    _activeSection = "inventory";
                    Refresh();
                    return true;
                default:
                    return false;
            }
        }

        private void OpenAttachmentsPanel(TabInventoryAttachmentContextIntentPayload payload)
        {
            if (!TryResolvePayloadItemId(payload, out var itemId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(payload.ItemId)
                || !TryResolveWeaponDefinition(itemId, out var definition))
            {
                return;
            }

            _attachmentsWeaponItemId = itemId;
            _attachmentsWeaponDefinition = definition;
            _attachmentsWeaponDisplayName = string.IsNullOrWhiteSpace(definition.DisplayName)
                ? itemId
                : definition.DisplayName;
            _attachmentsStatusText = string.Empty;
            BuildAttachmentSlotOptions(definition);
            if (_attachmentsSlotOptions.Count == 0)
            {
                _attachmentsSelectedSlotName = string.Empty;
                _attachmentsSelectedAttachmentItemId = string.Empty;
                _attachmentsStatusText = "No compatible attachment slots.";
            }
            else if (!_attachmentsSlotOptions.Contains(_attachmentsSelectedSlotName))
            {
                _attachmentsSelectedSlotName = _attachmentsSlotOptions[0];
            }

            RebuildAttachmentItemOptions();
            _activeSection = "attachments";
        }

        private bool TryResolvePayloadItemId(TabInventoryAttachmentContextIntentPayload payload, out string itemId)
        {
            itemId = null;
            var runtime = _inventoryController?.Runtime;
            if (runtime == null || payload.SlotIndex < 0)
            {
                return false;
            }

            if (string.Equals(payload.Container, "backpack", StringComparison.Ordinal))
            {
                if (payload.SlotIndex >= runtime.BackpackItemIds.Count)
                {
                    return false;
                }

                itemId = runtime.BackpackItemIds[payload.SlotIndex];
            }
            else
            {
                if (payload.SlotIndex >= PlayerInventoryRuntime.BeltSlotCount)
                {
                    return false;
                }

                itemId = runtime.BeltSlotItemIds[payload.SlotIndex];
            }

            return string.Equals(itemId, payload.ItemId, StringComparison.Ordinal);
        }

        private void HandleAttachmentSlotSelected(string slotName)
        {
            if (!_attachmentsSlotOptions.Contains(slotName))
            {
                return;
            }

            _attachmentsSelectedSlotName = slotName;
            RebuildAttachmentItemOptions();
        }

        private void HandleAttachmentItemSelected(string itemId)
        {
            if (!_attachmentsItemOptions.Contains(itemId))
            {
                return;
            }

            _attachmentsSelectedAttachmentItemId = itemId;
        }

        private void ApplyAttachmentSelection()
        {
            if (!CanApplyAttachmentSwap())
            {
                _attachmentsStatusText = "Select slot and attachment.";
                return;
            }

            var weaponController = ResolveWeaponController();
            if (weaponController == null)
            {
                _attachmentsStatusText = "Weapon controller unavailable.";
                return;
            }

            if (!TryParseSlotName(_attachmentsSelectedSlotName, out var slotType))
            {
                _attachmentsStatusText = "Invalid attachment slot.";
                return;
            }

            var selectedBeltItemId = _inventoryController?.Runtime?.SelectedBeltItemId;
            if (!string.Equals(selectedBeltItemId, _attachmentsWeaponItemId, StringComparison.Ordinal))
            {
                _attachmentsStatusText = "Select this weapon on belt before applying.";
                return;
            }

            var attachmentItemId = IsRemoveAttachmentOption(_attachmentsSelectedAttachmentItemId)
                ? string.Empty
                : _attachmentsSelectedAttachmentItemId;
            if (!weaponController.TrySwapEquippedWeaponAttachment(slotType, attachmentItemId))
            {
                _attachmentsStatusText = "Attachment swap failed.";
                return;
            }

            _attachmentsStatusText = string.IsNullOrWhiteSpace(attachmentItemId)
                ? "Attachment removed."
                : "Attachment applied.";
            RebuildAttachmentItemOptions();
        }

        private bool CanApplyAttachmentSwap()
        {
            return !string.IsNullOrWhiteSpace(_attachmentsWeaponItemId)
                   && !string.IsNullOrWhiteSpace(_attachmentsSelectedSlotName)
                   && !string.IsNullOrWhiteSpace(_attachmentsSelectedAttachmentItemId);
        }

        private void BuildAttachmentSlotOptions(WeaponDefinition definition)
        {
            _attachmentsSlotOptions.Clear();
            if (definition == null)
            {
                return;
            }

            AddSlotOptionIfCompatible(definition, WeaponAttachmentSlotType.Scope);
            AddSlotOptionIfCompatible(definition, WeaponAttachmentSlotType.Muzzle);
        }

        private void AddSlotOptionIfCompatible(WeaponDefinition definition, WeaponAttachmentSlotType slotType)
        {
            var compatible = definition.GetCompatibleAttachmentItemIds(slotType);
            if (compatible == null || compatible.Count == 0)
            {
                return;
            }

            _attachmentsSlotOptions.Add(slotType.ToString());
        }

        private void RebuildAttachmentItemOptions()
        {
            _attachmentsItemOptions.Clear();
            if (_attachmentsWeaponDefinition == null
                || !TryParseSlotName(_attachmentsSelectedSlotName, out var slotType))
            {
                _attachmentsSelectedAttachmentItemId = string.Empty;
                return;
            }

            var compatible = _attachmentsWeaponDefinition.GetCompatibleAttachmentItemIds(slotType);
            if (compatible == null || compatible.Count == 0)
            {
                _attachmentsSelectedAttachmentItemId = string.Empty;
                return;
            }

            var runtime = _inventoryController?.Runtime;
            TryGetContextWeaponRuntimeState(out var state);
            var equippedItemId = state?.GetEquippedAttachmentItemId(slotType) ?? string.Empty;
            if (!_attachmentsItemOptions.Contains(RemoveAttachmentOption))
            {
                _attachmentsItemOptions.Add(RemoveAttachmentOption);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < compatible.Count; i++)
            {
                var attachmentItemId = compatible[i];
                if (string.IsNullOrWhiteSpace(attachmentItemId) || !seen.Add(attachmentItemId))
                {
                    continue;
                }

                var owned = runtime != null && runtime.GetItemQuantity(attachmentItemId) > 0;
                var equipped = string.Equals(equippedItemId, attachmentItemId, StringComparison.Ordinal);
                if (owned || equipped)
                {
                    _attachmentsItemOptions.Add(attachmentItemId);
                }
            }

            if (_attachmentsItemOptions.Count == 0)
            {
                _attachmentsSelectedAttachmentItemId = string.Empty;
                return;
            }

            if (!_attachmentsItemOptions.Contains(_attachmentsSelectedAttachmentItemId))
            {
                _attachmentsSelectedAttachmentItemId = _attachmentsItemOptions[0];
            }
        }

        private static bool IsRemoveAttachmentOption(string value)
        {
            return string.Equals(value, RemoveAttachmentOption, StringComparison.Ordinal);
        }

        private bool TryResolveWeaponDefinition(string itemId, out WeaponDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            var registry = ResolveWeaponRegistry();
            if (registry != null && registry.TryGetWeaponDefinition(itemId, out definition))
            {
                return true;
            }

            var registries = FindObjectsByType<WeaponRegistry>(FindObjectsSortMode.None);
            for (var i = 0; i < registries.Length; i++)
            {
                var candidate = registries[i];
                if (candidate == null || candidate == registry)
                {
                    continue;
                }

                if (!candidate.TryGetWeaponDefinition(itemId, out definition))
                {
                    continue;
                }

                _weaponRegistry = candidate;
                return true;
            }

            definition = null;
            return false;
        }

        private bool TryGetContextWeaponRuntimeState(out WeaponRuntimeState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(_attachmentsWeaponItemId))
            {
                return false;
            }

            var weaponController = ResolveWeaponController();
            return weaponController != null && weaponController.TryGetRuntimeState(_attachmentsWeaponItemId, out state);
        }

        private static bool TryParseSlotName(string slotName, out WeaponAttachmentSlotType slotType)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                slotType = default;
                return false;
            }

            return Enum.TryParse(slotName, ignoreCase: true, out slotType);
        }

        private static string[] BuildSessionHistoryEntries(IReadOnlyList<DeviceSavedGroupEntry> savedGroups)
        {
            if (savedGroups == null || savedGroups.Count == 0)
            {
                return Array.Empty<string>();
            }

            var entries = new string[savedGroups.Count];
            for (var i = 0; i < savedGroups.Count; i++)
            {
                var group = savedGroups[i];
                var shotCount = Mathf.Max(0, group.ShotCount);
                var shotText = shotCount == 1
                    ? "1 shot"
                    : string.Format(CultureInfo.InvariantCulture, "{0} shots", shotCount);
                var spreadText = group.IsMoaAvailable
                    ? string.Format(CultureInfo.InvariantCulture, "{0:0.0} cm", Math.Max(0d, group.SpreadMeters) * 100d)
                    : "--";
                var moaText = group.IsMoaAvailable
                    ? string.Format(CultureInfo.InvariantCulture, "{0:0.00} MOA", Math.Max(0d, group.Moa))
                    : "--";
                entries[i] = string.Format(
                    CultureInfo.InvariantCulture,
                    "#{0} - {1} - {2} - {3}",
                    i + 1,
                    shotText,
                    moaText,
                    spreadText);
            }

            return entries;
        }

        private readonly struct DevicePanelFields
        {
            public DevicePanelFields(
                string selectedTargetText,
                string shotCountText,
                string spreadText,
                string moaText,
                string savedGroupsText,
                bool canSaveGroup,
                bool canClearGroup,
                bool canInstallHooks,
                bool canUninstallHooks,
                string installFeedbackText,
                string[] sessionHistoryEntries)
            {
                SelectedTargetText = selectedTargetText ?? "No target selected";
                ShotCountText = shotCountText ?? "0 shots";
                SpreadText = spreadText ?? "--";
                MoaText = moaText ?? "--";
                SavedGroupsText = savedGroupsText ?? "0";
                CanSaveGroup = canSaveGroup;
                CanClearGroup = canClearGroup;
                CanInstallHooks = canInstallHooks;
                CanUninstallHooks = canUninstallHooks;
                InstallFeedbackText = installFeedbackText ?? string.Empty;
                SessionHistoryEntries = sessionHistoryEntries ?? Array.Empty<string>();
            }

            public string SelectedTargetText { get; }
            public string ShotCountText { get; }
            public string SpreadText { get; }
            public string MoaText { get; }
            public string SavedGroupsText { get; }
            public bool CanSaveGroup { get; }
            public bool CanClearGroup { get; }
            public bool CanInstallHooks { get; }
            public bool CanUninstallHooks { get; }
            public string InstallFeedbackText { get; }
            public string[] SessionHistoryEntries { get; }

            public static DevicePanelFields CreateDefault()
            {
                return new DevicePanelFields(
                    selectedTargetText: "No target selected",
                    shotCountText: "0 shots",
                    spreadText: "--",
                    moaText: "--",
                    savedGroupsText: "0",
                    canSaveGroup: false,
                    canClearGroup: false,
                    canInstallHooks: false,
                    canUninstallHooks: false,
                    installFeedbackText: "Hooks are not installed.",
                    sessionHistoryEntries: Array.Empty<string>());
            }
        }

        private int NormalizeTargetIndex(InventoryArea sourceArea, InventoryArea targetArea, int requestedTargetIndex)
        {
            if (sourceArea == InventoryArea.Belt
                && targetArea == InventoryArea.Backpack
                && _inventoryController?.Runtime != null
                && requestedTargetIndex >= _inventoryController.Runtime.BackpackItemIds.Count)
            {
                return _inventoryController.Runtime.BackpackItemIds.Count;
            }

            return requestedTargetIndex;
        }

        private void ResolveInputSource()
        {
            if (!IsReferenceAlive(_inputSource))
            {
                _inputSource = null;
            }

            if (_inputSource != null)
            {
                return;
            }

            if (_inputSourceBehaviour is IPlayerInputSource directFromField)
            {
                _inputSource = directFromField;
                return;
            }

            _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            if (_inputSource != null)
            {
                return;
            }

            _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
        }

        private IDeviceController ResolveDeviceController()
        {
            if (!IsReferenceAlive(_deviceController))
            {
                _deviceController = null;
            }

            if (_deviceController != null)
            {
                return _deviceController;
            }

            if (_deviceControllerBehaviour is IDeviceController directFromField)
            {
                _deviceController = directFromField;
                return _deviceController;
            }

            _deviceController = DependencyResolutionGuard.FindInterface<IDeviceController>(GetComponents<MonoBehaviour>());
            if (_deviceController != null)
            {
                return _deviceController;
            }

            _deviceController = DependencyResolutionGuard.FindInterface<IDeviceController>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            return _deviceController;
        }

        private PlayerWeaponController ResolveWeaponController()
        {
            if (!IsReferenceAlive(_weaponController))
            {
                _weaponController = null;
            }

            if (_weaponController != null)
            {
                return _weaponController;
            }

            _weaponController = GetComponent<PlayerWeaponController>();
            if (_weaponController != null)
            {
                return _weaponController;
            }

            _weaponController = FindAnyObjectByType<PlayerWeaponController>();
            return _weaponController;
        }

        private WeaponRegistry ResolveWeaponRegistry()
        {
            if (!IsReferenceAlive(_weaponRegistry))
            {
                _weaponRegistry = null;
            }

            if (_weaponRegistry != null)
            {
                return _weaponRegistry;
            }

            _weaponRegistry = GetComponent<WeaponRegistry>();
            if (_weaponRegistry != null)
            {
                return _weaponRegistry;
            }

            _weaponRegistry = FindAnyObjectByType<WeaponRegistry>();
            return _weaponRegistry;
        }

        private static bool IsReferenceAlive(object instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (instance is UnityEngine.Object unityObject && unityObject == null)
            {
                return false;
            }

            return true;
        }

        private void SetMenuOpen(bool isOpen)
        {
            if (_isOpen == isOpen)
            {
                return;
            }

            _isOpen = isOpen;
            ResolveUiStateEvents()?.RaiseTabInventoryVisibilityChanged(_isOpen);
        }

        private void CloseAllOpenMenus()
        {
            if (StorageUiSession.IsOpen)
            {
                StorageUiSession.Close();
            }

            RuntimeKernelBootstrapper.ShopEvents?.RaiseShopTradeClosed();

            var uiStateEvents = ResolveUiStateEvents();
            uiStateEvents?.RaiseWorkbenchMenuVisibilityChanged(false);

            SetMenuOpen(false);
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                _uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            }

            return _uiStateEvents;
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_useRuntimeKernelInventoryEvents)
            {
                SubscribeToInventoryEvents(ResolveInventoryEvents());
            }

            if (_useRuntimeKernelUiStateEvents)
            {
                ResolveUiStateEvents()?.RaiseTabInventoryVisibilityChanged(_isOpen);
            }
        }

        private IInventoryEvents ResolveInventoryEvents()
        {
            if (_useRuntimeKernelInventoryEvents)
            {
                var runtimeInventoryEvents = RuntimeKernelBootstrapper.InventoryEvents;
                if (!ReferenceEquals(_inventoryEvents, runtimeInventoryEvents))
                {
                    _inventoryEvents = runtimeInventoryEvents;
                    SubscribeToInventoryEvents(_inventoryEvents);
                }
                else if (!ReferenceEquals(_subscribedInventoryEvents, _inventoryEvents))
                {
                    SubscribeToInventoryEvents(_inventoryEvents);
                }

                return _inventoryEvents;
            }

            if (!ReferenceEquals(_subscribedInventoryEvents, _inventoryEvents))
            {
                SubscribeToInventoryEvents(_inventoryEvents);
            }

            return _inventoryEvents;
        }

        private void SubscribeToInventoryEvents(IInventoryEvents inventoryEvents)
        {
            if (inventoryEvents == null)
            {
                UnsubscribeFromInventoryEvents();
                return;
            }

            if (ReferenceEquals(_subscribedInventoryEvents, inventoryEvents))
            {
                return;
            }

            UnsubscribeFromInventoryEvents();
            _subscribedInventoryEvents = inventoryEvents;
            _subscribedInventoryEvents.OnInventoryChanged += HandleInventoryChanged;
        }

        private void UnsubscribeFromInventoryEvents()
        {
            if (_subscribedInventoryEvents == null)
            {
                return;
            }

            _subscribedInventoryEvents.OnInventoryChanged -= HandleInventoryChanged;
            _subscribedInventoryEvents = null;
        }

        private void SubscribeToRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void UnsubscribeFromRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
        }
    }
}
