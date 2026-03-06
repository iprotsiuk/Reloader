using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Save
{
    public static class SaveBootstrapper
    {
        /// <summary>
        /// Creates the default save pipeline for the current runtime schema.
        /// Registration order is deterministic: CoreWorld, Inventory, Weapons, WorldObjectState, ContainerStorage, PlayerDevice, WorkbenchLoadout, ContractState, PoliceHeatState.
        /// </summary>
        public static SaveCoordinator CreateDefaultCoordinator(int currentSchemaVersion = 6)
        {
            return new SaveCoordinator(
                new SaveFileRepository(),
                new[]
                {
                    new SaveModuleRegistration(0, new CoreWorldModule()),
                    new SaveModuleRegistration(1, new InventoryModule()),
                    new SaveModuleRegistration(2, new WeaponsModule()),
                    new SaveModuleRegistration(3, new WorldObjectStateModule()),
                    new SaveModuleRegistration(4, new ContainerStorageModule()),
                    new SaveModuleRegistration(5, new PlayerDeviceModule()),
                    new SaveModuleRegistration(6, new WorkbenchLoadoutModule()),
                    new SaveModuleRegistration(7, new ContractStateModule()),
                    new SaveModuleRegistration(8, new PoliceHeatStateModule())
                },
                currentSchemaVersion);
        }
    }
}
