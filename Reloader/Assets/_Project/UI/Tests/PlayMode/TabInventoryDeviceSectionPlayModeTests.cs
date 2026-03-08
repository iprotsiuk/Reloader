using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Items;
using Reloader.Inventory;
using Reloader.Player;using Reloader.Weapons.Controllers;
using Reloader.Weapons.Runtime;

using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Runtime;
using Reloader.UI.Toolkit.TabInventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class TabInventoryDeviceSectionPlayModeTests
    {

[SetUp]
        public void SetUp()
        {
            CleanupScene();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupScene();
        }

        [Test]
        public void Controller_MenuToggleOpensDeviceSectionFromT0()
        {
            var go = new GameObject("TabInventoryControllerDeviceDefault");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var controller = go.AddComponent<TabInventoryController>();
            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();

            var deviceSection = root.Q<VisualElement>("inventory__section-device");
            var inventorySection = root.Q<VisualElement>("inventory__section-inventory");
            var deviceNotes = root.Q<VisualElement>("inventory__device-notes");

            Assert.That(deviceSection.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(inventorySection.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(deviceNotes.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_DeviceActionIntents_DelegateToDeviceController()
        {
            var go = new GameObject("TabInventoryControllerDeviceActions");
            var controller = go.AddComponent<TabInventoryController>();
            var deviceController = go.AddComponent<TestDeviceController>();
            deviceController.Status = new TabInventoryController.DeviceStatus(
                hasTarget: true,
                targetDisplayName: "TargetLane_1",
                targetDistanceMeters: 50f,
                shotCount: 2,
                isMoaAvailable: true,
                moa: 1.5d,
                spreadMeters: 0.02d,
                savedGroupCount: 0,
                canSaveGroup: true,
                canClearGroup: true,
                canInstallHooks: true,
                canUninstallHooks: true,
                attachmentFeedbackText: "Ready.",
                savedGroups: Array.Empty<TabInventoryController.DeviceSavedGroupEntry>());
            controller.SetDeviceController(deviceController);

            controller.HandleIntent(new UiIntent("tab.inventory.device.choose-target"));
            controller.HandleIntent(new UiIntent("tab.inventory.device.save-group"));
            controller.HandleIntent(new UiIntent("tab.inventory.device.clear-group"));
            controller.HandleIntent(new UiIntent("tab.inventory.device.install-hooks"));
            controller.HandleIntent(new UiIntent("tab.inventory.device.uninstall-hooks"));

            Assert.That(deviceController.ChooseTargetCalls, Is.EqualTo(1));
            Assert.That(deviceController.SaveGroupCalls, Is.EqualTo(1));
            Assert.That(deviceController.ClearGroupCalls, Is.EqualTo(1));
            Assert.That(deviceController.InstallHooksCalls, Is.EqualTo(1));
            Assert.That(deviceController.UninstallHooksCalls, Is.EqualTo(1));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_ChooseTargetIntent_ClosesTabInventoryPanel()
        {
            var go = new GameObject("TabInventoryControllerChooseTargetClosesPanel");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var controller = go.AddComponent<TabInventoryController>();
            var deviceController = go.AddComponent<TestDeviceController>();
            deviceController.Status = new TabInventoryController.DeviceStatus(
                hasTarget: false,
                targetDisplayName: string.Empty,
                targetDistanceMeters: 0f,
                shotCount: 0,
                isMoaAvailable: false,
                moa: 0d,
                spreadMeters: 0d,
                savedGroupCount: 0,
                canSaveGroup: false,
                canClearGroup: false,
                canInstallHooks: false,
                canUninstallHooks: false,
                attachmentFeedbackText: "Ready.",
                savedGroups: Array.Empty<TabInventoryController.DeviceSavedGroupEntry>());

            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetDeviceController(deviceController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();

            var panel = root.Q<VisualElement>("inventory__panel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            controller.HandleIntent(new UiIntent("tab.inventory.device.choose-target"));

            Assert.That(deviceController.ChooseTargetCalls, Is.EqualTo(1));
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_Refresh_PopulatesDeviceNotesFromDeviceStatus()
        {
            var go = new GameObject("TabInventoryControllerDeviceNotes");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var controller = go.AddComponent<TabInventoryController>();
            var deviceController = go.AddComponent<TestDeviceController>();
            deviceController.Status = new TabInventoryController.DeviceStatus(
                hasTarget: true,
                targetDisplayName: "TargetLane_2",
                targetDistanceMeters: 100f,
                shotCount: 3,
                isMoaAvailable: true,
                moa: 1.24d,
                spreadMeters: 0.029d,
                savedGroupCount: 1,
                canSaveGroup: true,
                canClearGroup: true,
                canInstallHooks: true,
                canUninstallHooks: false,
                attachmentFeedbackText: "Install hooks to enable marker tracking.",
                savedGroups: new[]
                {
                    new TabInventoryController.DeviceSavedGroupEntry(shotCount: 3, isMoaAvailable: true, moa: 1.24d, spreadMeters: 0.029d)
                });

            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetDeviceController(deviceController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();

            var targetText = root.Q<Label>("inventory__device-selected-target-value");
            var shotCountText = root.Q<Label>("inventory__device-shot-count-value");
            var spreadText = root.Q<Label>("inventory__device-spread-value");
            var moaText = root.Q<Label>("inventory__device-moa-value");
            var savedGroupsText = root.Q<Label>("inventory__device-saved-groups-value");
            var feedbackText = root.Q<Label>("inventory__device-install-feedback-text");
            var saveGroupButton = root.Q<Button>("inventory__device-save-group");
            var uninstallButton = root.Q<Button>("inventory__device-uninstall-hooks");
            var history = root.Q<VisualElement>("inventory__device-session-history");
            var historyRow = root.Q<Label>("inventory__device-session-row-0");

            Assert.That(targetText, Is.Not.Null);
            Assert.That(shotCountText, Is.Not.Null);
            Assert.That(spreadText, Is.Not.Null);
            Assert.That(moaText, Is.Not.Null);
            Assert.That(savedGroupsText, Is.Not.Null);
            Assert.That(feedbackText, Is.Not.Null);
            Assert.That(saveGroupButton, Is.Not.Null);
            Assert.That(uninstallButton, Is.Not.Null);
            Assert.That(history, Is.Not.Null);
            Assert.That(historyRow, Is.Not.Null);
            Assert.That(targetText.text, Is.EqualTo("TargetLane_2 (100.0 m)"));
            Assert.That(shotCountText.text, Is.EqualTo("3 validation shots"));
            Assert.That(spreadText.text, Is.EqualTo("2.9 cm"));
            Assert.That(moaText.text, Is.EqualTo("1.24 MOA"));
            Assert.That(savedGroupsText.text, Is.EqualTo("1"));
            Assert.That(feedbackText.text, Is.EqualTo("Install hooks to enable marker tracking."));
            Assert.That(saveGroupButton.enabledSelf, Is.True);
            Assert.That(uninstallButton.enabledSelf, Is.False);
            Assert.That(historyRow.text, Is.EqualTo("#1 - 3 validation shots - 1.24 MOA - 2.9 cm"));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_SaveGroupIntent_UpdatesSessionHistoryList()
        {
            var go = new GameObject("TabInventoryControllerDeviceSaveHistory");
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var inputSource = go.AddComponent<TestInputSource>();
            var controller = go.AddComponent<TabInventoryController>();
            var deviceController = go.AddComponent<TestDeviceController>();
            deviceController.Status = new TabInventoryController.DeviceStatus(
                hasTarget: true,
                targetDisplayName: "TargetLane_2",
                targetDistanceMeters: 100f,
                shotCount: 3,
                isMoaAvailable: true,
                moa: 1.24d,
                spreadMeters: 0.029d,
                savedGroupCount: 0,
                canSaveGroup: true,
                canClearGroup: true,
                canInstallHooks: false,
                canUninstallHooks: false,
                attachmentFeedbackText: "Ready to save.",
                savedGroups: Array.Empty<TabInventoryController.DeviceSavedGroupEntry>());

            controller.SetInventoryController(inventoryController);
            controller.SetInputSource(inputSource);
            controller.SetDeviceController(deviceController);
            controller.Configure(binder, new TabInventoryDragController());

            inputSource.MenuTogglePressedThisFrame = true;
            controller.Tick();
            controller.HandleIntent(new UiIntent("tab.inventory.device.save-group"));

            var savedGroupsText = root.Q<Label>("inventory__device-saved-groups-value");
            var historyRow = root.Q<Label>("inventory__device-session-row-0");
            Assert.That(savedGroupsText, Is.Not.Null);
            Assert.That(historyRow, Is.Not.Null);
            Assert.That(savedGroupsText.text, Is.EqualTo("1"));
            Assert.That(historyRow.text, Is.EqualTo("#1 - 3 validation shots - 1.24 MOA - 2.9 cm"));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_WiresDeviceControllerIntoTabController()
        {
            var go = new GameObject("UiToolkitBridgeDeviceWiring");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);
            var deviceController = go.AddComponent<TestDeviceController>();

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(bridge, new object[] { BuildRoot(), UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, null }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                var deviceField = typeof(TabInventoryController).GetField("_deviceController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(deviceField, Is.Not.Null);
                Assert.That(deviceField.GetValue(tabController), Is.SameAs(deviceController));
            }
            finally
            {
                subscription?.Dispose();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_EnsuresTargetSelectionControllerExists()
        {
            var selectionControllerType = Type.GetType(
                "Reloader.PlayerDevice.World.PlayerDeviceTargetSelectionController, Reloader.PlayerDevice");
            Assert.That(selectionControllerType, Is.Not.Null, "PlayerDeviceTargetSelectionController type should exist.");

            var go = new GameObject("UiToolkitBridgeTargetSelectionWiring");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(bridge, new object[] { BuildRoot(), UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, null }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                var deviceControllerField = typeof(TabInventoryController).GetField("_deviceController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(deviceControllerField, Is.Not.Null);
                var deviceController = deviceControllerField.GetValue(tabController);
                Assert.That(deviceController, Is.Not.Null);

                var adapterSelectionField = deviceController.GetType().GetField("_targetSelectionController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(adapterSelectionField, Is.Not.Null);
                Assert.That(
                    adapterSelectionField.GetValue(deviceController),
                    Is.Not.Null,
                    "Bridge should ensure a target-selection controller exists for choose-target flow.");
            }
            finally
            {
                subscription?.Dispose();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_ChooseTargetIntent_EntersSelectionModeThroughAdapter()
        {
            var go = new GameObject("UiToolkitBridgeChooseTargetRouting");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(bridge, new object[] { BuildRoot(), UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, null }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                var deviceControllerField = typeof(TabInventoryController).GetField("_deviceController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(deviceControllerField, Is.Not.Null);
                var deviceController = deviceControllerField.GetValue(tabController);
                Assert.That(deviceController, Is.Not.Null);

                var adapterSelectionField = deviceController.GetType().GetField("_targetSelectionController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(adapterSelectionField, Is.Not.Null);
                var selectionController = adapterSelectionField.GetValue(deviceController);
                Assert.That(selectionController, Is.Not.Null);

                var isSelectingField = selectionController.GetType().GetField("_isSelectingTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(isSelectingField, Is.Not.Null);
                Assert.That((bool)isSelectingField.GetValue(selectionController), Is.False);

                tabController.HandleIntent(new UiIntent("tab.inventory.device.choose-target"));

                Assert.That((bool)isSelectingField.GetValue(selectionController), Is.True);

                // Verify remaining device intents route without null-path exceptions.
                tabController.HandleIntent(new UiIntent("tab.inventory.device.save-group"));
                tabController.HandleIntent(new UiIntent("tab.inventory.device.clear-group"));
                tabController.HandleIntent(new UiIntent("tab.inventory.device.install-hooks"));
                tabController.HandleIntent(new UiIntent("tab.inventory.device.uninstall-hooks"));
            }
            finally
            {
                subscription?.Dispose();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_DefaultAdapter_UsesAttachmentCatalogFromInventoryDefinitions()
        {
            var go = new GameObject("UiToolkitBridgeAttachmentCatalog");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            runtime.TryStoreItem("attachment.rangefinder", out _, out var selectedBeltIndex, out _);
            runtime.SelectBeltSlot(selectedBeltIndex);
            inventoryController.Configure(null, null, runtime);

            var rangefinderDefinition = ScriptableObject.CreateInstance<ItemDefinition>();
            rangefinderDefinition.SetValuesForTests("attachment.rangefinder", ItemCategory.Misc, "Rangefinder", ItemStackPolicy.NonStackable, 1);
            SetPrivateField(typeof(PlayerInventoryController), inventoryController, "_itemDefinitionRegistry", new List<ItemDefinition> { rangefinderDefinition });

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(bridge, new object[] { BuildRoot(), UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, null }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                var adapterField = typeof(TabInventoryController).GetField("_deviceController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(adapterField, Is.Not.Null);
                var adapter = adapterField.GetValue(tabController);
                Assert.That(adapter, Is.Not.Null);

                var tryGetStatus = adapter.GetType().GetMethod("TryGetStatus", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(tryGetStatus, Is.Not.Null);
                var args = new object[] { null };
                var ok = (bool)tryGetStatus.Invoke(adapter, args);
                Assert.That(ok, Is.True);

                var status = (TabInventoryController.DeviceStatus)args[0];
                Assert.That(status.CanInstallHooks, Is.True);
            }
            finally
            {
                subscription?.Dispose();
                UnityEngine.Object.DestroyImmediate(rangefinderDefinition);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_DefaultAdapter_StatusFeedback_ReflectsInstalledHooksState()
        {
            var go = new GameObject("UiToolkitBridgeInstalledHooksFeedback");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var deviceRuntimeStateType = Type.GetType("Reloader.PlayerDevice.Runtime.PlayerDeviceRuntimeState, Reloader.PlayerDevice");
            Assert.That(deviceRuntimeStateType, Is.Not.Null);
            var deviceAttachmentType = Type.GetType("Reloader.PlayerDevice.Runtime.DeviceAttachmentType, Reloader.PlayerDevice");
            Assert.That(deviceAttachmentType, Is.Not.Null);
            var installAttachmentMethod = deviceRuntimeStateType.GetMethod("InstallAttachment", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(installAttachmentMethod, Is.Not.Null);
            var rangefinderEnum = Enum.Parse(deviceAttachmentType, "Rangefinder");
            var deviceRuntimeState = Activator.CreateInstance(deviceRuntimeStateType);
            installAttachmentMethod.Invoke(deviceRuntimeState, new[] { rangefinderEnum });
            SetPrivateField(typeof(UiToolkitScreenRuntimeBridge), bridge, "_playerDeviceRuntimeState", deviceRuntimeState);

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var subscription = bindMethod.Invoke(bridge, new object[] { BuildRoot(), UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, null }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                var adapterField = typeof(TabInventoryController).GetField("_deviceController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(adapterField, Is.Not.Null);
                var adapter = adapterField.GetValue(tabController);
                Assert.That(adapter, Is.Not.Null);

                var tryGetStatus = adapter.GetType().GetMethod("TryGetStatus", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(tryGetStatus, Is.Not.Null);
                var args = new object[] { null };
                var ok = (bool)tryGetStatus.Invoke(adapter, args);
                Assert.That(ok, Is.True);

                var status = (TabInventoryController.DeviceStatus)args[0];
                Assert.That(status.AttachmentFeedbackText, Is.EqualTo("Recon hooks installed."));
            }
            finally
            {
                subscription?.Dispose();
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_BindTabInventory_WhenActiveContractExists_KeepsDeviceFeedbackDeviceSpecific()
        {
            var definitionType = Type.GetType("Reloader.Contracts.Runtime.AssassinationContractDefinition, Reloader.Contracts");
            var providerType = Type.GetType("Reloader.Contracts.Runtime.StaticContractRuntimeProvider, Reloader.Core");
            var populationBridgeType = Type.GetType("Reloader.NPCs.Runtime.CivilianPopulationRuntimeBridge, Reloader.NPCs");
            var populationRecordType = Type.GetType("Reloader.Core.Save.Modules.CivilianPopulationRecord, Reloader.Core");
            var spawnedCivilianType = Type.GetType("Reloader.NPCs.Runtime.MainTownPopulationSpawnedCivilian, Reloader.NPCs");
            Assert.That(definitionType, Is.Not.Null);
            Assert.That(providerType, Is.Not.Null);
            Assert.That(populationBridgeType, Is.Not.Null);
            Assert.That(populationRecordType, Is.Not.Null);
            Assert.That(spawnedCivilianType, Is.Not.Null);

            var go = new GameObject("UiToolkitBridgeDeviceIgnoresContractTracking");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryRuntime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, inventoryRuntime);
            var inputSource = go.AddComponent<TestInputSource>();

            var providerGo = new GameObject("StaticContractRuntimeProviderDeviceFeedback");
            var provider = providerGo.AddComponent(providerType);
            var definition = ScriptableObject.CreateInstance(definitionType);
            SetPrivateField(definitionType, definition, "_contractId", "contract.device.feedback");
            SetPrivateField(definitionType, definition, "_targetId", "target.device.feedback");
            SetPrivateField(definitionType, definition, "_title", "Square Watch");
            SetPrivateField(definitionType, definition, "_targetDisplayName", "Tomas Varga");
            SetPrivateField(definitionType, definition, "_targetDescription", "Tan coat, pacing the fountain.");
            SetPrivateField(definitionType, definition, "_briefingText", "Keep visual contact and confirm the square before taking the shot.");
            SetPrivateField(definitionType, definition, "_distanceBand", 160f);
            SetPrivateField(definitionType, definition, "_payout", 1500);
            var setAvailableContractMethod = providerType.GetMethod("SetAvailableContract", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(setAvailableContractMethod, Is.Not.Null);
            setAvailableContractMethod.Invoke(provider, new object[] { definition });

            var populationGo = new GameObject("CivilianPopulationRuntimeBridgeDeviceFeedback");
            var populationBridge = populationGo.AddComponent(populationBridgeType);
            var runtimeProperty = populationBridgeType.GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(runtimeProperty, Is.Not.Null);
            var runtimeState = runtimeProperty.GetValue(populationBridge);
            var civiliansProperty = runtimeState.GetType().GetProperty("Civilians", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(civiliansProperty, Is.Not.Null);
            var civilians = civiliansProperty.GetValue(runtimeState) as System.Collections.IList;
            Assert.That(civilians, Is.Not.Null);
            var record = Activator.CreateInstance(populationRecordType);
            SetProperty(populationRecordType, record, "CivilianId", "target.device.feedback");
            SetProperty(populationRecordType, record, "PopulationSlotId", "townsfolk.device.feedback");
            SetProperty(populationRecordType, record, "PoolId", "townsfolk");
            SetProperty(populationRecordType, record, "SpawnAnchorId", "Anchor_Device");
            SetProperty(populationRecordType, record, "AreaTag", "maintown.square");
            SetProperty(populationRecordType, record, "IsAlive", true);
            civilians.Add(record);

            var spawnedGo = new GameObject("SpawnedCivilianDeviceFeedback");
            spawnedGo.transform.position = new Vector3(0f, 0f, 12f);
            var spawnedCivilian = spawnedGo.AddComponent(spawnedCivilianType);
            var initializeMethod = spawnedCivilianType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(initializeMethod, Is.Not.Null);
            initializeMethod.Invoke(spawnedCivilian, new[] { record });
            var registerMethod = runtimeState.GetType().GetMethod("RegisterSpawnedCivilian", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(registerMethod, Is.Not.Null);
            registerMethod.Invoke(runtimeState, new object[] { spawnedCivilian });

            var viewerGo = new GameObject("DeviceFeedbackViewer");
            viewerGo.tag = "MainCamera";
            viewerGo.AddComponent<Camera>();
            viewerGo.transform.position = Vector3.zero;

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var root = BuildRoot();
            var subscription = bindMethod.Invoke(bridge, new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, inputSource }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                inputSource.MenuTogglePressedThisFrame = true;
                tabController.Tick();
                tabController.HandleIntent(new UiIntent("tab.menu.select", "contracts"));
                tabController.HandleIntent(new UiIntent("tab.inventory.contracts.accept"));
                tabController.HandleIntent(new UiIntent("tab.menu.select", "device"));

                var deviceFeedback = root.Q<Label>("inventory__device-install-feedback-text");
                var selectedTargetText = root.Q<Label>("inventory__device-selected-target-value");
                Assert.That(deviceFeedback, Is.Not.Null);
                Assert.That(selectedTargetText, Is.Not.Null);
                Assert.That(deviceFeedback.text, Is.EqualTo("Recon hooks are not installed."));
                Assert.That(deviceFeedback.text, Does.Not.Contain("TRACK:"));
                Assert.That(selectedTargetText.text, Is.EqualTo("No target marked"));
            }
            finally
            {
                subscription?.Dispose();
                UnityEngine.Object.DestroyImmediate(viewerGo);
                UnityEngine.Object.DestroyImmediate(spawnedGo);
                UnityEngine.Object.DestroyImmediate(populationGo);
                UnityEngine.Object.DestroyImmediate((UnityEngine.Object)definition);
                UnityEngine.Object.DestroyImmediate(providerGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_ResolveTabDeviceControllerAdapter_RecreatesControllerWhenInventoryChanges()
        {
            var go = new GameObject("UiToolkitBridgeInventorySwitch");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var firstInventory = go.AddComponent<PlayerInventoryController>();
            var firstRuntime = new PlayerInventoryRuntime();
            firstRuntime.SetBackpackCapacity(0);
            firstInventory.Configure(null, null, firstRuntime);

            var secondInventory = new GameObject("SecondInventoryOwner").AddComponent<PlayerInventoryController>();
            var secondRuntime = new PlayerInventoryRuntime();
            secondRuntime.SetBackpackCapacity(0);
            secondInventory.Configure(null, null, secondRuntime);

            var resolveMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "ResolveTabDeviceControllerAdapter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null);

            try
            {
                resolveMethod.Invoke(bridge, new object[] { firstInventory, null });
                resolveMethod.Invoke(bridge, new object[] { secondInventory, null });

                var bridgeControllerField = typeof(UiToolkitScreenRuntimeBridge).GetField("_playerDeviceController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bridgeControllerField, Is.Not.Null);
                var bridgeController = bridgeControllerField.GetValue(bridge);
                Assert.That(bridgeController, Is.Not.Null);

                var inventoryField = bridgeController.GetType().GetField("_inventoryController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(inventoryField, Is.Not.Null);
                Assert.That(inventoryField.GetValue(bridgeController), Is.SameAs(secondInventory));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(secondInventory.gameObject);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RuntimeBridge_OnDisable_ClearsPlayerDeviceActiveInstance()
        {
            var go = new GameObject("UiToolkitBridgeClearsActiveDeviceController");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var runtime = new PlayerInventoryRuntime();
            runtime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, runtime);

            var resolveMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "ResolveTabDeviceControllerAdapter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(resolveMethod, Is.Not.Null);
            var playerDeviceControllerType = Type.GetType("Reloader.PlayerDevice.World.PlayerDeviceController, Reloader.PlayerDevice");
            Assert.That(playerDeviceControllerType, Is.Not.Null);
            var activeInstanceProperty = playerDeviceControllerType.GetProperty("ActiveInstance", BindingFlags.Static | BindingFlags.Public);
            Assert.That(activeInstanceProperty, Is.Not.Null);

            try
            {
                resolveMethod.Invoke(bridge, new object[] { inventoryController, null });
                Assert.That(activeInstanceProperty.GetValue(null), Is.Not.Null);

                go.SetActive(false);

                Assert.That(activeInstanceProperty.GetValue(null), Is.Null);
            }
            finally
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void Acceptance_DeviceFullLoop_ChooseTargetFireSaveClearReopenTab_PreservesSavedSessionAndClearsMarkers()
        {
            var targetSelectionControllerType = Type.GetType(
                "Reloader.PlayerDevice.World.PlayerDeviceTargetSelectionController, Reloader.PlayerDevice");
            var dummyTargetDamageableType = Type.GetType("Reloader.Weapons.World.DummyTargetDamageable, Reloader.Weapons");
            var targetImpactMarkerType = Type.GetType("Reloader.Weapons.World.TargetImpactMarker, Reloader.Weapons");
            Assert.That(targetSelectionControllerType, Is.Not.Null);
            Assert.That(dummyTargetDamageableType, Is.Not.Null);
            Assert.That(targetImpactMarkerType, Is.Not.Null);

            var go = new GameObject("UiToolkitBridgeDeviceFullLoop");
            var bridge = go.AddComponent<UiToolkitScreenRuntimeBridge>();
            var inventoryController = go.AddComponent<PlayerInventoryController>();
            var inventoryRuntime = new PlayerInventoryRuntime();
            inventoryRuntime.SetBackpackCapacity(0);
            inventoryController.Configure(null, null, inventoryRuntime);
            var inputSource = go.AddComponent<TestInputSource>();

            var cameraGo = new GameObject("DeviceSelectionCamera");
            cameraGo.transform.position = Vector3.zero;
            cameraGo.transform.forward = Vector3.forward;
            cameraGo.tag = "MainCamera";
            cameraGo.AddComponent<Camera>();

            var targetParent = new GameObject("LaneAcceptance");
            var target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            target.name = "AcceptanceTarget";
            target.transform.SetParent(targetParent.transform, worldPositionStays: false);
            target.transform.position = new Vector3(0f, 0f, 8f);
            var targetDamageable = target.AddComponent(dummyTargetDamageableType);
            SetPrivateField(dummyTargetDamageableType, targetDamageable, "_targetId", "target.acceptance");
            SetPrivateField(dummyTargetDamageableType, targetDamageable, "_displayName", "Acceptance Lane");
            SetPrivateField(dummyTargetDamageableType, targetDamageable, "_authoritativeDistanceMeters", 120f);

            var markerPrefab = new GameObject("AcceptanceMarkerPrefab");
            markerPrefab.AddComponent(targetImpactMarkerType);
            SetPrivateField(dummyTargetDamageableType, targetDamageable, "_impactMarkerPrefab", markerPrefab);

            var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod(
                "BindTabInventory",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bindMethod, Is.Not.Null);

            var root = BuildRoot();
            var subscription = bindMethod.Invoke(bridge, new object[] { root, UiRuntimeCompositionIds.ControllerObjectNames.TabInventory, inventoryController, inputSource }) as IDisposable;
            Assert.That(subscription, Is.Not.Null);

            try
            {
                var tabController = go.transform.Find(UiRuntimeCompositionIds.ControllerObjectNames.TabInventory)?.GetComponent<TabInventoryController>();
                Assert.That(tabController, Is.Not.Null);

                inputSource.MenuTogglePressedThisFrame = true;
                tabController.Tick();

                tabController.HandleIntent(new UiIntent("tab.inventory.device.choose-target"));
                inputSource.PickupPressedThisFrame = true;
                var targetSelectionController = go.GetComponentInChildren(targetSelectionControllerType);
                Assert.That(targetSelectionController, Is.Not.Null);
                InvokeMethod(targetSelectionControllerType, targetSelectionController, "Tick");

                var deviceController = ResolveBridgeDeviceController(tabController);
                Assert.That(deviceController, Is.Not.Null);
                FireImpact(deviceController, target, new Vector3(0.05f, 0.01f, 0f));
                FireImpact(deviceController, target, new Vector3(-0.04f, -0.02f, 0f));

                UnityEngine.Object.Instantiate(markerPrefab, target.transform);
                UnityEngine.Object.Instantiate(markerPrefab, target.transform);
                tabController.HandleIntent(new UiIntent("tab.menu.select", "device"));

                var targetText = root.Q<Label>("inventory__device-selected-target-value");
                var shotCountText = root.Q<Label>("inventory__device-shot-count-value");
                var spreadText = root.Q<Label>("inventory__device-spread-value");
                var moaText = root.Q<Label>("inventory__device-moa-value");
                var savedGroupsText = root.Q<Label>("inventory__device-saved-groups-value");

                Assert.That(targetText.text, Does.StartWith("Acceptance Lane ("));
                Assert.That(shotCountText.text, Is.EqualTo("2 validation shots"));
                Assert.That(spreadText.text, Is.Not.EqualTo("--"));
                Assert.That(moaText.text, Is.Not.EqualTo("--"));
                Assert.That(savedGroupsText.text, Is.EqualTo("0"));

                tabController.HandleIntent(new UiIntent("tab.inventory.device.save-group"));
                var historyRow = root.Q<Label>("inventory__device-session-row-0");
                Assert.That(savedGroupsText.text, Is.EqualTo("1"));
                Assert.That(historyRow, Is.Not.Null);
                Assert.That(historyRow.text, Does.StartWith("#1 - 2 validation shots"));

                tabController.HandleIntent(new UiIntent("tab.inventory.device.clear-group"));
                Assert.That(shotCountText.text, Is.EqualTo("0 validation shots"));
                Assert.That(target.GetComponentsInChildren(targetImpactMarkerType, includeInactive: true).Length, Is.EqualTo(0));

                inputSource.MenuTogglePressedThisFrame = true;
                tabController.Tick();
                inputSource.MenuTogglePressedThisFrame = true;
                tabController.Tick();

                Assert.That(savedGroupsText.text, Is.EqualTo("1"));
                Assert.That(shotCountText.text, Is.EqualTo("0 validation shots"));
                Assert.That(targetText.text, Does.StartWith("Acceptance Lane ("));
            }
            finally
            {
                subscription.Dispose();
                UnityEngine.Object.DestroyImmediate(markerPrefab);
                UnityEngine.Object.DestroyImmediate(targetParent);
                UnityEngine.Object.DestroyImmediate(cameraGo);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Render_DefaultState_ShowsDeviceSectionAndNotes()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            binder.Render(TabInventoryUiState.Create(
                isOpen: true,
                beltSlots: Array.Empty<TabInventoryUiState.SlotState>(),
                backpackSlots: Array.Empty<TabInventoryUiState.SlotState>(),
                tooltipTitle: string.Empty,
                tooltipVisible: false));

            var deviceTab = root.Q<Button>("inventory__tab-device");
            var deviceSection = root.Q<VisualElement>("inventory__section-device");
            var deviceNotes = root.Q<VisualElement>("inventory__device-notes");
            var targetValue = root.Q<Label>("inventory__device-selected-target-value");
            var shotCountValue = root.Q<Label>("inventory__device-shot-count-value");
            var spreadValue = root.Q<Label>("inventory__device-spread-value");
            var moaValue = root.Q<Label>("inventory__device-moa-value");
            var savedGroupsValue = root.Q<Label>("inventory__device-saved-groups-value");

            Assert.That(deviceTab, Is.Not.Null);
            Assert.That(deviceSection.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(deviceNotes.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(targetValue.text, Is.EqualTo("No target marked"));
            Assert.That(shotCountValue.text, Is.EqualTo("0 validation shots"));
            Assert.That(spreadValue.text, Is.EqualTo("--"));
            Assert.That(moaValue.text, Is.EqualTo("--"));
            Assert.That(savedGroupsValue.text, Is.EqualTo("0"));
        }

        [Test]
        public void Render_InvalidInstallState_DisablesButtonsAndShowsFeedback()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            binder.Render(TabInventoryUiState.Create(
                isOpen: true,
                beltSlots: Array.Empty<TabInventoryUiState.SlotState>(),
                backpackSlots: Array.Empty<TabInventoryUiState.SlotState>(),
                tooltipTitle: string.Empty,
                tooltipVisible: false,
                deviceCanInstallHooks: false,
                deviceCanUninstallHooks: false,
                deviceInstallFeedbackText: "Select recon hooks in your belt to install.",
                deviceSessionHistoryEntries: Array.Empty<string>()));

            var installButton = root.Q<Button>("inventory__device-install-hooks");
            var uninstallButton = root.Q<Button>("inventory__device-uninstall-hooks");
            var feedback = root.Q<Label>("inventory__device-install-feedback-text");
            Assert.That(installButton.enabledSelf, Is.False);
            Assert.That(uninstallButton.enabledSelf, Is.False);
            Assert.That(feedback.text, Is.EqualTo("Select recon hooks in your belt to install."));
        }

        [Test]
        public void DeviceActions_EmitUiOnlyIntents()
        {
            var root = BuildRoot();
            var binder = new TabInventoryViewBinder();
            binder.Initialize(root, beltSlotCount: 0, backpackSlotCount: 0);

            var intents = new List<UiIntent>();
            binder.IntentRaised += intents.Add;

            Assert.That(binder.TryInvokeTabSelectionForTests("device"), Is.True);
            Assert.That(binder.TryInvokeDeviceActionForTests("choose-target"), Is.True);
            Assert.That(binder.TryInvokeDeviceActionForTests("save-group"), Is.True);
            Assert.That(binder.TryInvokeDeviceActionForTests("clear-group"), Is.True);
            Assert.That(binder.TryInvokeDeviceActionForTests("install-hooks"), Is.True);
            Assert.That(binder.TryInvokeDeviceActionForTests("uninstall-hooks"), Is.True);

            Assert.That(intents.Count, Is.EqualTo(6));
            Assert.That(intents[0].Key, Is.EqualTo("tab.menu.select"));
            Assert.That(intents[0].Payload, Is.EqualTo("device"));
            Assert.That(intents[1].Key, Is.EqualTo("tab.inventory.device.choose-target"));
            Assert.That(intents[2].Key, Is.EqualTo("tab.inventory.device.save-group"));
            Assert.That(intents[3].Key, Is.EqualTo("tab.inventory.device.clear-group"));
            Assert.That(intents[4].Key, Is.EqualTo("tab.inventory.device.install-hooks"));
            Assert.That(intents[5].Key, Is.EqualTo("tab.inventory.device.uninstall-hooks"));
        }

        private static object ResolveBridgeDeviceController(TabInventoryController tabController)
        {
            var adapterField = typeof(TabInventoryController).GetField("_deviceController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(adapterField, Is.Not.Null);
            var adapter = adapterField.GetValue(tabController);
            Assert.That(adapter, Is.Not.Null);

            var bridgeControllerField = adapter.GetType().GetField("_deviceController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bridgeControllerField, Is.Not.Null);
            return bridgeControllerField.GetValue(adapter);
        }

        private static void FireImpact(object deviceController, GameObject hitObject, Vector3 targetLocalPoint)
        {
            var impactPoint = hitObject.transform.TransformPoint(targetLocalPoint);
            var ingestMethod = deviceController.GetType().GetMethod("IngestImpact", new[] { typeof(Vector3), typeof(GameObject) });
            Assert.That(ingestMethod, Is.Not.Null);
            ingestMethod.Invoke(deviceController, new object[] { impactPoint, hitObject });
        }

        private static void SetPrivateField(Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {type?.Name}.");
            field.SetValue(target, value);
        }

        private static void SetProperty(Type type, object target, string propertyName, object value)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on {type?.Name}.");
            property.SetValue(target, value);
        }

        private static void InvokeMethod(Type type, object instance, string methodName, params object[] args)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {type?.FullName}.");
            method.Invoke(instance, args);
        }

        private static VisualElement BuildRoot()
        {
            var root = new VisualElement { name = "inventory__root" };
            var panel = new VisualElement { name = "inventory__panel" };
            root.Add(panel);

            panel.Add(new VisualElement { name = "inventory__tabbar" });
            panel.Add(new Button { name = "inventory__tab-inventory" });
            panel.Add(new Button { name = "inventory__tab-quests" });
            panel.Add(new Button { name = "inventory__tab-journal" });
            panel.Add(new Button { name = "inventory__tab-calendar" });
            panel.Add(new Button { name = "inventory__tab-device" });

            panel.Add(new VisualElement { name = "inventory__section-inventory" });
            panel.Add(new VisualElement { name = "inventory__section-quests" });
            panel.Add(new VisualElement { name = "inventory__section-journal" });
            panel.Add(new VisualElement { name = "inventory__section-calendar" });
            panel.Add(new VisualElement { name = "inventory__section-device" });
            var deviceNotes = new VisualElement { name = "inventory__device-notes" };
            deviceNotes.Add(new Label { name = "inventory__device-selected-target-value" });
            deviceNotes.Add(new Label { name = "inventory__device-shot-count-value" });
            deviceNotes.Add(new Label { name = "inventory__device-spread-value" });
            deviceNotes.Add(new Label { name = "inventory__device-moa-value" });
            deviceNotes.Add(new Label { name = "inventory__device-saved-groups-value" });
            panel.Add(deviceNotes);
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

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource
        {
            public bool MenuTogglePressedThisFrame { get; set; }
            public bool PickupPressedThisFrame { get; set; }

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed()
            {
                var result = PickupPressedThisFrame;
                PickupPressedThisFrame = false;
                return result;
            }
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

        private sealed class TestDeviceController : MonoBehaviour, TabInventoryController.IDeviceController
        {
            public int ChooseTargetCalls { get; private set; }
            public int SaveGroupCalls { get; private set; }
            public int ClearGroupCalls { get; private set; }
            public int InstallHooksCalls { get; private set; }
            public int UninstallHooksCalls { get; private set; }
            public TabInventoryController.DeviceStatus Status { get; set; }

            public bool TryGetStatus(out TabInventoryController.DeviceStatus status)
            {
                status = Status;
                return true;
            }

            public void ChooseTarget()
            {
                ChooseTargetCalls++;
            }

            public void SaveGroup()
            {
                SaveGroupCalls++;
                if (!Status.CanSaveGroup)
                {
                    return;
                }

                var groups = new List<TabInventoryController.DeviceSavedGroupEntry>(Status.SavedGroups ?? Array.Empty<TabInventoryController.DeviceSavedGroupEntry>())
                {
                    new TabInventoryController.DeviceSavedGroupEntry(Status.ShotCount, Status.IsMoaAvailable, Status.Moa, Status.SpreadMeters)
                };
                Status = new TabInventoryController.DeviceStatus(
                    hasTarget: Status.HasTarget,
                    targetDisplayName: Status.TargetDisplayName,
                    targetDistanceMeters: Status.TargetDistanceMeters,
                    shotCount: Status.ShotCount,
                    isMoaAvailable: Status.IsMoaAvailable,
                    moa: Status.Moa,
                    spreadMeters: Status.SpreadMeters,
                    savedGroupCount: groups.Count,
                    canSaveGroup: Status.CanSaveGroup,
                    canClearGroup: Status.CanClearGroup,
                    canInstallHooks: Status.CanInstallHooks,
                    canUninstallHooks: Status.CanUninstallHooks,
                    attachmentFeedbackText: "Group saved.",
                    savedGroups: groups);
            }

            public void ClearGroup()
            {
                ClearGroupCalls++;
            }

            public bool InstallHooks()
            {
                InstallHooksCalls++;
                return true;
            }

            public bool UninstallHooks()
            {
                UninstallHooksCalls++;
                return true;
            }
        }


private static void CleanupScene()
        {
            DestroyOwnersOfType<UiToolkitScreenRuntimeBridge>();
            DestroyOwnersOfType<PlayerInventoryController>();
            DestroyOwnersOfType<PlayerWeaponController>();
            DestroyOwnersOfType<WeaponRegistry>();
            DestroyOwnersOfType<Camera>();
            DestroyOwnersOfType<TestInputSource>();
            DestroyOwnersOfType<TestDeviceController>();
        }

        private static void DestroyOwnersOfType<T>() where T : Component
        {
            var components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && component.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(component.gameObject);
                }
            }
        }
}
}
