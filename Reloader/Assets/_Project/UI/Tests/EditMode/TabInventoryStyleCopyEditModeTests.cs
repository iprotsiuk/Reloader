using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Reloader.UI.Tests.EditMode
{
    public class TabInventoryStyleCopyEditModeTests
    {
        [Test]
        public void ContractsLayout_UsesCompactDensityTokens()
        {
            var uss = File.ReadAllText(Path.Combine(Application.dataPath, "_Project/UI/Toolkit/USS/TabInventory.uss"));

            var railRule = ExtractRule(uss, ".inventory__rail");
            Assert.That(railRule, Does.Contain("width: 52px;"));
            Assert.That(railRule, Does.Contain("min-width: 48px;"));
            Assert.That(railRule, Does.Contain("max-width: 56px;"));

            var detailPaneRule = ExtractRule(uss, ".inventory__detail-pane");
            Assert.That(detailPaneRule, Does.Contain("width: 136px;"));
            Assert.That(detailPaneRule, Does.Contain("min-width: 116px;"));
            Assert.That(detailPaneRule, Does.Contain("max-width: 144px;"));
            Assert.That(detailPaneRule, Does.Contain("padding-top: 6px;"));

            var rowRule = ExtractRule(uss, ".inventory__contracts-row");
            Assert.That(rowRule, Does.Contain("min-height: 60px;"));
            Assert.That(rowRule, Does.Contain("padding-right: 6px;"));

            var payoutRule = ExtractRule(uss, ".inventory__contracts-payout");
            Assert.That(payoutRule, Does.Contain("width: 58px;"));
            Assert.That(payoutRule, Does.Contain("margin-right: 4px;"));

            var actionRule = ExtractRule(uss, ".inventory__contracts-primary-action");
            Assert.That(actionRule, Does.Contain("width: 52px;"));
            Assert.That(actionRule, Does.Contain("height: 24px;"));
        }

        private static string ExtractRule(string uss, string selector)
        {
            var selectorIndex = uss.IndexOf(selector);
            Assert.That(selectorIndex, Is.GreaterThanOrEqualTo(0), $"Expected selector '{selector}'.");

            var braceStart = uss.IndexOf('{', selectorIndex);
            Assert.That(braceStart, Is.GreaterThan(selectorIndex), $"Expected opening brace for '{selector}'.");

            var braceEnd = uss.IndexOf('}', braceStart);
            Assert.That(braceEnd, Is.GreaterThan(braceStart), $"Expected closing brace for '{selector}'.");

            return uss.Substring(braceStart, braceEnd - braceStart + 1);
        }
    }
}
