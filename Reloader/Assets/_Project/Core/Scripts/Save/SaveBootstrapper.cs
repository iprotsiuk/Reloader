using Reloader.Core.Save.IO;
using Reloader.Core.Save.Migrations;
using Reloader.Core.Save.Modules;

namespace Reloader.Core.Save
{
    public static class SaveBootstrapper
    {
        /// <summary>
        /// Creates the default save pipeline for v0.x.
        /// Registration order is deterministic: CoreWorld first, Inventory second.
        /// </summary>
        public static SaveCoordinator CreateDefaultCoordinator(int currentSchemaVersion = 1)
        {
            return new SaveCoordinator(
                new SaveFileRepository(),
                new MigrationRunner(new ISaveMigration[]
                {
                    new SchemaV1ToV1NoOpMigration()
                }),
                new[]
                {
                    new SaveModuleRegistration(0, new CoreWorldModule()),
                    new SaveModuleRegistration(1, new InventoryModule()),
                    new SaveModuleRegistration(2, new WeaponsModule())
                },
                currentSchemaVersion);
        }
    }
}
