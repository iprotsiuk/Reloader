using System;
using System.Collections.Generic;

namespace Reloader.UI.Toolkit.Runtime
{
    public sealed class UiActionMapConfig
    {
        private readonly Dictionary<string, string> _intentToCommand = new(StringComparer.Ordinal);

        public void Set(string intentKey, string commandName)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                throw new ArgumentException("Intent key is required.", nameof(intentKey));
            }

            if (string.IsNullOrWhiteSpace(commandName))
            {
                throw new ArgumentException("Command name is required.", nameof(commandName));
            }

            _intentToCommand[intentKey] = commandName;
        }

        public bool TryResolve(string intentKey, out string commandName)
        {
            if (string.IsNullOrWhiteSpace(intentKey))
            {
                commandName = null;
                return false;
            }

            return _intentToCommand.TryGetValue(intentKey, out commandName);
        }

        public static UiActionMapConfig CreateWithDefaults()
        {
            var config = new UiActionMapConfig();
            config.Set(UiRuntimeCompositionIds.IntentKeys.BeltSlotSelect, "SelectBeltSlot");
            config.Set(UiRuntimeCompositionIds.IntentKeys.InventoryDragMerge, "MergeStacks");
            config.Set(UiRuntimeCompositionIds.IntentKeys.InventoryDragSwap, "SwapSlots");
            config.Set(UiRuntimeCompositionIds.IntentKeys.InventoryDragDrop, "DropStack");
            config.Set(UiRuntimeCompositionIds.IntentKeys.EscMenuResume, "ResumeGameplay");
            config.Set(UiRuntimeCompositionIds.IntentKeys.EscMenuSettings, "OpenEscSettings");
            config.Set(UiRuntimeCompositionIds.IntentKeys.EscMenuKeybindings, "OpenEscKeybindings");
            config.Set(UiRuntimeCompositionIds.IntentKeys.EscMenuQuit, "QuitToDesktop");
            config.Set(UiRuntimeCompositionIds.IntentKeys.TradeConfirmBuy, "ConfirmBuyOrder");
            config.Set(UiRuntimeCompositionIds.IntentKeys.TradeConfirmSell, "ConfirmSellOrder");
            config.Set(UiRuntimeCompositionIds.IntentKeys.ReloadingOperationSelect, "SelectReloadingOperation");
            config.Set(UiRuntimeCompositionIds.IntentKeys.ReloadingOperationExecute, "ExecuteReloadingOperation");
            return config;
        }
    }
}
