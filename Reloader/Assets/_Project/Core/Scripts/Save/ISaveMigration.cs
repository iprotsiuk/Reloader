using System;

namespace Reloader.Core.Save
{
    public interface ISaveMigration
    {
        int FromSchemaVersion { get; }
        int ToSchemaVersion { get; }
        SaveEnvelope Apply(SaveEnvelope envelope);
    }
}
