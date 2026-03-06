using System;
using System.Collections.Generic;

namespace Reloader.Core.Save.Migrations
{
    public sealed class SchemaV5ToV6AddContractAndPoliceHeatStateMigration : ISaveMigration
    {
        public int FromSchemaVersion => 5;
        public int ToSchemaVersion => 6;

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

            if (!envelope.Modules.TryGetValue("ContractState", out var contractBlock) || contractBlock == null)
            {
                envelope.Modules["ContractState"] = new ModuleSaveBlock
                {
                    ModuleVersion = 1,
                    PayloadJson = "{}"
                };
            }

            if (!envelope.Modules.TryGetValue("PoliceHeatState", out var policeHeatBlock) || policeHeatBlock == null)
            {
                envelope.Modules["PoliceHeatState"] = new ModuleSaveBlock
                {
                    ModuleVersion = 1,
                    PayloadJson = "{}"
                };
            }

            envelope.FeatureFlags ??= new SaveFeatureFlags();
            envelope.SchemaVersion = 6;
            return envelope;
        }
    }
}
