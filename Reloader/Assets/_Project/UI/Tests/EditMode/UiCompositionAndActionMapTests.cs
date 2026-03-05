using NUnit.Framework;
using Reloader.UI.Toolkit.Runtime;

namespace Reloader.UI.Tests.EditMode
{
    public class UiCompositionAndActionMapTests
    {
        [TestCase(UiRuntimeCompositionIds.ScreenIds.BeltHud)]
        [TestCase(UiRuntimeCompositionIds.ScreenIds.AmmoHud)]
        [TestCase(UiRuntimeCompositionIds.ScreenIds.TabInventory)]
        [TestCase(UiRuntimeCompositionIds.ScreenIds.EscMenu)]
        [TestCase(UiRuntimeCompositionIds.ScreenIds.Trade)]
        [TestCase(UiRuntimeCompositionIds.ScreenIds.ReloadingWorkbench)]
        public void CompositionConfig_DefaultsContainRequiredScreens(string screenId)
        {
            var config = UiScreenCompositionConfig.CreateWithDefaults();

            var resolved = config.TryGetComponents(screenId, out var components);

            Assert.That(resolved, Is.True);
            Assert.That(components, Is.Not.Null);
            Assert.That(components.Count, Is.GreaterThan(0));
        }

        [TestCase(UiRuntimeCompositionIds.IntentKeys.BeltSlotSelect)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.InventoryDragMerge)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.InventoryDragSwap)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.InventoryDragDrop)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.EscMenuResume)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.EscMenuSettings)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.EscMenuKeybindings)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.EscMenuQuit)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.TradeConfirmBuy)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.TradeConfirmSell)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.ReloadingOperationSelect)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.ReloadingOperationExecute)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.TabInventoryAttachmentsOpen)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.TabInventoryAttachmentsSlotSelected)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.TabInventoryAttachmentsItemSelected)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.TabInventoryAttachmentsApply)]
        [TestCase(UiRuntimeCompositionIds.IntentKeys.TabInventoryAttachmentsBack)]
        public void ActionMap_DefaultsContainRequiredIntentKeys(string intentKey)
        {
            var map = UiActionMapConfig.CreateWithDefaults();

            var resolved = map.TryResolve(intentKey, out var commandName);

            Assert.That(resolved, Is.True);
            Assert.That(string.IsNullOrWhiteSpace(commandName), Is.False);
        }
    }
}
