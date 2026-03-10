using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Save
{
    public static class SaveBootstrapper
    {
        /// <summary>
        /// Creates the default save pipeline for the current runtime schema.
        /// Registration order is deterministic: CoreWorld, CivilianPopulation, Inventory, Weapons, WorldObjectState,
        /// ContainerStorage, PlayerDevice, WorkbenchLoadout, ContractState, PoliceHeatState.
        /// </summary>
        public static SaveCoordinator CreateDefaultCoordinator(int currentSchemaVersion = 9)
        {
            return new SaveCoordinator(
                new SaveFileRepository(),
                new[]
                {
                    new SaveModuleRegistration(0, new CoreWorldModule()),
                    new SaveModuleRegistration(1, new CivilianPopulationModule()),
                    new SaveModuleRegistration(2, new InventoryModule()),
                    new SaveModuleRegistration(3, new WeaponsModule()),
                    new SaveModuleRegistration(4, new WorldObjectStateModule()),
                    new SaveModuleRegistration(5, new ContainerStorageModule()),
                    new SaveModuleRegistration(6, new PlayerDeviceModule()),
                    new SaveModuleRegistration(7, new WorkbenchLoadoutModule()),
                    new SaveModuleRegistration(8, new ContractStateModule()),
                    new SaveModuleRegistration(9, new PoliceHeatStateModule())
                },
                currentSchemaVersion);
        }
    }
}
