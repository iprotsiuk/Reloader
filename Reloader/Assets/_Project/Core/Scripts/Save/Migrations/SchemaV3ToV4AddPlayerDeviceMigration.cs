using System;
using System.Collections.Generic;

namespace Reloader.Core.Save.Migrations
{
    public sealed class SchemaV3ToV4AddPlayerDeviceMigration : ISaveMigration
    {
        public int FromSchemaVersion => 3;
        public int ToSchemaVersion => 4;

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

            if (!envelope.Modules.TryGetValue("PlayerDevice", out var playerDeviceBlock) || playerDeviceBlock == null)
            {
                envelope.Modules["PlayerDevice"] = new ModuleSaveBlock
                {
                    ModuleVersion = 1,
                    PayloadJson = "{}"
                };
            }

            envelope.SchemaVersion = 4;
            return envelope;
        }
    }
}
