using System;
using System.Collections.Generic;

namespace Reloader.Core.Save
{
    public sealed class SaveLoadTransaction
    {
        private readonly List<SaveModuleRegistration> _moduleRegistrations;
        private readonly Dictionary<string, string> _snapshotByModuleKey;

        private SaveLoadTransaction(
            List<SaveModuleRegistration> moduleRegistrations,
            Dictionary<string, string> snapshotByModuleKey)
        {
            _moduleRegistrations = moduleRegistrations;
            _snapshotByModuleKey = snapshotByModuleKey;
        }

        public static SaveLoadTransaction Capture(IEnumerable<SaveModuleRegistration> moduleRegistrations)
        {
            var registrations = new List<SaveModuleRegistration>(moduleRegistrations ?? throw new ArgumentNullException(nameof(moduleRegistrations)));
            var snapshots = new Dictionary<string, string>(registrations.Count, StringComparer.Ordinal);
            for (var i = 0; i < registrations.Count; i++)
            {
                var registration = registrations[i];
                snapshots[registration.Module.ModuleKey] = registration.Module.CaptureModuleStateJson() ?? "{}";
            }

            return new SaveLoadTransaction(registrations, snapshots);
        }

        public void RollbackOrThrow(Exception restoreException)
        {
            try
            {
                for (var i = 0; i < _moduleRegistrations.Count; i++)
                {
                    var registration = _moduleRegistrations[i];
                    if (!_snapshotByModuleKey.TryGetValue(registration.Module.ModuleKey, out var payload))
                    {
                        continue;
                    }

                    registration.Module.RestoreModuleStateFromJson(payload);
                }
            }
            catch (Exception rollbackException)
            {
                throw new InvalidOperationException(
                    "Save load failed and rollback to pre-load state also failed.",
                    new AggregateException(restoreException, rollbackException));
            }
        }
    }
}
