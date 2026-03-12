using Reloader.Inventory;
using UnityEngine;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevCommandContext
    {
        public PlayerInventoryController InventoryController { get; set; }

        public PlayerInventoryController ResolveInventoryController()
        {
            if (InventoryController != null)
            {
                return InventoryController;
            }

            InventoryController = Object.FindFirstObjectByType<PlayerInventoryController>(FindObjectsInactive.Include);
            return InventoryController;
        }
    }
}
