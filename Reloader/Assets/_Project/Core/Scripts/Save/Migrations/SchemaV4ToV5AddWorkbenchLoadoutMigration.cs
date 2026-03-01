using System;
using System.Collections.Generic;

namespace Reloader.Core.Save.Migrations
{
    public sealed class SchemaV4ToV5AddWorkbenchLoadoutMigration : ISaveMigration
    {
        public int FromSchemaVersion => 4;
        public int ToSchemaVersion => 5;

        public SaveEnvelope Apply(SaveEnvelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (envelope.Modules == null)
            {
                envelope.Modules = new Dictionary<string, ModuleSaveBlock>();
            }

            if (!envelope.Modules.TryGetValue("WorkbenchLoadout", out var workbenchBlock) || workbenchBlock == null)
            {
                envelope.Modules["WorkbenchLoadout"] = new ModuleSaveBlock
                {
                    ModuleVersion = 1,
                    PayloadJson = "{}"
                };
            }

            envelope.SchemaVersion = 5;
            return envelope;
        }
    }
}
