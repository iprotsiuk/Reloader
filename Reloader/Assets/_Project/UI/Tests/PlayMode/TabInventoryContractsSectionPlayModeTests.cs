using System;
using System.Reflection;
using NUnit.Framework;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.TabInventory;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Reloader.UI.Tests.PlayMode
{
    public class TabInventoryContractsSectionPlayModeTests
    {
        [Test]
        public void Controller_ContractsTab_RendersPostedContractRowWhenOfferIsAvailable()
        {
            var go = new GameObject("TabInventoryControllerContractsSection");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var contractController = go.AddComponent<TestContractController>();
            contractController.Status = new TabInventoryContractStatus(
                hasAvailableContract: true,
                hasActiveContract: false,
                contractTitle: "First Bloodline Job",
                targetDisplayName: "Maksim Volkov",
                targetDescription: "Gray coat, smoker, exits the cafe at dusk.",
                briefingText: "Observe from the ridge and confirm the target before taking the shot.",
                distanceBandMeters: 420f,
                payout: 1500,
                canAccept: true,
                canCancel: false,
                canClaimReward: false,
                statusText: "Available contract");

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetContractController(contractController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent("tab.menu.select", "contracts"));

            var contractsTab = root.Q<Button>("inventory__tab-quests");
            var contractsSection = root.Q<VisualElement>("inventory__section-quests");
            var postedFeed = root.Q<VisualElement>("inventory__contracts-feed");
            var postedRow = root.Q<VisualElement>("inventory__contracts-row");
            var activeWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            var contractsStatus = root.Q<Label>("inventory__contracts-status");
            var contractsTitle = root.Q<Label>("inventory__contracts-title");
            var contractsSummary = root.Q<Label>("inventory__contracts-summary");
            var contractsPayout = root.Q<Label>("inventory__contracts-payout");
            var acceptButton = root.Q<Button>("inventory__contracts-primary-action");

            Assert.That(contractsTab, Is.Not.Null);
            Assert.That(contractsTab.text, Is.EqualTo("Contracts"));
            Assert.That(contractsSection.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(postedFeed.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(postedRow.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(activeWorkspace.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(contractsStatus.text, Is.EqualTo("Available contract"));
            Assert.That(contractsTitle.text, Is.EqualTo("First Bloodline Job"));
            Assert.That(contractsSummary.text, Is.EqualTo("Gray coat, smoker, exits the cafe at dusk."));
            Assert.That(contractsPayout.text, Is.EqualTo("Payout: $1,500"));
            Assert.That(acceptButton.enabledSelf, Is.True);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_ContractsAcceptIntent_SwitchesContractsTabIntoActiveWorkspace()
        {
            var go = new GameObject("TabInventoryControllerContractsAccept");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var contractController = go.AddComponent<TestContractController>();
            contractController.Status = new TabInventoryContractStatus(
                hasAvailableContract: true,
                hasActiveContract: false,
                contractTitle: "Bridge Glass",
                targetDisplayName: "Yuri Antonov",
                targetDescription: "Black cap, waits near the river checkpoint.",
                briefingText: "Target becomes exposed for roughly thirty seconds each loop.",
                distanceBandMeters: 610f,
                payout: 2400,
                canAccept: true,
                canCancel: false,
                canClaimReward: false,
                statusText: "Available contract");

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetContractController(contractController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent("tab.menu.select", "contracts"));
            controller.HandleIntent(new UiIntent("tab.inventory.contracts.accept"));

            var postedFeed = root.Q<VisualElement>("inventory__contracts-feed");
            var activeWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            var contractsStatus = root.Q<Label>("inventory__contracts-status");
            var contractsTarget = root.Q<Label>("inventory__contracts-target");
            var contractsBriefing = root.Q<Label>("inventory__contracts-briefing");
            var acceptButton = root.Q<Button>("inventory__contracts-primary-action");
            var activeActionButton = root.Q<Button>("inventory__contracts-active-primary-action");

            Assert.That(contractController.AcceptCalls, Is.EqualTo(1));
            Assert.That(postedFeed.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeWorkspace.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(contractsStatus.text, Is.EqualTo("Active contract"));
            Assert.That(contractsTarget.text, Is.EqualTo("Yuri Antonov"));
            Assert.That(contractsBriefing.text, Does.Contain("Target becomes exposed"));
            Assert.That(acceptButton.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeActionButton.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(activeActionButton.text, Is.EqualTo("Cancel Contract"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_ContractsCancelIntent_ReturnsContractsTabToPostedOffer()
        {
            var go = new GameObject("TabInventoryControllerContractsCancel");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var contractController = go.AddComponent<TestContractController>();
            contractController.Status = new TabInventoryContractStatus(
                hasAvailableContract: false,
                hasActiveContract: true,
                contractTitle: "Bridge Glass",
                targetDisplayName: "Yuri Antonov",
                targetDescription: "Black cap, waits near the river checkpoint.",
                briefingText: "Target becomes exposed for roughly thirty seconds each loop.",
                distanceBandMeters: 610f,
                payout: 2400,
                canAccept: false,
                canCancel: true,
                canClaimReward: false,
                statusText: "Active contract");

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetContractController(contractController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent("tab.menu.select", "contracts"));
            controller.HandleIntent(new UiIntent("tab.inventory.contracts.cancel"));

            var postedFeed = root.Q<VisualElement>("inventory__contracts-feed");
            var activeWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            var contractsStatus = root.Q<Label>("inventory__contracts-status");

            Assert.That(contractController.CancelCalls, Is.EqualTo(1));
            Assert.That(postedFeed.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(activeWorkspace.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(contractsStatus.text, Is.EqualTo("Available contract"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_ContractsClaimIntent_CompletesClaimableContractAndReturnsToEmptyState()
        {
            var go = new GameObject("TabInventoryControllerContractsClaim");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var contractController = go.AddComponent<TestContractController>();
            contractController.Status = new TabInventoryContractStatus(
                hasAvailableContract: false,
                hasActiveContract: true,
                contractTitle: "Bridge Glass",
                targetDisplayName: "Yuri Antonov",
                targetDescription: "Black cap, waits near the river checkpoint.",
                briefingText: "Objective complete. Reward is ready to collect.",
                distanceBandMeters: 610f,
                payout: 2400,
                canAccept: false,
                canCancel: false,
                canClaimReward: true,
                statusText: "Ready to claim");

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetContractController(contractController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent("tab.menu.select", "contracts"));

            var activeActionButton = root.Q<Button>("inventory__contracts-active-primary-action");
            Assert.That(activeActionButton.text, Is.EqualTo("Claim Reward"));

            controller.HandleIntent(new UiIntent("tab.inventory.contracts.claim"));

            var postedFeed = root.Q<VisualElement>("inventory__contracts-feed");
            var activeWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            var contractsStatus = root.Q<Label>("inventory__contracts-status");

            Assert.That(contractController.ClaimCalls, Is.EqualTo(1));
            Assert.That(postedFeed.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeWorkspace.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(contractsStatus.text, Is.EqualTo("No contracts currently posted"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Initialize_WhenContractsControlsAlreadyExist_RebindsAcceptButtonToCurrentBinder()
        {
            var root = BuildRoot();
            var firstBinder = new TabInventoryViewBinder();
            var firstAcceptCount = 0;
            Action<UiIntent> firstHandler = intent =>
            {
                if (string.Equals(intent.Key, "tab.inventory.contracts.accept", StringComparison.Ordinal))
                {
                    firstAcceptCount++;
                }
            };

            firstBinder.IntentRaised += firstHandler;
            firstBinder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var acceptButton = root.Q<Button>("inventory__contracts-primary-action");
            Assert.That(acceptButton, Is.Not.Null);

            InvokeClick(acceptButton);
            Assert.That(firstAcceptCount, Is.EqualTo(1));

            firstBinder.IntentRaised -= firstHandler;

            var secondBinder = new TabInventoryViewBinder();
            var secondAcceptCount = 0;
            secondBinder.IntentRaised += intent =>
            {
                if (string.Equals(intent.Key, "tab.inventory.contracts.accept", StringComparison.Ordinal))
                {
                    secondAcceptCount++;
                }
            };
            secondBinder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            InvokeClick(acceptButton);

            Assert.That(firstAcceptCount, Is.EqualTo(1));
            Assert.That(secondAcceptCount, Is.EqualTo(1));
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            root.Add(panel);

            panel.Add(new VisualElement { name = "inventory__tabbar" });
            panel.Add(new Button { name = "inventory__tab-inventory", text = "Inventory" });
            panel.Add(new Button { name = "inventory__tab-quests", text = "Quests" });
            panel.Add(new Button { name = "inventory__tab-journal", text = "Journal" });
            panel.Add(new Button { name = "inventory__tab-calendar", text = "Calendar" });
            panel.Add(new Button { name = "inventory__tab-device", text = "Device" });

            panel.Add(new VisualElement { name = "inventory__section-inventory" });
            panel.Add(new VisualElement { name = "inventory__section-quests" });
            panel.Add(new VisualElement { name = "inventory__section-journal" });
            panel.Add(new VisualElement { name = "inventory__section-calendar" });
            panel.Add(new VisualElement { name = "inventory__section-device" });
            panel.Add(new VisualElement { name = "inventory__section-attachments" });
            panel.Add(new VisualElement { name = "inventory__device-notes" });
            panel.Add(new Label { name = "inventory__device-selected-target-value" });
            panel.Add(new Label { name = "inventory__device-shot-count-value" });
            panel.Add(new Label { name = "inventory__device-spread-value" });
            panel.Add(new Label { name = "inventory__device-moa-value" });
            panel.Add(new Label { name = "inventory__device-saved-groups-value" });
            panel.Add(new Label { name = "inventory__device-install-feedback-text" });
            panel.Add(new VisualElement { name = "inventory__device-session-history" });

            panel.Add(new Button { name = "inventory__device-choose-target" });
            panel.Add(new Button { name = "inventory__device-save-group" });
            panel.Add(new Button { name = "inventory__device-clear-group" });
            panel.Add(new Button { name = "inventory__device-install-hooks" });
            panel.Add(new Button { name = "inventory__device-uninstall-hooks" });

            panel.Add(new VisualElement { name = "inventory__backpack-grid" });
            panel.Add(new VisualElement { name = "inventory__grid-row--belt" });

            var tooltip = new VisualElement { name = "inventory__tooltip" };
            tooltip.Add(new Label { name = "inventory__tooltip-title" });
            panel.Add(tooltip);

            return root;
        }

        private static void InvokeClick(Button button)
        {
            Assert.That(button, Is.Not.Null);

            var clickableField = typeof(Button).GetField("m_Clickable", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(clickableField, Is.Not.Null);

            var clickable = clickableField.GetValue(button);
            Assert.That(clickable, Is.Not.Null);

            var clickedField = clickable.GetType().GetField("clicked", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(clickedField, Is.Not.Null);

            var clicked = clickedField.GetValue(clickable) as Action;
            Assert.That(clicked, Is.Not.Null);
            clicked.Invoke();
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool MenuTogglePressedThisFrame { get; set; }

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;

            public bool ConsumeMenuTogglePressed()
            {
                var result = MenuTogglePressedThisFrame;
                MenuTogglePressedThisFrame = false;
                return result;
            }
        }

        private sealed class TestContractController : MonoBehaviour, ITabInventoryContractController
        {
            public int AcceptCalls { get; private set; }
            public int CancelCalls { get; private set; }
            public int ClaimCalls { get; private set; }
            public TabInventoryContractStatus Status { get; set; }

            public bool TryGetStatus(out TabInventoryContractStatus status)
            {
                status = Status;
                return true;
            }

            public bool AcceptAvailableContract()
            {
                AcceptCalls++;
                if (!Status.CanAccept)
                {
                    return false;
                }

                Status = new TabInventoryContractStatus(
                    hasAvailableContract: Status.HasAvailableContract,
                    hasActiveContract: true,
                    contractTitle: Status.ContractTitle,
                    targetDisplayName: Status.TargetDisplayName,
                    targetDescription: Status.TargetDescription,
                    briefingText: Status.BriefingText,
                    distanceBandMeters: Status.DistanceBandMeters,
                    payout: Status.Payout,
                    canAccept: false,
                    canCancel: true,
                    canClaimReward: false,
                    statusText: "Active contract");
                return true;
            }

            public bool CancelActiveContract()
            {
                CancelCalls++;
                if (!Status.CanCancel)
                {
                    return false;
                }

                Status = new TabInventoryContractStatus(
                    hasAvailableContract: true,
                    hasActiveContract: false,
                    contractTitle: Status.ContractTitle,
                    targetDisplayName: Status.TargetDisplayName,
                    targetDescription: Status.TargetDescription,
                    briefingText: Status.BriefingText,
                    distanceBandMeters: Status.DistanceBandMeters,
                    payout: Status.Payout,
                    canAccept: true,
                    canCancel: false,
                    canClaimReward: false,
                    statusText: "Available contract");
                return true;
            }

            public bool ClaimCompletedContractReward()
            {
                ClaimCalls++;
                if (!Status.CanClaimReward)
                {
                    return false;
                }

                Status = TabInventoryContractStatus.CreateDefault();
                return true;
            }
        }
    }
}
