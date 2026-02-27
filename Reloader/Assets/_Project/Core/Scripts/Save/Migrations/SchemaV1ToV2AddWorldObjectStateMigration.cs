using System;
using System.Collections.Generic;

namespace Reloader.Core.Save.Migrations
{
    public sealed class SchemaV1ToV2AddWorldObjectStateMigration : ISaveMigration
    {
        public int FromSchemaVersion => 1;
        public int ToSchemaVersion => 2;

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

            if (!envelope.Modules.TryGetValue("WorldObjectState", out var worldObjectStateBlock) || worldObjectStateBlock == null)
            {
                envelope.Modules["WorldObjectState"] = new ModuleSaveBlock
                {
                    ModuleVersion = 1,
                    PayloadJson = "{}"
                };
            }

            envelope.SchemaVersion = 2;
            return envelope;
        }
    }
}
