using System;
using System.Collections.Generic;

namespace Reloader.Core.Save.Migrations
{
    public sealed class SchemaV2ToV3AddContainerStorageMigration : ISaveMigration
    {
        public int FromSchemaVersion => 2;
        public int ToSchemaVersion => 3;

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

            if (!envelope.Modules.TryGetValue("ContainerStorage", out var containerStorageBlock) || containerStorageBlock == null)
            {
                envelope.Modules["ContainerStorage"] = new ModuleSaveBlock
                {
                    ModuleVersion = 1,
                    PayloadJson = "{}"
                };
            }

            envelope.SchemaVersion = 3;
            return envelope;
        }
    }
}
