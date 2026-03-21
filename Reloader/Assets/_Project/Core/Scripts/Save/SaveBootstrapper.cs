using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Save
{
    public static class SaveBootstrapper
    {
        /// <summary>
        /// Creates the default save pipeline for the current runtime schema.
        /// Registration order is deterministic: CoreWorld, CivilianPopulation, Inventory, Weapons, PlayerState,
        /// WorldObjectState, ContainerStorage, PlayerDevice, WorkbenchLoadout, ContractState, PoliceHeatState.
        /// </summary>
        public static SaveCoordinator CreateDefaultCoordinator(int currentSchemaVersion = 10)
        {
            return new SaveCoordinator(
                new SaveFileRepository(),
                new[]
                {
                    new SaveModuleRegistration(0, new CoreWorldModule()),
                    new SaveModuleRegistration(1, new CivilianPopulationModule()),
                    new SaveModuleRegistration(2, new InventoryModule()),
                    new SaveModuleRegistration(3, new WeaponsModule()),
                    new SaveModuleRegistration(4, new PlayerStateModule()),
                    new SaveModuleRegistration(5, new WorldObjectStateModule()),
                    new SaveModuleRegistration(6, new ContainerStorageModule()),
                    new SaveModuleRegistration(7, new PlayerDeviceModule()),
                    new SaveModuleRegistration(8, new WorkbenchLoadoutModule()),
                    new SaveModuleRegistration(9, new ContractStateModule()),
                    new SaveModuleRegistration(10, new PoliceHeatStateModule())
                },
                currentSchemaVersion);
        }
    }
}
