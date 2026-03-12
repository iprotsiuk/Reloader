using Reloader.DevTools.Data;
using Reloader.Inventory;
using UnityEngine;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevCommandContext
    {
        public PlayerInventoryController InventoryController { get; set; }
        public DevNpcSpawnCatalog NpcSpawnCatalog { get; set; }
        public DevNpcSpawnService NpcSpawnService { get; set; }
        public Camera SpawnCamera { get; set; }

        public PlayerInventoryController ResolveInventoryController()
        {
            if (InventoryController != null)
            {
                return InventoryController;
            }

            InventoryController = Object.FindFirstObjectByType<PlayerInventoryController>(FindObjectsInactive.Include);
            return InventoryController;
        }

        public DevNpcSpawnCatalog ResolveNpcSpawnCatalog()
        {
            if (NpcSpawnCatalog != null)
            {
                return NpcSpawnCatalog;
            }

            NpcSpawnCatalog = Resources.Load<DevNpcSpawnCatalog>("DevNpcSpawnCatalog");
            if (NpcSpawnCatalog != null)
            {
                return NpcSpawnCatalog;
            }

            var loadedCatalogs = Resources.FindObjectsOfTypeAll<DevNpcSpawnCatalog>();
            for (var i = 0; i < loadedCatalogs.Length; i++)
            {
                if (loadedCatalogs[i] == null)
                {
                    continue;
                }

                NpcSpawnCatalog = loadedCatalogs[i];
                return NpcSpawnCatalog;
            }

            return null;
        }

        public DevNpcSpawnService ResolveNpcSpawnService()
        {
            if (NpcSpawnService == null)
            {
                NpcSpawnService = new DevNpcSpawnService(ResolveNpcSpawnCatalog(), SpawnCamera);
            }
            else if (SpawnCamera != null)
            {
                NpcSpawnService.SetCameraForTests(SpawnCamera);
            }

            return NpcSpawnService;
        }
    }
}
