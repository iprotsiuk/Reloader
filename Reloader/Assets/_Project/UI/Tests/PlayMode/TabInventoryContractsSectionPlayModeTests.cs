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
                statusText: "Available contract",
                trackingText: string.Empty);

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
            var contractsStatusRow = root.Q<VisualElement>("inventory__contracts-status-row");
            var contractsStatus = root.Q<Label>("inventory__contracts-status");
            var contractsTracking = root.Q<Label>("inventory__contracts-tracking");
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
            Assert.That(contractsStatusRow, Is.Not.Null);
            Assert.That(contractsStatus.parent, Is.SameAs(contractsStatusRow));
            Assert.That(contractsTracking, Is.Not.Null);
            Assert.That(contractsTracking.parent, Is.SameAs(contractsStatusRow));
            Assert.That(contractsStatus.text, Is.EqualTo("Available contract"));
            Assert.That(contractsTracking.style.display.value, Is.EqualTo(DisplayStyle.None));
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
                statusText: "Available contract",
                trackingText: string.Empty);

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
            var activeStatus = root.Q<Label>("inventory__contracts-active-status");
            var activeTracking = root.Q<Label>("inventory__contracts-active-tracking");
            var activePayout = root.Q<Label>("inventory__contracts-active-payout");
            var activeTargetBlock = root.Q<VisualElement>("inventory__contracts-active-target-block");
            var contractsTarget = root.Q<Label>("inventory__contracts-target");
            var contractsSummary = root.Q<Label>("inventory__contracts-target-summary");
            var briefingCard = root.Q<VisualElement>("inventory__contracts-briefing-card");
            var contractsBriefing = root.Q<Label>("inventory__contracts-briefing");
            var intelCard = root.Q<VisualElement>("inventory__contracts-intel-card");
            var activeHeader = root.Q<VisualElement>("inventory__contracts-active-header");
            var activeFooter = root.Q<VisualElement>("inventory__contracts-active-footer");
            var acceptButton = root.Q<Button>("inventory__contracts-primary-action");
            var activeActionButton = root.Q<Button>("inventory__contracts-active-primary-action");

            Assert.That(contractController.AcceptCalls, Is.EqualTo(1));
            Assert.That(postedFeed.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeWorkspace.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(contractsStatus.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeHeader, Is.Not.Null);
            Assert.That(activeFooter, Is.Not.Null);
            Assert.That(activeStatus, Is.Not.Null);
            Assert.That(activeTracking, Is.Not.Null);
            Assert.That(activeTracking.parent, Is.SameAs(activeHeader));
            Assert.That(activeStatus.text, Is.EqualTo("Mission Status: Active contract"));
            Assert.That(activeTracking.text, Is.Empty);
            Assert.That(activeTracking.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activePayout, Is.Not.Null);
            Assert.That(activePayout.text, Is.EqualTo("Payout: $2,400"));
            Assert.That(activeTargetBlock, Is.Not.Null);
            Assert.That(contractsTarget.text, Is.EqualTo("Yuri Antonov"));
            Assert.That(contractsSummary, Is.Not.Null);
            Assert.That(contractsSummary.text, Is.EqualTo("Black cap, waits near the river checkpoint."));
            Assert.That(briefingCard, Is.Not.Null);
            Assert.That(contractsBriefing.text, Does.Contain("Target becomes exposed"));
            Assert.That(intelCard, Is.Not.Null);
            Assert.That(acceptButton.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeActionButton.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(activeActionButton.text, Is.EqualTo("Cancel Contract"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_ContractsTab_WhenContractFailed_KeepsFailureWorkspaceVisible()
        {
            var go = new GameObject("TabInventoryControllerContractsFailed");
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
                hasActiveContract: false,
                contractTitle: "Bridge Glass",
                targetDisplayName: "Yuri Antonov",
                targetDescription: "Black cap, waits near the river checkpoint.",
                briefingText: "Target becomes exposed for roughly thirty seconds each loop.",
                distanceBandMeters: 610f,
                payout: 2400,
                canAccept: false,
                canCancel: false,
                canClaimReward: false,
                canClearFailed: true,
                statusText: "Failed: wrong target • Escape search: 28s",
                restrictionsText: "Wrong target fails contract",
                failureConditionsText: "Wrong target kill • Manual cancel",
                trackingText: string.Empty,
                hasFailedContract: true);

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetContractController(contractController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent("tab.menu.select", "contracts"));

            var postedFeed = root.Q<VisualElement>("inventory__contracts-feed");
            var activeWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            var contractsStatus = root.Q<Label>("inventory__contracts-status");
            var activeStatus = root.Q<Label>("inventory__contracts-active-status");
            var activeTracking = root.Q<Label>("inventory__contracts-active-tracking");
            var activeTargetBlock = root.Q<VisualElement>("inventory__contracts-active-target-block");
            var contractsTarget = root.Q<Label>("inventory__contracts-target");
            var acceptButton = root.Q<Button>("inventory__contracts-primary-action");
            var activeActionButton = root.Q<Button>("inventory__contracts-active-primary-action");

            Assert.That(postedFeed.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeWorkspace.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(contractsStatus.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeStatus.text, Is.EqualTo("Mission Status: Failed: wrong target • Escape search: 28s"));
            Assert.That(activeTracking.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeTargetBlock, Is.Not.Null);
            Assert.That(contractsTarget.text, Is.EqualTo("Yuri Antonov"));
            Assert.That(acceptButton.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeActionButton.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(activeActionButton.text, Is.EqualTo("Clear Contract"));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_HeaderMeta_RendersWorldTimeAndBalance()
        {
            var worldControllerType = Type.GetType("Reloader.Core.Runtime.CoreWorldController, Reloader.Core");
            var economyControllerType = Type.GetType("Reloader.Economy.EconomyController, Reloader.Economy");
            Assert.That(worldControllerType, Is.Not.Null);
            Assert.That(economyControllerType, Is.Not.Null);

            var go = new GameObject("TabInventoryControllerHeaderMeta");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var contractController = go.AddComponent<TestContractController>();
            var worldController = go.AddComponent(worldControllerType);
            var economyController = go.AddComponent(economyControllerType);

            var setWorldStateMethod = worldControllerType.GetMethod("SetWorldState", BindingFlags.Instance | BindingFlags.Public);
            var tryAwardMoneyMethod = economyControllerType.GetMethod("TryAwardMoney", BindingFlags.Instance | BindingFlags.Public);
            var setCoreWorldControllerMethod = typeof(TabInventoryController).GetMethod("SetCoreWorldController", BindingFlags.Instance | BindingFlags.Public);
            var setEconomyControllerMethod = typeof(TabInventoryController).GetMethod("SetEconomyController", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setWorldStateMethod, Is.Not.Null);
            Assert.That(tryAwardMoneyMethod, Is.Not.Null);
            Assert.That(setCoreWorldControllerMethod, Is.Not.Null);
            Assert.That(setEconomyControllerMethod, Is.Not.Null);

            setWorldStateMethod!.Invoke(worldController, new object[] { 4, 18.6667f });
            var awarded = (bool)tryAwardMoneyMethod!.Invoke(economyController, new object[] { 1950 });
            Assert.That(awarded, Is.True);

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetContractController(contractController);
            setCoreWorldControllerMethod!.Invoke(controller, new[] { worldController });
            setEconomyControllerMethod!.Invoke(controller, new[] { economyController });
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();

            var headerMeta = root.Q<Label>("inventory__header-meta");
            Assert.That(headerMeta, Is.Not.Null);
            Assert.That(headerMeta.text, Is.EqualTo("Friday • 18:40 • $2,450"));

            setWorldStateMethod.Invoke(worldController, new object[] { 6, 5.5f });
            awarded = (bool)tryAwardMoneyMethod.Invoke(economyController, new object[] { 50 });
            Assert.That(awarded, Is.True);
            Assert.That(headerMeta.text, Is.EqualTo("Sunday • 05:30 • $2,500"));

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
        public void Controller_ContractsTab_RendersTermsPaneForPostedOffer()
        {
            var go = new GameObject("TabInventoryControllerContractsTerms");
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
                contractTitle: "Cafe Exit",
                targetDisplayName: "Maksim Volkov",
                targetDescription: "Gray coat, smoker, exits the cafe at dusk.",
                briefingText: "Observe from the ridge and confirm the target before taking the shot.",
                distanceBandMeters: 420f,
                payout: 1500,
                canAccept: true,
                canCancel: false,
                canClaimReward: false,
                canClearFailed: false,
                statusText: "Available contract",
                restrictionsText: "Wrong target fails contract",
                failureConditionsText: "Wrong target kill • Manual cancel");

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetContractController(contractController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent("tab.menu.select", "contracts"));

            var genericDetailPane = root.Q<VisualElement>("inventory__detail-pane-generic");
            var contractsDetailPane = root.Q<VisualElement>("inventory__detail-pane-contracts");
            var basePayout = root.Q<Label>("inventory__detail-pane-base-payout");
            var bonusConditions = root.Q<Label>("inventory__detail-pane-bonus-conditions");
            var restrictions = root.Q<Label>("inventory__detail-pane-restrictions");
            var failureConditions = root.Q<Label>("inventory__detail-pane-failure-conditions");
            var rewardState = root.Q<Label>("inventory__detail-pane-reward-state");

            Assert.That(genericDetailPane.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(contractsDetailPane.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(basePayout.text, Is.EqualTo("Payout: $1,500"));
            Assert.That(bonusConditions.text, Is.EqualTo("None"));
            Assert.That(restrictions.text, Is.EqualTo("Wrong target fails contract"));
            Assert.That(failureConditions.text, Is.EqualTo("Wrong target kill • Manual cancel"));
            Assert.That(rewardState.text, Is.EqualTo("Reward pending contract acceptance."));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_ContractsClearIntent_ClearsFailedWorkspaceWithoutRepostingOffer()
        {
            var go = new GameObject("TabInventoryControllerContractsClear");
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
                hasActiveContract: false,
                contractTitle: "Bridge Glass",
                targetDisplayName: "Yuri Antonov",
                targetDescription: "Black cap, waits near the river checkpoint.",
                briefingText: "Target becomes exposed for roughly thirty seconds each loop.",
                distanceBandMeters: 610f,
                payout: 2400,
                canAccept: false,
                canCancel: false,
                canClaimReward: false,
                canClearFailed: true,
                statusText: "Failed: wrong target • Escape search: 28s",
                restrictionsText: "Wrong target fails contract",
                failureConditionsText: "Wrong target kill • Manual cancel",
                trackingText: string.Empty,
                hasFailedContract: true);

            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetContractController(contractController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent("tab.menu.select", "contracts"));
            controller.HandleIntent(new UiIntent("tab.inventory.contracts.clear"));

            var postedFeed = root.Q<VisualElement>("inventory__contracts-feed");
            var activeWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            var contractsStatus = root.Q<Label>("inventory__contracts-status");

            Assert.That(contractController.ClearFailedCalls, Is.EqualTo(1));
            Assert.That(postedFeed.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(activeWorkspace.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(contractsStatus.text, Is.EqualTo("No contracts currently posted"));

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

            var activeWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            var activeStatus = root.Q<Label>("inventory__contracts-active-status");
            var contractsBriefing = root.Q<Label>("inventory__contracts-briefing");
            var activeActionButton = root.Q<Button>("inventory__contracts-active-primary-action");
            Assert.That(activeWorkspace.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(activeStatus, Is.Not.Null);
            Assert.That(activeStatus.text, Is.EqualTo("Mission Status: Ready to claim"));
            Assert.That(contractsBriefing, Is.Not.Null);
            Assert.That(contractsBriefing.text, Is.EqualTo("Objective complete. Reward is ready to collect."));
            Assert.That(activeActionButton.text, Is.EqualTo("Claim Reward"));

            controller.HandleIntent(new UiIntent("tab.inventory.contracts.claim"));

            var postedFeed = root.Q<VisualElement>("inventory__contracts-feed");
            var claimedWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            var contractsStatus = root.Q<Label>("inventory__contracts-status");

            Assert.That(contractController.ClaimCalls, Is.EqualTo(1));
            Assert.That(postedFeed.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(claimedWorkspace.style.display.value, Is.EqualTo(DisplayStyle.None));
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

        [Test]
        public void Initialize_WhenContractsSectionRequiresFallback_CreatesTrackingLabelBesideStatus()
        {
            var root = BuildRootWithFallbackContractsSection();
            var binder = new TabInventoryViewBinder();

            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var contractsSection = root.Q<VisualElement>("inventory__section-quests");
            var contractsStatusRow = contractsSection?.Q<VisualElement>("inventory__contracts-status-row");
            var contractsStatus = contractsSection?.Q<Label>("inventory__contracts-status");
            var contractsTracking = contractsSection?.Q<Label>("inventory__contracts-tracking");

            Assert.That(contractsSection, Is.Not.Null);
            Assert.That(contractsStatusRow, Is.Not.Null);
            Assert.That(contractsStatus, Is.Not.Null);
            Assert.That(contractsTracking, Is.Not.Null);
            Assert.That(contractsStatus.parent, Is.SameAs(contractsStatusRow));
            Assert.That(contractsTracking.parent, Is.SameAs(contractsStatusRow));
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            root.Add(panel);

            panel.Add(new Label { name = "inventory__header-meta", text = "Monday • 08:00 • $500" });
            panel.Add(new VisualElement { name = "inventory__tabbar" });
            panel.Add(new Button { name = "inventory__tab-inventory", text = "Inventory" });
            panel.Add(new Button { name = "inventory__tab-quests", text = "Quests" });
            panel.Add(new Button { name = "inventory__tab-journal", text = "Journal" });
            panel.Add(new Button { name = "inventory__tab-calendar", text = "Calendar" });
            panel.Add(new Button { name = "inventory__tab-device", text = "Device" });

            panel.Add(new VisualElement { name = "inventory__section-inventory" });
            var questsSection = new VisualElement { name = "inventory__section-quests" };
            var contractsStatusRow = new VisualElement { name = "inventory__contracts-status-row" };
            contractsStatusRow.Add(new Label { name = "inventory__contracts-status" });
            contractsStatusRow.Add(new Label { name = "inventory__contracts-tracking" });
            questsSection.Add(contractsStatusRow);
            var contractsFeed = new VisualElement { name = "inventory__contracts-feed" };
            var contractsRow = new VisualElement { name = "inventory__contracts-row" };
            var rowPreview = new VisualElement();
            rowPreview.Add(new VisualElement());
            var rowCopy = new VisualElement();
            rowCopy.Add(new Label { name = "inventory__contracts-title" });
            rowCopy.Add(new Label { name = "inventory__contracts-summary" });
            contractsRow.Add(rowPreview);
            contractsRow.Add(rowCopy);
            contractsRow.Add(new Label { name = "inventory__contracts-payout" });
            contractsRow.Add(new Button { name = "inventory__contracts-primary-action" });
            contractsFeed.Add(contractsRow);
            questsSection.Add(contractsFeed);

            var contractsActive = new VisualElement { name = "inventory__contracts-active" };
            var contractsActiveHeader = new VisualElement { name = "inventory__contracts-active-header" };
            contractsActiveHeader.Add(new Label { name = "inventory__contracts-active-status" });
            contractsActiveHeader.Add(new Label { name = "inventory__contracts-active-tracking" });
            contractsActiveHeader.Add(new Label { name = "inventory__contracts-active-payout" });
            contractsActive.Add(contractsActiveHeader);
            var contractsActiveTargetBlock = new VisualElement { name = "inventory__contracts-active-target-block" };
            contractsActiveTargetBlock.Add(new Label { name = "inventory__contracts-target" });
            contractsActiveTargetBlock.Add(new Label { name = "inventory__contracts-target-summary" });
            contractsActive.Add(contractsActiveTargetBlock);
            var contractsBriefingCard = new VisualElement { name = "inventory__contracts-briefing-card" };
            contractsBriefingCard.Add(new Label { name = "inventory__contracts-briefing" });
            contractsActive.Add(contractsBriefingCard);
            var contractsIntelCard = new VisualElement { name = "inventory__contracts-intel-card" };
            contractsIntelCard.Add(new Label { name = "inventory__contracts-intel" });
            contractsActive.Add(contractsIntelCard);
            var contractsActiveFooter = new VisualElement { name = "inventory__contracts-active-footer" };
            contractsActiveFooter.Add(new Button { name = "inventory__contracts-active-primary-action" });
            contractsActive.Add(contractsActiveFooter);
            questsSection.Add(contractsActive);
            panel.Add(questsSection);
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
            var detailPane = new VisualElement { name = "inventory__detail-pane" };
            detailPane.Add(new VisualElement { name = "inventory__detail-pane-generic" });
            detailPane.Add(new VisualElement { name = "inventory__detail-pane-contracts" });
            detailPane.Add(new Label { name = "inventory__detail-pane-base-payout", text = "Payout: --" });
            detailPane.Add(new Label { name = "inventory__detail-pane-bonus-conditions", text = "None" });
            detailPane.Add(new Label { name = "inventory__detail-pane-restrictions", text = "None" });
            detailPane.Add(new Label { name = "inventory__detail-pane-failure-conditions", text = "None" });
            detailPane.Add(new Label { name = "inventory__detail-pane-reward-state", text = "No contract selected" });
            panel.Add(detailPane);

            var tooltip = new VisualElement { name = "inventory__tooltip" };
            tooltip.Add(new Label { name = "inventory__tooltip-title" });
            panel.Add(tooltip);

            return root;
        }

        private static VisualElement BuildRootWithFallbackContractsSection()
        {
            var root = BuildRoot();
            var questsSection = root.Q<VisualElement>("inventory__section-quests");
            Assert.That(questsSection, Is.Not.Null);
            questsSection!.Clear();
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
            public int ClearFailedCalls { get; private set; }
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
                    canClearFailed: false,
                    statusText: "Active contract",
                    restrictionsText: Status.RestrictionsText,
                    failureConditionsText: Status.FailureConditionsText);
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
                    canClearFailed: false,
                    statusText: "Available contract",
                    restrictionsText: Status.RestrictionsText,
                    failureConditionsText: Status.FailureConditionsText);
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

            public bool ClearFailedContract()
            {
                ClearFailedCalls++;
                if (!Status.CanClearFailed)
                {
                    return false;
                }

                Status = TabInventoryContractStatus.CreateDefault();
                return true;
            }
        }
    }
}
