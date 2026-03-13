using Reloader.DevTools.Data;
using Reloader.Inventory;
using System;
using UnityEngine;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevCommandContext
    {
        public PlayerInventoryController InventoryController { get; set; }
        public MonoBehaviour WeaponController { get; set; }
        public DevNpcSpawnCatalog NpcSpawnCatalog { get; set; }
        public DevNpcSpawnService NpcSpawnService { get; set; }
        public Camera SpawnCamera { get; set; }

        public PlayerInventoryController ResolveInventoryController()
        {
            if (InventoryController != null)
            {
                return InventoryController;
            }

            InventoryController = UnityEngine.Object.FindFirstObjectByType<PlayerInventoryController>(FindObjectsInactive.Include);
            return InventoryController;
        }

        public MonoBehaviour ResolveWeaponController()
        {
            if (WeaponController != null)
            {
                return WeaponController;
            }

            var weaponControllerType = ResolveRuntimeType("Reloader.Weapons.Controllers.PlayerWeaponController");
            if (weaponControllerType == null)
            {
                return null;
            }

            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || !weaponControllerType.IsAssignableFrom(behaviour.GetType()))
                {
                    continue;
                }

                WeaponController = behaviour;
                return WeaponController;
            }

            return null;
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

        private static Type ResolveRuntimeType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            var direct = Type.GetType(fullName);
            if (direct != null)
            {
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var resolved = assemblies[i].GetType(fullName);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }
    }
}
