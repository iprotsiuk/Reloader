using System;
using System.Collections.Generic;
using Reloader.Core.UI;
using Reloader.UI;
using Reloader.UI.Toolkit;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryViewBinder : IUiViewBinder
    {
        private VisualElement _panel;
        private VisualElement _rail;
        private VisualElement _tabBar;
        private VisualElement _workspace;
        private VisualElement _gridArea;
        private VisualElement _detailPane;
        private VisualElement _detailPaneGeneric;
        private VisualElement _detailPaneContracts;
        private Label _detailPaneBasePayout;
        private Label _detailPaneBonusConditions;
        private Label _detailPaneRestrictions;
        private Label _detailPaneFailureConditions;
        private Label _detailPaneRewardState;
        private Label _headerMeta;
        private VisualElement _backpackGrid;
        private VisualElement _beltGrid;
        private VisualElement[] _beltSlots = Array.Empty<VisualElement>();
        private VisualElement[] _backpackSlots = Array.Empty<VisualElement>();
        private readonly List<VisualElement> _backpackSlotElements = new List<VisualElement>();
        private readonly List<VisualElement> _beltSlotElements = new List<VisualElement>();
        private readonly List<Button> _tabs = new List<Button>(4);
        private readonly List<VisualElement> _deviceButtonRows = new List<VisualElement>(2);
        private VisualElement _tooltip;
        private Label _tooltipTitle;
        private Label _tooltipSpecs;
        private VisualElement _inventorySection;
        private VisualElement _contractsFeed;
        private VisualElement _contractsRow;
        private VisualElement _contractsActive;
        private Label _contractsStatus;
        private Label _contractsTitle;
        private Label _contractsSummary;
        private Label _contractsTarget;
        private Label _contractsPayout;
        private Label _contractsBriefing;
        private Button _contractsActivePrimaryActionButton;
        private VisualElement _questsSection;
        private VisualElement _journalSection;
        private VisualElement _calendarSection;
        private VisualElement _deviceSection;
        private VisualElement _attachmentsSection;
        private VisualElement _deviceNotes;
        private Label _attachmentsWeaponName;
        private Label _attachmentsStatus;
        private DropdownField _attachmentsSlotDropdown;
        private DropdownField _attachmentsItemDropdown;
        private Label _deviceSelectedTargetValue;
        private Label _deviceShotCountValue;
        private Label _deviceSpreadValue;
        private Label _deviceMoaValue;
        private Label _deviceSavedGroupsValue;
        private Label _deviceInstallFeedbackText;
        private VisualElement _deviceSessionHistory;
        private Button _tabInventory;
        private Button _contractsAcceptButton;
        private Button _boundContractsAcceptButton;
        private Button _boundContractsActivePrimaryActionButton;
        private Button _tabQuests;
        private Button _tabJournal;
        private Button _tabCalendar;
        private Button _tabDevice;
        private Button _deviceChooseTargetButton;
        private Button _deviceSaveGroupButton;
        private Button _deviceClearGroupButton;
        private Button _deviceInstallHooksButton;
        private Button _deviceUninstallHooksButton;
        private Button _attachmentsApplyButton;
        private Button _attachmentsBackButton;
        private readonly Dictionary<string, Action> _intentInvokeByTestId = new Dictionary<string, Action>();
        private string[] _beltSlotItemIds = Array.Empty<string>();
        private int[] _beltSlotMaxStacks = Array.Empty<int>();
        private int[] _beltSlotQuantities = Array.Empty<int>();
        private string[] _backpackSlotItemIds = Array.Empty<string>();
        private int[] _backpackSlotMaxStacks = Array.Empty<int>();
        private int[] _backpackSlotQuantities = Array.Empty<int>();
        private string _dragSourceContainer;
        private int _dragSourceIndex = -1;
        private string _dragSourceItemId;
        private string _dragTargetContainer;
        private int _dragTargetIndex = -1;
        private bool _suppressNextClick;
        private int _activePointerId = -1;
        private float _resolvedSlotSize = 46f;
        private InventoryItemTooltipPresenter _tooltipPresenter;
        private bool _isHoverTooltipActive;
        private string _hoverTooltipContainer;
        private int _hoverTooltipSlotIndex = -1;
        private string _hoverTooltipItemId;
        private int _hoverTooltipQuantity;
        private int _hoverTooltipMaxStack = 1;
        private Vector2 _hoverTooltipPanelPosition;
        private string _contractsActiveActionIntentKey;

        private const float MinSlotSize = 16.5f;
        private const float MaxSlotSize = 42f;
        private const float SlotGap = 4f;
        private const float MinTabWidth = 56f;
        private const float MaxTabWidth = 180f;
        private const float DeviceButtonGap = 6f;
        private const float MinDeviceButtonWidth = 82f;
        private const float MaxDeviceButtonWidth = 220f;
        private const float InventoryVerticalChrome = 56f;
        private const float MinDetailPaneWidth = 132f;
        private const float MinWorkspaceWidthWithDetailPane = 180f;
        private const float MinPanelWidthForDetailPane = 390f;
        private const float DefaultRailGap = 6f;
        private const float DefaultWorkspaceGap = 8f;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int beltSlotCount, int backpackSlotCount)
        {
            _panel = root?.Q<VisualElement>("inventory__panel");
            _rail = root?.Q<VisualElement>("inventory__rail");
            _tabBar = root?.Q<VisualElement>("inventory__tabbar");
            _workspace = root?.Q<VisualElement>("inventory__workspace");
            _gridArea = root?.Q<VisualElement>(className: "inventory__grid-area");
            _detailPane = root?.Q<VisualElement>("inventory__detail-pane");
            _detailPaneGeneric = root?.Q<VisualElement>("inventory__detail-pane-generic");
            _detailPaneContracts = root?.Q<VisualElement>("inventory__detail-pane-contracts");
            _detailPaneBasePayout = root?.Q<Label>("inventory__detail-pane-base-payout");
            _detailPaneBonusConditions = root?.Q<Label>("inventory__detail-pane-bonus-conditions");
            _detailPaneRestrictions = root?.Q<Label>("inventory__detail-pane-restrictions");
            _detailPaneFailureConditions = root?.Q<Label>("inventory__detail-pane-failure-conditions");
            _detailPaneRewardState = root?.Q<Label>("inventory__detail-pane-reward-state");
            _headerMeta = root?.Q<Label>("inventory__header-meta");
            _backpackGrid = root?.Q<VisualElement>("inventory__backpack-grid");
            _beltGrid = root?.Q<VisualElement>("inventory__grid-row--belt")
                ?? root?.Q<VisualElement>(className: "inventory__grid-row--belt");
            _tooltip = root?.Q<VisualElement>("inventory__tooltip");
            _tooltipTitle = root?.Q<Label>("inventory__tooltip-title");
            _tooltipSpecs = root?.Q<Label>("inventory__tooltip-specs");
            if (_tooltip != null)
            {
                _tooltip.pickingMode = PickingMode.Ignore;
            }
            if (_tooltip != null && _tooltipSpecs == null)
            {
                _tooltipSpecs = new Label { name = "inventory__tooltip-specs" };
                _tooltipSpecs.AddToClassList("inventory__tooltip-specs");
                _tooltip.Add(_tooltipSpecs);
            }
            _tooltipPresenter = InventoryItemTooltipPresenter.CreateOrBind(root, _panel ?? root, "inventory");
            _inventorySection = root?.Q<VisualElement>("inventory__section-inventory");
            _questsSection = root?.Q<VisualElement>("inventory__section-quests");
            _journalSection = root?.Q<VisualElement>("inventory__section-journal");
            _calendarSection = root?.Q<VisualElement>("inventory__section-calendar");
            _deviceSection = root?.Q<VisualElement>("inventory__section-device");
            _attachmentsSection = root?.Q<VisualElement>("inventory__section-attachments");
            _deviceNotes = root?.Q<VisualElement>("inventory__device-notes");
            _attachmentsWeaponName = root?.Q<Label>("inventory__attachments-weapon-name");
            _attachmentsStatus = root?.Q<Label>("inventory__attachments-status");
            _attachmentsSlotDropdown = root?.Q<DropdownField>("inventory__attachments-slot-dropdown");
            _attachmentsItemDropdown = root?.Q<DropdownField>("inventory__attachments-item-dropdown");
            _deviceSelectedTargetValue = root?.Q<Label>("inventory__device-selected-target-value");
            _deviceShotCountValue = root?.Q<Label>("inventory__device-shot-count-value");
            _deviceSpreadValue = root?.Q<Label>("inventory__device-spread-value");
            _deviceMoaValue = root?.Q<Label>("inventory__device-moa-value");
            _deviceSavedGroupsValue = root?.Q<Label>("inventory__device-saved-groups-value");
            _deviceInstallFeedbackText = root?.Q<Label>("inventory__device-install-feedback-text");
            _deviceSessionHistory = root?.Q<VisualElement>("inventory__device-session-history");
            _tabInventory = root?.Q<Button>("inventory__tab-inventory");
            _tabQuests = root?.Q<Button>("inventory__tab-quests");
            _tabJournal = root?.Q<Button>("inventory__tab-journal");
            _tabCalendar = root?.Q<Button>("inventory__tab-calendar");
            _tabDevice = root?.Q<Button>("inventory__tab-device");
            _deviceChooseTargetButton = root?.Q<Button>("inventory__device-choose-target");
            _deviceSaveGroupButton = root?.Q<Button>("inventory__device-save-group");
            _deviceClearGroupButton = root?.Q<Button>("inventory__device-clear-group");
            _deviceInstallHooksButton = root?.Q<Button>("inventory__device-install-hooks");
            _deviceUninstallHooksButton = root?.Q<Button>("inventory__device-uninstall-hooks");
            _attachmentsApplyButton = root?.Q<Button>("inventory__attachments-apply");
            _attachmentsBackButton = root?.Q<Button>("inventory__attachments-back");
            EnsureContractsSectionBindings();
            _intentInvokeByTestId.Clear();
            _tabs.Clear();
            _deviceButtonRows.Clear();
            AddTab(_tabInventory);
            AddTab(_tabQuests);
            AddTab(_tabJournal);
            AddTab(_tabCalendar);
            AddTab(_tabDevice);

            if (_deviceSection != null)
            {
                var buttonRows = _deviceSection.Query<VisualElement>(className: "inventory__device-button-row").ToList();
                for (var i = 0; i < buttonRows.Count; i++)
                {
                    if (buttonRows[i] != null)
                    {
                        _deviceButtonRows.Add(buttonRows[i]);
                    }
                }
            }

            if (_attachmentsSection != null)
            {
                var buttonRows = _attachmentsSection.Query<VisualElement>(className: "inventory__device-button-row").ToList();
                for (var i = 0; i < buttonRows.Count; i++)
                {
                    if (buttonRows[i] != null)
                    {
                        _deviceButtonRows.Add(buttonRows[i]);
                    }
                }
            }

            RegisterTabIntents();
            RegisterDeviceActionIntents();
            RegisterAttachmentActionIntents();

            _beltSlots = new VisualElement[Math.Max(0, beltSlotCount)];
            _beltSlotItemIds = new string[_beltSlots.Length];
            _beltSlotMaxStacks = new int[_beltSlots.Length];
            _beltSlotQuantities = new int[_beltSlots.Length];
            for (var i = 0; i < _beltSlots.Length; i++)
            {
                var slot = root?.Q<VisualElement>($"inventory__belt-slot-{i}");
                _beltSlots[i] = slot;
                if (slot != null)
                {
                    var capturedIndex = i;
                    slot.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button == 0)
                        {
                            TryPointerDown("belt", capturedIndex, evt.pointerId);
                        }
                    });
                    slot.RegisterCallback<PointerUpEvent>(evt => TryPointerUp("belt", capturedIndex, evt.pointerId));
                    slot.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button == 1 && TryRaiseAttachmentContextIntent("belt", capturedIndex))
                        {
                            evt.StopImmediatePropagation();
                        }
                    });
                    slot.RegisterCallback<PointerEnterEvent>(evt => TryPointerEnter("belt", capturedIndex, evt.position));
                    slot.RegisterCallback<PointerMoveEvent>(evt => TryPointerMove("belt", capturedIndex, evt.position));
                    slot.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
                    slot.RegisterCallback<ClickEvent>(_ => HandleSlotClick("belt", capturedIndex));
                }
            }

            _backpackSlots = new VisualElement[Math.Max(0, backpackSlotCount)];
            _backpackSlotItemIds = new string[_backpackSlots.Length];
            _backpackSlotMaxStacks = new int[_backpackSlots.Length];
            _backpackSlotQuantities = new int[_backpackSlots.Length];
            for (var i = 0; i < _backpackSlots.Length; i++)
            {
                var slot = root?.Q<VisualElement>($"inventory__backpack-slot-{i}");
                _backpackSlots[i] = slot;
                if (slot != null)
                {
                    var capturedIndex = i;
                    slot.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button == 0)
                        {
                            TryPointerDown("backpack", capturedIndex, evt.pointerId);
                        }
                    });
                    slot.RegisterCallback<PointerUpEvent>(evt => TryPointerUp("backpack", capturedIndex, evt.pointerId));
                    slot.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button == 1 && TryRaiseAttachmentContextIntent("backpack", capturedIndex))
                        {
                            evt.StopImmediatePropagation();
                        }
                    });
                    slot.RegisterCallback<PointerEnterEvent>(evt => TryPointerEnter("backpack", capturedIndex, evt.position));
                    slot.RegisterCallback<PointerMoveEvent>(evt => TryPointerMove("backpack", capturedIndex, evt.position));
                    slot.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
                    slot.RegisterCallback<ClickEvent>(_ => HandleSlotClick("backpack", capturedIndex));
                }
            }

            CacheSlotElements();
            RegisterResponsiveCallbacks();
            RegisterDragDropOutsideCallbacks();
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
            RenderSlotVisuals(_beltSlots, inventoryState.BeltSlots, "belt");
            RenderSlotVisuals(_backpackSlots, inventoryState.BackpackSlots, "backpack");
            if (_headerMeta != null)
            {
                _headerMeta.text = inventoryState.HeaderMetaText;
            }

            CacheSlotItemIds(_beltSlotItemIds, inventoryState.BeltSlots);
            CacheSlotStackData(_beltSlotMaxStacks, _beltSlotQuantities, inventoryState.BeltSlots);
            CacheSlotItemIds(_backpackSlotItemIds, inventoryState.BackpackSlots);
            CacheSlotStackData(_backpackSlotMaxStacks, _backpackSlotQuantities, inventoryState.BackpackSlots);
            ApplySectionVisibility(inventoryState.ActiveSection);
            if (_contractsStatus != null)
            {
                _contractsStatus.text = inventoryState.ContractPanel.StatusText;
            }

            if (_contractsTitle != null)
            {
                _contractsTitle.text = inventoryState.ContractPanel.TitleText;
            }

            if (_contractsSummary != null)
            {
                _contractsSummary.text = inventoryState.ContractPanel.SummaryText;
            }

            if (_contractsTarget != null)
            {
                _contractsTarget.text = inventoryState.ContractPanel.TargetText;
            }

            if (_contractsPayout != null)
            {
                _contractsPayout.text = inventoryState.ContractPanel.PayoutText;
            }

            if (_contractsBriefing != null)
            {
                _contractsBriefing.text = inventoryState.ContractPanel.BriefingText;
            }

            if (_detailPaneBasePayout != null)
            {
                _detailPaneBasePayout.text = inventoryState.ContractPanel.BasePayoutText;
            }

            if (_detailPaneBonusConditions != null)
            {
                _detailPaneBonusConditions.text = inventoryState.ContractPanel.BonusConditionsText;
            }

            if (_detailPaneRestrictions != null)
            {
                _detailPaneRestrictions.text = inventoryState.ContractPanel.RestrictionsText;
            }

            if (_detailPaneFailureConditions != null)
            {
                _detailPaneFailureConditions.text = inventoryState.ContractPanel.FailureConditionsText;
            }

            if (_detailPaneRewardState != null)
            {
                _detailPaneRewardState.text = inventoryState.ContractPanel.RewardStateText;
            }

            var isPostedOffer = inventoryState.ContractPanel.Mode == TabInventoryUiState.ContractPanelMode.PostedOffer;
            var isActiveContract = inventoryState.ContractPanel.Mode == TabInventoryUiState.ContractPanelMode.ActiveContract;
            if (_contractsFeed != null)
            {
                _contractsFeed.style.display = isPostedOffer ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_contractsRow != null)
            {
                _contractsRow.style.display = isPostedOffer ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_contractsActive != null)
            {
                _contractsActive.style.display = isActiveContract ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_contractsAcceptButton != null)
            {
                _contractsAcceptButton.style.display = isPostedOffer ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _contractsAcceptButton?.SetEnabled(inventoryState.ContractPanel.CanAccept);
            var showActivePrimaryAction = isActiveContract
                && (inventoryState.ContractPanel.CanCancel || inventoryState.ContractPanel.CanClaimReward);
            _contractsActiveActionIntentKey = inventoryState.ContractPanel.CanClaimReward
                ? "tab.inventory.contracts.claim"
                : inventoryState.ContractPanel.CanCancel
                    ? "tab.inventory.contracts.cancel"
                    : null;
            if (_contractsActivePrimaryActionButton != null)
            {
                _contractsActivePrimaryActionButton.style.display = showActivePrimaryAction ? DisplayStyle.Flex : DisplayStyle.None;
                _contractsActivePrimaryActionButton.text = inventoryState.ContractPanel.CanClaimReward
                    ? "Claim Reward"
                    : "Cancel Contract";
                _contractsActivePrimaryActionButton.SetEnabled(showActivePrimaryAction);
            }

            if (_deviceNotes != null)
            {
                _deviceNotes.style.display = inventoryState.DeviceNotesVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_deviceSelectedTargetValue != null)
            {
                _deviceSelectedTargetValue.text = inventoryState.DeviceSelectedTargetText;
            }

            if (_deviceShotCountValue != null)
            {
                _deviceShotCountValue.text = inventoryState.DeviceShotCountText;
            }

            if (_deviceSpreadValue != null)
            {
                _deviceSpreadValue.text = inventoryState.DeviceSpreadText;
            }

            if (_deviceMoaValue != null)
            {
                _deviceMoaValue.text = inventoryState.DeviceMoaText;
            }

            if (_deviceSavedGroupsValue != null)
            {
                _deviceSavedGroupsValue.text = inventoryState.DeviceSavedGroupsText;
            }

            if (_deviceInstallFeedbackText != null)
            {
                _deviceInstallFeedbackText.text = inventoryState.DeviceInstallFeedbackText;
            }

            _deviceSaveGroupButton?.SetEnabled(inventoryState.DeviceCanSaveGroup);
            _deviceClearGroupButton?.SetEnabled(inventoryState.DeviceCanClearGroup);
            _deviceInstallHooksButton?.SetEnabled(inventoryState.DeviceCanInstallHooks);
            _deviceUninstallHooksButton?.SetEnabled(inventoryState.DeviceCanUninstallHooks);
            RenderDeviceSessionHistory(inventoryState.DeviceSessionHistoryEntries);
            if (_attachmentsWeaponName != null)
            {
                _attachmentsWeaponName.text = inventoryState.AttachmentsWeaponName;
            }

            if (_attachmentsStatus != null)
            {
                _attachmentsStatus.text = inventoryState.AttachmentsStatusText;
            }

            if (_attachmentsSlotDropdown != null)
            {
                _attachmentsSlotDropdown.choices = new List<string>(inventoryState.AttachmentSlotOptions);
                _attachmentsSlotDropdown.SetValueWithoutNotify(inventoryState.AttachmentsSelectedSlot);
            }

            if (_attachmentsItemDropdown != null)
            {
                _attachmentsItemDropdown.choices = new List<string>(inventoryState.AttachmentItemOptions);
                _attachmentsItemDropdown.SetValueWithoutNotify(inventoryState.AttachmentsSelectedItem);
            }

            _attachmentsApplyButton?.SetEnabled(inventoryState.AttachmentsCanApply);

            if (!inventoryState.IsOpen)
            {
                HideTooltip();
            }
            else
            {
                RevalidateHoverTooltip(inventoryState);
            }

            if (_tooltip != null && (!_isHoverTooltipActive || inventoryState.TooltipVisible))
            {
                _tooltip.style.display = inventoryState.TooltipVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_tooltipTitle != null && (!_isHoverTooltipActive || inventoryState.TooltipVisible))
            {
                _tooltipTitle.text = inventoryState.TooltipTitle;
            }

            if (_tooltipSpecs != null && !inventoryState.TooltipVisible && !_isHoverTooltipActive)
            {
                _tooltipSpecs.text = string.Empty;
            }
        }

        private void RegisterTabIntents()
        {
            RegisterIntent(_tabInventory, "test.tab.inventory", "tab.menu.select", "inventory");
            RegisterIntent(_tabQuests, "test.tab.contracts", "tab.menu.select", "contracts");
            RegisterIntent(_tabCalendar, "test.tab.calendar", "tab.menu.select", "calendar");
            RegisterIntent(_tabJournal, "test.tab.journal", "tab.menu.select", "journal");
            RegisterIntent(_tabDevice, "test.tab.device", "tab.menu.select", "device");
        }

        private void EnsureContractsSectionBindings()
        {
            if (_tabQuests != null)
            {
                _tabQuests.text = "Contracts";
            }

            if (_questsSection == null)
            {
                return;
            }

            _contractsFeed = _questsSection.Q<VisualElement>("inventory__contracts-feed");
            _contractsRow = _questsSection.Q<VisualElement>("inventory__contracts-row");
            _contractsActive = _questsSection.Q<VisualElement>("inventory__contracts-active");
            _contractsStatus = _questsSection.Q<Label>("inventory__contracts-status");
            _contractsTitle = _questsSection.Q<Label>("inventory__contracts-title");
            _contractsSummary = _questsSection.Q<Label>("inventory__contracts-summary");
            _contractsTarget = _questsSection.Q<Label>("inventory__contracts-target");
            _contractsPayout = _questsSection.Q<Label>("inventory__contracts-payout");
            _contractsBriefing = _questsSection.Q<Label>("inventory__contracts-briefing");
            _contractsAcceptButton = _questsSection.Q<Button>("inventory__contracts-primary-action");
            _contractsActivePrimaryActionButton = _questsSection.Q<Button>("inventory__contracts-active-primary-action");
            if (_contractsFeed != null
                && _contractsRow != null
                && _contractsActive != null
                && _contractsStatus != null
                && _contractsTitle != null
                && _contractsSummary != null
                && _contractsTarget != null
                && _contractsPayout != null
                && _contractsBriefing != null
                && _contractsAcceptButton != null
                && _contractsActivePrimaryActionButton != null)
            {
                EnsureContractsAcceptIntentBinding();
                EnsureContractsActivePrimaryActionIntentBinding();
                return;
            }

            _questsSection.Clear();
            var panel = new VisualElement { name = "inventory__contracts-shell" };
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.marginTop = 4f;

            _contractsStatus = new Label { name = "inventory__contracts-status" };
            _contractsFeed = new VisualElement { name = "inventory__contracts-feed" };
            _contractsFeed.style.flexDirection = FlexDirection.Column;
            _contractsRow = new VisualElement { name = "inventory__contracts-row" };
            _contractsRow.style.flexDirection = FlexDirection.Row;
            _contractsTitle = new Label { name = "inventory__contracts-title" };
            _contractsSummary = new Label { name = "inventory__contracts-summary" };
            _contractsTarget = new Label { name = "inventory__contracts-target" };
            _contractsPayout = new Label { name = "inventory__contracts-payout" };
            _contractsActive = new VisualElement { name = "inventory__contracts-active" };
            _contractsActive.style.flexDirection = FlexDirection.Column;
            _contractsBriefing = new Label { name = "inventory__contracts-briefing" };
            _contractsBriefing.style.whiteSpace = WhiteSpace.Normal;
            _contractsAcceptButton = new Button { name = "inventory__contracts-primary-action", text = "Accept Contract" };
            _contractsAcceptButton.AddToClassList("inventory__contracts-primary-action");
            _contractsActivePrimaryActionButton = new Button { name = "inventory__contracts-active-primary-action", text = "Cancel Contract" };
            _contractsActivePrimaryActionButton.AddToClassList("inventory__contracts-active-primary-action");
            EnsureContractsAcceptIntentBinding();
            EnsureContractsActivePrimaryActionIntentBinding();

            panel.Add(_contractsStatus);

            var postedCopy = new VisualElement();
            postedCopy.style.flexDirection = FlexDirection.Column;
            postedCopy.style.flexGrow = 1f;
            postedCopy.Add(_contractsTitle);
            postedCopy.Add(_contractsSummary);

            _contractsRow.Add(postedCopy);
            _contractsRow.Add(_contractsPayout);
            _contractsRow.Add(_contractsAcceptButton);
            _contractsFeed.Add(_contractsRow);

            _contractsActive.Add(_contractsTarget);
            _contractsActive.Add(_contractsBriefing);
            _contractsActive.Add(_contractsActivePrimaryActionButton);

            panel.Add(_contractsFeed);
            panel.Add(_contractsActive);
            _questsSection.Add(panel);
        }

        private void EnsureContractsAcceptIntentBinding()
        {
            if (_contractsAcceptButton == null || ReferenceEquals(_boundContractsAcceptButton, _contractsAcceptButton))
            {
                return;
            }

            _contractsAcceptButton.clicked += RaiseContractsAcceptIntent;
            _boundContractsAcceptButton = _contractsAcceptButton;
        }

        private void RaiseContractsAcceptIntent()
        {
            IntentRaised?.Invoke(new UiIntent("tab.inventory.contracts.accept", null));
        }

        private void EnsureContractsActivePrimaryActionIntentBinding()
        {
            if (_contractsActivePrimaryActionButton == null
                || ReferenceEquals(_boundContractsActivePrimaryActionButton, _contractsActivePrimaryActionButton))
            {
                return;
            }

            _contractsActivePrimaryActionButton.clicked += RaiseContractsActivePrimaryActionIntent;
            _boundContractsActivePrimaryActionButton = _contractsActivePrimaryActionButton;
        }

        private void RaiseContractsActivePrimaryActionIntent()
        {
            if (string.IsNullOrWhiteSpace(_contractsActiveActionIntentKey))
            {
                return;
            }

            IntentRaised?.Invoke(new UiIntent(_contractsActiveActionIntentKey, null));
        }


        private void RegisterDeviceActionIntents()
        {
            RegisterIntent(_deviceChooseTargetButton, "test.device.choose-target", "tab.inventory.device.choose-target", null);
            RegisterIntent(_deviceSaveGroupButton, "test.device.save-group", "tab.inventory.device.save-group", null);
            RegisterIntent(_deviceClearGroupButton, "test.device.clear-group", "tab.inventory.device.clear-group", null);
            RegisterIntent(_deviceInstallHooksButton, "test.device.install-hooks", "tab.inventory.device.install-hooks", null);
            RegisterIntent(_deviceUninstallHooksButton, "test.device.uninstall-hooks", "tab.inventory.device.uninstall-hooks", null);
        }

        private void RegisterAttachmentActionIntents()
        {
            RegisterIntent(_attachmentsApplyButton, "test.attachments.apply", "tab.inventory.attachments.apply", null);
            RegisterIntent(_attachmentsBackButton, "test.attachments.back", "tab.inventory.attachments.back", null);
            if (_attachmentsSlotDropdown != null)
            {
                _attachmentsSlotDropdown.RegisterValueChangedCallback(evt =>
                    IntentRaised?.Invoke(new UiIntent("tab.inventory.attachments.slot-selected", evt.newValue)));
            }

            if (_attachmentsItemDropdown != null)
            {
                _attachmentsItemDropdown.RegisterValueChangedCallback(evt =>
                    IntentRaised?.Invoke(new UiIntent("tab.inventory.attachments.item-selected", evt.newValue)));
            }
        }

        private void RegisterIntent(Button button, string testId, string key, object payload)
        {
            if (button == null)
            {
                return;
            }

            Action invoke = () => IntentRaised?.Invoke(new UiIntent(key, payload));
            button.clicked += () => invoke();
            _intentInvokeByTestId[testId] = invoke;
        }

        private void RegisterResponsiveCallbacks()
        {
            _panel?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _tabBar?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _gridArea?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _backpackGrid?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _beltGrid?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
            _deviceSection?.RegisterCallback<GeometryChangedEvent>(_ => ApplyResponsiveLayout());
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
            ApplyResponsiveDetailPane();
            ApplyResponsiveSlots(_backpackGrid, _backpackSlotElements, preferredColumns: 8, verticalRowsInLayout: 3);
            ApplyResponsiveBeltSlots();
            ApplyResponsiveTabs();
            ApplyResponsiveDeviceButtons();
        }

        private void ApplyResponsiveDetailPane()
        {
            if (_panel == null || _workspace == null || _detailPane == null)
            {
                return;
            }

            var panelWidth = ResolveAssignedWidth(_panel);
            if (panelWidth <= 0f)
            {
                return;
            }

            var railWidth = ResolveAssignedWidth(_rail);
            var railGap = ResolveResolvedMarginRight(_rail, DefaultRailGap);
            var detailGap = ResolveResolvedMarginRight(_workspace, DefaultWorkspaceGap);
            var requiredPanelWidth = Mathf.Max(
                MinPanelWidthForDetailPane,
                railWidth
                + railGap
                + detailGap
                + ResolveAssignedWidth(_detailPane, MinDetailPaneWidth)
                + MinWorkspaceWidthWithDetailPane);
            var shouldCollapse = panelWidth < requiredPanelWidth;

            _detailPane.style.display = shouldCollapse ? DisplayStyle.None : DisplayStyle.Flex;
            _workspace.style.marginRight = shouldCollapse ? 0f : StyleKeyword.Null;
        }

        private static float ResolveAssignedWidth(VisualElement element, float fallbackWidth = 0f)
        {
            if (element == null)
            {
                return fallbackWidth;
            }

            var styledWidth = element.style.width;
            if (styledWidth.keyword == StyleKeyword.Null
                || styledWidth.keyword == StyleKeyword.Auto)
            {
                var resolvedWidth = Mathf.Max(element.contentRect.width, element.resolvedStyle.width);
                if (resolvedWidth > 0f)
                {
                    return resolvedWidth;
                }

                var styledMinWidth = element.style.minWidth;
                if (styledMinWidth.keyword != StyleKeyword.Null
                    && styledMinWidth.keyword != StyleKeyword.Auto
                    && styledMinWidth.value.value > 0f)
                {
                    return styledMinWidth.value.value;
                }

                return fallbackWidth;
            }

            return styledWidth.value.value;
        }

        private static float ResolveAssignedLength(StyleLength length)
        {
            return length.keyword == StyleKeyword.Null || length.keyword == StyleKeyword.Auto
                ? 0f
                : length.value.value;
        }

        private static float ResolveResolvedMarginRight(VisualElement element, float authoredFallback)
        {
            if (element == null)
            {
                return authoredFallback;
            }

            var resolvedMarginRight = element.resolvedStyle.marginRight;
            if (resolvedMarginRight > 0f)
            {
                return resolvedMarginRight;
            }

            var assignedMarginRight = ResolveAssignedLength(element.style.marginRight);
            return assignedMarginRight > 0f ? assignedMarginRight : authoredFallback;
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

        private void ApplyResponsiveSlots(VisualElement container, List<VisualElement> slots, int preferredColumns, int verticalRowsInLayout)
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
            var resolvedByWidth = Mathf.Clamp(maxWidthPerSlot, MinSlotSize, MaxSlotSize);
            var resolvedByHeight = ResolveSlotSizeByHeight(verticalRowsInLayout);
            _resolvedSlotSize = Mathf.Clamp(Mathf.Min(resolvedByWidth, resolvedByHeight), MinSlotSize, MaxSlotSize);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.style.width = _resolvedSlotSize;
                slot.style.height = _resolvedSlotSize;
            }
        }

        private float ResolveSlotSizeByHeight(int verticalRowsInLayout)
        {
            var rows = Mathf.Max(1, verticalRowsInLayout);
            var gridHeight = _gridArea?.contentRect.height ?? 0f;
            if (gridHeight <= 0f)
            {
                return MaxSlotSize;
            }

            if (gridHeight < 200f)
            {
                return MaxSlotSize;
            }

            var availableHeight = Mathf.Max(0f, gridHeight - InventoryVerticalChrome);
            var gapBudget = Mathf.Max(0f, (rows - 1) * SlotGap);
            var perRow = (availableHeight - gapBudget) / rows;
            if (perRow <= 0f)
            {
                return MinSlotSize;
            }

            return Mathf.Clamp(perRow, MinSlotSize, MaxSlotSize);
        }

        private void ApplyResponsiveTabs()
        {
            if (_tabBar == null || _tabs.Count == 0)
            {
                return;
            }

            var barWidth = ResolveAssignedWidth(_tabBar);
            if (barWidth <= 0f)
            {
                return;
            }

            var isRailLayout = barWidth <= 180f;
            var totalGap = (_tabs.Count - 1) * 6f;
            var perTabWidth = (barWidth - totalGap) / _tabs.Count;
            var resolvedWidth = isRailLayout
                ? Mathf.Clamp(barWidth, 42f, 60f)
                : Mathf.Clamp(perTabWidth, MinTabWidth, MaxTabWidth);
            var resolvedHeight = isRailLayout
                ? Mathf.Clamp(_resolvedSlotSize * 0.88f, 38f, 46f)
                : Mathf.Clamp(_resolvedSlotSize * 0.56f, 20f, 28f);
            var resolvedFontSize = isRailLayout
                ? 0f
                : Mathf.Clamp(_resolvedSlotSize * 0.24f, 9f, 12f);

            for (var i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];
                tab.style.width = resolvedWidth;
                tab.style.height = resolvedHeight;
                tab.style.fontSize = resolvedFontSize;
                tab.style.paddingLeft = isRailLayout ? 0f : StyleKeyword.Null;
                tab.style.paddingRight = isRailLayout ? 0f : StyleKeyword.Null;
            }
        }

        private void ApplyResponsiveDeviceButtons()
        {
            if (_deviceButtonRows.Count == 0)
            {
                return;
            }

            var resolvedHeight = Mathf.Clamp(_resolvedSlotSize * 0.56f, 22f, 30f);
            var resolvedFontSize = Mathf.Clamp(_resolvedSlotSize * 0.24f, 9f, 12f);

            for (var rowIndex = 0; rowIndex < _deviceButtonRows.Count; rowIndex++)
            {
                var row = _deviceButtonRows[rowIndex];
                if (row == null)
                {
                    continue;
                }

                var rowButtons = row.Query<Button>().ToList();
                if (rowButtons.Count == 0)
                {
                    continue;
                }

                var rowWidth = row.contentRect.width;
                if (rowWidth <= 0f)
                {
                    rowWidth = _deviceSection?.contentRect.width ?? 0f;
                }

                if (rowWidth <= 0f)
                {
                    continue;
                }

                var totalGap = (rowButtons.Count - 1) * DeviceButtonGap;
                var perButtonWidth = (rowWidth - totalGap) / rowButtons.Count;
                var resolvedWidth = Mathf.Clamp(perButtonWidth, MinDeviceButtonWidth, MaxDeviceButtonWidth);
                for (var buttonIndex = 0; buttonIndex < rowButtons.Count; buttonIndex++)
                {
                    var button = rowButtons[buttonIndex];
                    if (button == null)
                    {
                        continue;
                    }

                    button.style.width = resolvedWidth;
                    button.style.height = resolvedHeight;
                    button.style.fontSize = resolvedFontSize;
                }
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
            SetSectionVisibility(_questsSection, section == "contracts");
            SetSectionVisibility(_journalSection, section == "journal");
            SetSectionVisibility(_calendarSection, section == "calendar");
            SetSectionVisibility(_deviceSection, section == "device");
            SetSectionVisibility(_attachmentsSection, section == "attachments");
            SetSectionVisibility(_detailPaneGeneric, section != "contracts");
            SetSectionVisibility(_detailPaneContracts, section == "contracts");

            _tabInventory?.EnableInClassList("is-active", section == "inventory");
            _tabQuests?.EnableInClassList("is-active", section == "contracts");
            _tabJournal?.EnableInClassList("is-active", section == "journal");
            _tabCalendar?.EnableInClassList("is-active", section == "calendar");
            _tabDevice?.EnableInClassList("is-active", section == "device");
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

        private static void CacheSlotStackData(int[] maxStacks, int[] quantities, System.Collections.Generic.IReadOnlyList<TabInventoryUiState.SlotState> slots)
        {
            var limit = Math.Min(Math.Min(maxStacks.Length, quantities.Length), slots.Count);
            for (var i = 0; i < limit; i++)
            {
                quantities[i] = Math.Max(0, slots[i].Quantity);
                maxStacks[i] = Math.Max(1, slots[i].MaxStack);
            }
        }

        private static void RenderSlotVisuals(VisualElement[] slotElements, System.Collections.Generic.IReadOnlyList<TabInventoryUiState.SlotState> slots, string container)
        {
            var limit = Math.Min(slotElements.Length, slots.Count);
            for (var i = 0; i < limit; i++)
            {
                var slotElement = slotElements[i];
                if (slotElement == null)
                {
                    continue;
                }

                var slotState = slots[i];
                var contentName = $"inventory__slot-item-{container}-{i}";
                var iconName = $"inventory__slot-item-icon-{container}-{i}";
                var quantityName = $"inventory__slot-item-quantity-{container}-{i}";
                var hasVisual = slotState.IsOccupied && !string.IsNullOrWhiteSpace(slotState.ItemId);
                var content = slotElement.Q<VisualElement>(contentName);

                if (!hasVisual)
                {
                    content?.RemoveFromHierarchy();
                    continue;
                }

                if (content == null)
                {
                    content = new VisualElement
                    {
                        name = contentName,
                        pickingMode = PickingMode.Ignore
                    };
                    content.AddToClassList("inventory__slot-item");
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
                    icon.AddToClassList("inventory__slot-item-icon");
                    content.Add(icon);
                }

                var quantity = content.Q<Label>(quantityName);
                if (quantity == null)
                {
                    quantity = new Label
                    {
                        name = quantityName,
                        pickingMode = PickingMode.Ignore
                    };
                    quantity.AddToClassList("inventory__slot-item-quantity");
                    content.Add(quantity);
                }

                icon.style.backgroundImage = ItemIconResolver.ResolveBackground(slotState.ItemId);
                icon.EnableInClassList("is-missing", false);
                var showQuantity = slotState.Quantity > 1;
                quantity.style.display = showQuantity ? DisplayStyle.Flex : DisplayStyle.None;
                quantity.text = showQuantity ? slotState.Quantity.ToString() : string.Empty;
            }
        }

        public bool TryInteractSlotForTests(string container, int slotIndex)
        {
            return TryInteractSlot(container, slotIndex);
        }

        public bool TryRightClickSlotForTests(string container, int slotIndex)
        {
            return TryRaiseAttachmentContextIntent(container, slotIndex);
        }

        public bool TryPointerDownForTests(string container, int slotIndex)
        {
            return TryPointerDown(container, slotIndex, pointerId: -1);
        }

        public bool TryPointerEnterForTests(string container, int slotIndex)
        {
            return TryPointerEnter(container, slotIndex, Vector2.zero);
        }

        public bool TryPointerUpForTests(string container, int slotIndex)
        {
            return TryPointerUp(container, slotIndex);
        }

        public bool TryPointerUpOutsideForTests()
        {
            return TryPointerUpOutsideSlots();
        }

public bool TryInvokeTabSelectionForTests(string section)
        {
            var testId = section switch
            {
                "inventory" => "test.tab.inventory",
                "contracts" => "test.tab.contracts",
                "journal" => "test.tab.journal",
                "calendar" => "test.tab.calendar",
                "device" => "test.tab.device",
                _ => null
            };

            return TryInvokeIntentForTests(testId);
        }

        public bool TryInvokeDeviceActionForTests(string action)
        {
            var testId = action switch
            {
                "choose-target" => "test.device.choose-target",
                "save-group" => "test.device.save-group",
                "clear-group" => "test.device.clear-group",
                "install-hooks" => "test.device.install-hooks",
                "uninstall-hooks" => "test.device.uninstall-hooks",
                _ => null
            };

            return TryInvokeIntentForTests(testId);
        }

        public bool TryInvokeAttachmentActionForTests(string action)
        {
            var testId = action switch
            {
                "apply" => "test.attachments.apply",
                "back" => "test.attachments.back",
                _ => null
            };

            return TryInvokeIntentForTests(testId);
        }

        private bool TryInvokeIntentForTests(string testId)
        {
            if (string.IsNullOrWhiteSpace(testId) || !_intentInvokeByTestId.TryGetValue(testId, out var invoke))
            {
                return false;
            }

            invoke?.Invoke();
            return true;
        }

        private bool TryRaiseAttachmentContextIntent(string container, int slotIndex)
        {
            if (slotIndex < 0)
            {
                return false;
            }

            var normalizedContainer = NormalizeContainer(container);
            var itemId = ResolveSlotItemId(normalizedContainer, slotIndex);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            IntentRaised?.Invoke(new UiIntent(
                "tab.inventory.item.context.attachments",
                new TabInventoryAttachmentContextIntentPayload(normalizedContainer, slotIndex, itemId)));
            return true;
        }

        private void RenderDeviceSessionHistory(IReadOnlyList<string> entries)
        {
            if (_deviceSessionHistory == null)
            {
                return;
            }

            _deviceSessionHistory.Clear();
            if (entries == null || entries.Count == 0)
            {
                var empty = new Label("No saved groups yet.") { name = "inventory__device-session-empty" };
                empty.AddToClassList("inventory__device-session-row");
                _deviceSessionHistory.Add(empty);
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var row = new Label(entries[i] ?? string.Empty)
                {
                    name = $"inventory__device-session-row-{i}"
                };
                row.AddToClassList("inventory__device-session-row");
                _deviceSessionHistory.Add(row);
            }
        }

        private void HandleSlotClick(string container, int slotIndex)
        {
            if (_suppressNextClick)
            {
                _suppressNextClick = false;
                return;
            }

            TryInteractSlot(container, slotIndex);
        }

        private bool TryPointerDown(string container, int slotIndex, int pointerId)
        {
            if (slotIndex < 0)
            {
                return false;
            }

            var sourceItemId = ResolveSlotItemId(container, slotIndex);
            if (string.IsNullOrWhiteSpace(sourceItemId))
            {
                return false;
            }

            _dragSourceContainer = NormalizeContainer(container);
            _dragSourceIndex = slotIndex;
            _dragSourceItemId = sourceItemId;
            _dragTargetContainer = null;
            _dragTargetIndex = -1;
            _activePointerId = pointerId;
            ApplyDragVisuals();
            return true;
        }

        private bool TryPointerEnter(string container, int slotIndex, Vector2 pointerPanelPosition)
        {
            if (slotIndex < 0)
            {
                return false;
            }

            ShowTooltipForSlot(container, slotIndex, pointerPanelPosition);

            if (_dragSourceIndex < 0)
            {
                return true;
            }

            _dragTargetContainer = NormalizeContainer(container);
            _dragTargetIndex = slotIndex;
            ApplyDragVisuals();
            return true;
        }

        private bool TryPointerMove(string container, int slotIndex, Vector2 pointerPanelPosition)
        {
            if (slotIndex < 0)
            {
                return false;
            }

            ShowTooltipForSlot(container, slotIndex, pointerPanelPosition);
            return true;
        }

        private bool TryPointerUp(string container, int slotIndex, int pointerId = -1)
        {
            if (_dragSourceIndex < 0 || slotIndex < 0)
            {
                return false;
            }

            if (!IsMatchingActivePointer(pointerId))
            {
                return false;
            }

            _suppressNextClick = true;
            var targetContainer = NormalizeContainer(container);
            var targetItemId = ResolveSlotItemId(targetContainer, slotIndex);

            if (_dragSourceContainer == targetContainer && _dragSourceIndex == slotIndex)
            {
                ClearDragSource();
                return true;
            }

            var key = !string.IsNullOrWhiteSpace(_dragSourceItemId) && _dragSourceItemId == targetItemId
                ? "inventory.drag.merge"
                : "inventory.drag.swap";
            var payload = new TabInventoryDragController.DragIntentPayload(_dragSourceContainer, _dragSourceIndex, targetContainer, slotIndex);
            IntentRaised?.Invoke(new UiIntent(key, payload));
            ClearDragSource();
            return true;
        }

        private void RegisterDragDropOutsideCallbacks()
        {
            _panel?.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_dragSourceIndex < 0)
                {
                    return;
                }

                if (!IsMatchingActivePointer(evt.pointerId))
                {
                    return;
                }

                if (IsPointerOverAnySlot(evt.position))
                {
                    return;
                }

                _suppressNextClick = true;
                TryPointerUpOutsideSlots(evt.pointerId);
            }, TrickleDown.TrickleDown);
        }

        private bool TryPointerUpOutsideSlots(int pointerId = -1)
        {
            if (_dragSourceIndex < 0)
            {
                return false;
            }

            if (!IsMatchingActivePointer(pointerId))
            {
                return false;
            }

            var payload = new TabInventoryDragController.DragIntentPayload(_dragSourceContainer, _dragSourceIndex, _dragSourceContainer, -1);
            IntentRaised?.Invoke(new UiIntent("inventory.drag.drop", payload));
            ClearDragSource();
            return true;
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

        private int ResolveSlotQuantity(string container, int slotIndex)
        {
            var normalized = NormalizeContainer(container);
            var source = normalized == "backpack" ? _backpackSlotQuantities : _beltSlotQuantities;
            return slotIndex >= 0 && slotIndex < source.Length ? source[slotIndex] : 0;
        }

        private int ResolveSlotMaxStack(string container, int slotIndex)
        {
            var normalized = NormalizeContainer(container);
            var source = normalized == "backpack" ? _backpackSlotMaxStacks : _beltSlotMaxStacks;
            return slotIndex >= 0 && slotIndex < source.Length ? source[slotIndex] : 1;
        }

        private static string NormalizeContainer(string container)
        {
            return container == "backpack" ? "backpack" : "belt";
        }

        private bool IsPointerOverAnySlot(Vector2 panelSpacePosition)
        {
            for (var i = 0; i < _beltSlots.Length; i++)
            {
                var slot = _beltSlots[i];
                if (slot != null && slot.worldBound.Contains(panelSpacePosition))
                {
                    return true;
                }
            }

            for (var i = 0; i < _backpackSlots.Length; i++)
            {
                var slot = _backpackSlots[i];
                if (slot != null && slot.worldBound.Contains(panelSpacePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearDragSource()
        {
            ReleaseCapturedPointer();
            _dragSourceContainer = null;
            _dragSourceIndex = -1;
            _dragSourceItemId = null;
            _dragTargetContainer = null;
            _dragTargetIndex = -1;
            ApplyDragVisuals();
            HideTooltip();
        }

        private void ShowTooltipForSlot(string container, int slotIndex, Vector2 panelPosition)
        {
            var itemId = ResolveSlotItemId(container, slotIndex);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                HideTooltip();
                return;
            }

            _hoverTooltipContainer = NormalizeContainer(container);
            _hoverTooltipSlotIndex = slotIndex;
            _hoverTooltipItemId = itemId;
            _hoverTooltipPanelPosition = panelPosition;
            var quantity = ResolveSlotQuantity(container, slotIndex);
            var maxStack = ResolveSlotMaxStack(container, slotIndex);
            _hoverTooltipQuantity = quantity;
            _hoverTooltipMaxStack = maxStack;
            if (_tooltipPresenter != null)
            {
                _isHoverTooltipActive = _tooltipPresenter.TryShowItem(itemId, quantity, maxStack, panelPosition);
            }
        }

        private void RevalidateHoverTooltip(TabInventoryUiState inventoryState)
        {
            if (!_isHoverTooltipActive)
            {
                return;
            }

            if (!string.Equals(inventoryState.ActiveSection, "inventory", StringComparison.Ordinal))
            {
                HideTooltip();
                return;
            }

            var currentItemId = ResolveSlotItemId(_hoverTooltipContainer, _hoverTooltipSlotIndex);
            if (string.IsNullOrWhiteSpace(currentItemId)
                || !string.Equals(currentItemId, _hoverTooltipItemId, StringComparison.Ordinal))
            {
                HideTooltip();
                return;
            }

            var currentQuantity = ResolveSlotQuantity(_hoverTooltipContainer, _hoverTooltipSlotIndex);
            var currentMaxStack = ResolveSlotMaxStack(_hoverTooltipContainer, _hoverTooltipSlotIndex);
            if (currentQuantity == _hoverTooltipQuantity && currentMaxStack == _hoverTooltipMaxStack)
            {
                return;
            }

            if (_tooltipPresenter == null)
            {
                HideTooltip();
                return;
            }

            _isHoverTooltipActive = _tooltipPresenter.TryShowItem(
                currentItemId,
                currentQuantity,
                currentMaxStack,
                _hoverTooltipPanelPosition);
            if (!_isHoverTooltipActive)
            {
                HideTooltip();
                return;
            }

            _hoverTooltipQuantity = currentQuantity;
            _hoverTooltipMaxStack = currentMaxStack;
        }

        private void HideTooltip()
        {
            _isHoverTooltipActive = false;
            _hoverTooltipContainer = null;
            _hoverTooltipSlotIndex = -1;
            _hoverTooltipItemId = null;
            _hoverTooltipQuantity = 0;
            _hoverTooltipMaxStack = 1;
            _hoverTooltipPanelPosition = Vector2.zero;
            if (_tooltipPresenter != null)
            {
                _tooltipPresenter.Hide();
            }
            if (_tooltip != null)
            {
                _tooltip.style.display = DisplayStyle.None;
            }
        }

        private void ReleaseCapturedPointer()
        {
            _activePointerId = -1;
        }

        private bool IsMatchingActivePointer(int pointerId)
        {
            return pointerId < 0 || _activePointerId < 0 || pointerId == _activePointerId;
        }

        private void ApplyDragVisuals()
        {
            _panel?.EnableInClassList("is-dragging", _dragSourceIndex >= 0);
            ApplySlotClass("is-drag-source", _dragSourceContainer, _dragSourceIndex);
            ApplySlotClass("is-drag-target", _dragTargetContainer, _dragTargetIndex);
        }

        private void ApplySlotClass(string className, string activeContainer, int activeIndex)
        {
            for (var i = 0; i < _beltSlots.Length; i++)
            {
                var slot = _beltSlots[i];
                if (slot == null)
                {
                    continue;
                }

                slot.EnableInClassList(className, activeContainer == "belt" && activeIndex == i);
            }

            for (var i = 0; i < _backpackSlots.Length; i++)
            {
                var slot = _backpackSlots[i];
                if (slot == null)
                {
                    continue;
                }

                slot.EnableInClassList(className, activeContainer == "backpack" && activeIndex == i);
            }
        }
    }
}
