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
    }
}
