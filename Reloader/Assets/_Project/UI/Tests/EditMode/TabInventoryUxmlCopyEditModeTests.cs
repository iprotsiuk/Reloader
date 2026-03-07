using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.EditMode
{
    public class TabInventoryUxmlCopyEditModeTests
    {
        private const string TabInventoryUxmlPath = "Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml";

        [Test]
        public void DeviceSection_UsesContractPrepCopy()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TabInventoryUxmlPath);
            Assert.That(asset, Is.Not.Null, $"Expected UXML asset at '{TabInventoryUxmlPath}'.");

            var root = asset.CloneTree();
            var deviceSection = root.Q<VisualElement>("inventory__section-device");
            Assert.That(deviceSection, Is.Not.Null);

            var labelTexts = deviceSection.Query<Label>().ToList().Select(label => label.text).ToArray();
            var buttonTexts = deviceSection.Query<Button>().ToList().Select(button => button.text).ToArray();

            Assert.That(labelTexts, Does.Contain("Field Device"));
            Assert.That(labelTexts, Does.Contain("Contract Prep"));
            Assert.That(labelTexts, Does.Contain("Marked Target"));
            Assert.That(labelTexts, Does.Contain("Validation Shots"));
            Assert.That(labelTexts, Does.Contain("Logged Groups"));
            Assert.That(labelTexts, Does.Contain("Validation History"));
            Assert.That(buttonTexts, Does.Contain("Mark Target"));
            Assert.That(buttonTexts, Does.Contain("Log Group"));
            Assert.That(buttonTexts, Does.Contain("Reset Group"));
        }

        [Test]
        public void ContractsSection_AuthorsPostedFeedAndActiveWorkspaceShell()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TabInventoryUxmlPath);
            Assert.That(asset, Is.Not.Null, $"Expected UXML asset at '{TabInventoryUxmlPath}'.");

            var root = asset.CloneTree();
            var contractsSection = root.Q<VisualElement>("inventory__section-quests");
            Assert.That(contractsSection, Is.Not.Null);
            Assert.That(contractsSection.Q<VisualElement>("inventory__contracts-feed"), Is.Not.Null);
            Assert.That(contractsSection.Q<VisualElement>("inventory__contracts-row"), Is.Not.Null);
            Assert.That(contractsSection.Q<VisualElement>("inventory__contracts-active"), Is.Not.Null);
            Assert.That(contractsSection.Q<VisualElement>("inventory__contracts-active-header"), Is.Not.Null);
            Assert.That(contractsSection.Q<Label>("inventory__contracts-active-status"), Is.Not.Null);
            Assert.That(contractsSection.Q<Label>("inventory__contracts-active-payout"), Is.Not.Null);
            Assert.That(contractsSection.Q<VisualElement>("inventory__contracts-active-target-block"), Is.Not.Null);
            Assert.That(contractsSection.Q<Label>("inventory__contracts-target-summary"), Is.Not.Null);
            Assert.That(contractsSection.Q<VisualElement>("inventory__contracts-briefing-card"), Is.Not.Null);
            Assert.That(contractsSection.Q<VisualElement>("inventory__contracts-intel-card"), Is.Not.Null);
            Assert.That(contractsSection.Q<VisualElement>("inventory__contracts-active-footer"), Is.Not.Null);
            Assert.That(contractsSection.Q<Button>("inventory__contracts-primary-action"), Is.Not.Null);
            Assert.That(contractsSection.Q<Button>("inventory__contracts-active-primary-action"), Is.Not.Null);
            Assert.That(root.Q<Label>("inventory__header-meta"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("inventory__detail-pane-contracts"), Is.Not.Null);
            Assert.That(root.Q<Label>("inventory__detail-pane-reward-state"), Is.Not.Null);
        }

        [Test]
        public void ContractsActiveWorkspace_AuthorsStructuredMissionWorkspace()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TabInventoryUxmlPath);
            Assert.That(asset, Is.Not.Null, $"Expected UXML asset at '{TabInventoryUxmlPath}'.");

            var root = asset.CloneTree();
            var activeWorkspace = root.Q<VisualElement>("inventory__contracts-active");
            Assert.That(activeWorkspace, Is.Not.Null);
            Assert.That(activeWorkspace.Q<VisualElement>("inventory__contracts-active-header"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<Label>("inventory__contracts-active-status"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<Label>("inventory__contracts-active-payout"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<VisualElement>("inventory__contracts-active-target-block"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<Label>("inventory__contracts-target"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<Label>("inventory__contracts-target-summary"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<VisualElement>("inventory__contracts-briefing-card"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<VisualElement>("inventory__contracts-intel-card"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<VisualElement>("inventory__contracts-active-footer"), Is.Not.Null);
            Assert.That(activeWorkspace.Q<Button>("inventory__contracts-active-primary-action"), Is.Not.Null);
        }

        [Test]
        public void ContractsDetailPane_UsesCompactFieldRows()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TabInventoryUxmlPath);
            Assert.That(asset, Is.Not.Null, $"Expected UXML asset at '{TabInventoryUxmlPath}'.");

            var root = asset.CloneTree();
            var contractsDetailPane = root.Q<VisualElement>("inventory__detail-pane-contracts");
            Assert.That(contractsDetailPane, Is.Not.Null);

            var fieldRows = contractsDetailPane.Query<VisualElement>(className: "inventory__detail-pane-field").ToList();
            Assert.That(fieldRows, Has.Count.EqualTo(5));

            foreach (var fieldRow in fieldRows)
            {
                Assert.That(fieldRow.Q<Label>(className: "inventory__detail-pane-field-label"), Is.Not.Null);
                Assert.That(fieldRow.Q<Label>(className: "inventory__mock-line"), Is.Not.Null);
            }
        }

        [Test]
        public void DeviceAndAttachmentsActions_UseActionButtonClass_NotNavigationTabClass()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TabInventoryUxmlPath);
            Assert.That(asset, Is.Not.Null, $"Expected UXML asset at '{TabInventoryUxmlPath}'.");

            var root = asset.CloneTree();
            var attachmentsApply = root.Q<Button>("inventory__attachments-apply");
            var attachmentsBack = root.Q<Button>("inventory__attachments-back");
            var deviceChooseTarget = root.Q<Button>("inventory__device-choose-target");
            Assert.That(attachmentsApply, Is.Not.Null);
            Assert.That(attachmentsBack, Is.Not.Null);
            Assert.That(deviceChooseTarget, Is.Not.Null);

            Assert.That(attachmentsApply.ClassListContains("inventory__action-button"), Is.True);
            Assert.That(attachmentsBack.ClassListContains("inventory__action-button"), Is.True);
            Assert.That(deviceChooseTarget.ClassListContains("inventory__action-button"), Is.True);
            Assert.That(attachmentsApply.ClassListContains("inventory__tab"), Is.False);
            Assert.That(attachmentsBack.ClassListContains("inventory__tab"), Is.False);
            Assert.That(deviceChooseTarget.ClassListContains("inventory__tab"), Is.False);
        }
    }
}
