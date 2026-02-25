using System;

namespace Reloader.Core.Save.Migrations
{
    public sealed class SchemaV1ToV1NoOpMigration : ISaveMigration
    {
        public int FromSchemaVersion => 1;
        public int ToSchemaVersion => 1;

        public SaveEnvelope Apply(SaveEnvelope envelope)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            envelope.SchemaVersion = 1;
            return envelope;
        }
    }
}
