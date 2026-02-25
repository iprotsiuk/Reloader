using System;
using System.Collections.Generic;
using System.Linq;

namespace Reloader.Core.Save
{
    public sealed class MigrationRunner
    {
        private readonly List<ISaveMigration> _migrations;

        public MigrationRunner(IEnumerable<ISaveMigration> migrations)
        {
            if (migrations == null)
            {
                throw new ArgumentNullException(nameof(migrations));
            }

            _migrations = migrations.ToList();
        }

        public void ValidateConfiguration()
        {
            var duplicatePairs = _migrations
                .GroupBy(x => (x.FromSchemaVersion, x.ToSchemaVersion))
                .Where(group => group.Count() > 1)
                .Select(group => $"{group.Key.FromSchemaVersion}->{group.Key.ToSchemaVersion}")
                .ToArray();

            if (duplicatePairs.Length > 0)
            {
                throw new InvalidOperationException(
                    "Duplicate save migrations configured: " + string.Join(", ", duplicatePairs));
            }

            var hasBaselineNoOp = _migrations.Any(x => x.FromSchemaVersion == 1 && x.ToSchemaVersion == 1);
            if (!hasBaselineNoOp)
            {
                throw new InvalidOperationException(
                    "Missing baseline migration: SchemaV1ToV1NoOpMigration (1->1) is required.");
            }
        }

        public SaveEnvelope MigrateTo(SaveEnvelope envelope, int targetSchemaVersion)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (targetSchemaVersion < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetSchemaVersion), "Target schema version must be >= 1.");
            }

            if (envelope.SchemaVersion > targetSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Save schema {envelope.SchemaVersion} is newer than runtime schema {targetSchemaVersion}.");
            }

            if (envelope.SchemaVersion == targetSchemaVersion)
            {
                return envelope;
            }

            var currentEnvelope = envelope;
            while (currentEnvelope.SchemaVersion < targetSchemaVersion)
            {
                var fromVersion = currentEnvelope.SchemaVersion;
                var nextMigration = _migrations.SingleOrDefault(x =>
                    x.FromSchemaVersion == fromVersion && x.ToSchemaVersion == fromVersion + 1);

                if (nextMigration == null)
                {
                    throw new InvalidOperationException(
                        $"Missing migration step from schema {fromVersion} to {fromVersion + 1}.");
                }

                currentEnvelope = nextMigration.Apply(currentEnvelope);

                if (currentEnvelope.SchemaVersion != fromVersion + 1)
                {
                    throw new InvalidOperationException(
                        $"Migration {nextMigration.GetType().Name} did not set schema to {fromVersion + 1}.");
                }
            }

            return currentEnvelope;
        }
    }
}
