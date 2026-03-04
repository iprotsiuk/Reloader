namespace Reloader.UI.Toolkit.Runtime
{
    public static class UiRuntimeCompositionIds
    {
        public static class ScreenIds
        {
            public const string BeltHud = "belt-hud";
            public const string AmmoHud = "ammo-hud";
            public const string TabInventory = "tab-inventory";
            public const string EscMenu = "esc-menu";
            public const string ChestInventory = "chest-inventory";
            public const string Trade = "trade-ui";
            public const string ReloadingWorkbench = "reloading-workbench";
            public const string InteractionHint = "interaction-hint";
        }

        public static class ControllerObjectNames
        {
            public const string BeltHud = "belt-hud-controller";
            public const string AmmoHud = "ammo-hud-controller";
            public const string TabInventory = "tab-menu-controller";
            public const string EscMenu = "esc-menu-controller";
            public const string ChestInventory = "chest-inventory-controller";
            public const string Trade = "trade-menu-controller";
            public const string ReloadingWorkbench = "reloading-menu-controller";
            public const string InteractionHint = "interaction-hint-controller";
            public const string DeviceTargetSelection = "player-device-target-selection-controller";
        }

        public static class IntentKeys
        {
            public const string BeltSlotSelect = "belt.slot.select";
            public const string InventoryDragMerge = "inventory.drag.merge";
            public const string InventoryDragSwap = "inventory.drag.swap";
            public const string InventoryDragDrop = "inventory.drag.drop";
            public const string EscMenuResume = "esc.menu.resume";
            public const string EscMenuSettings = "esc.menu.settings";
            public const string EscMenuKeybindings = "esc.menu.keybindings";
            public const string EscMenuQuit = "esc.menu.quit";
            public const string TradeConfirmBuy = "trade.confirm.buy";
            public const string TradeConfirmSell = "trade.confirm.sell";
            public const string ReloadingOperationSelect = "reloading.operation.select";
            public const string ReloadingOperationExecute = "reloading.operation.execute";
        }
    }
}
