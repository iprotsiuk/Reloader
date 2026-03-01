using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class PlayerDeviceModule : ISaveDomainModule
    {
        [Serializable]
        public sealed class TargetBindingRecord
        {
            [JsonProperty("targetId")]
            public string TargetId { get; set; } = string.Empty;

            [JsonProperty("displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonProperty("distanceMeters")]
            public float DistanceMeters { get; set; }
        }

        [Serializable]
        public sealed class ShotRecord
        {
            [JsonProperty("targetPlanePointXMeters")]
            public float TargetPlanePointXMeters { get; set; }

            [JsonProperty("targetPlanePointYMeters")]
            public float TargetPlanePointYMeters { get; set; }

            [JsonProperty("distanceMeters")]
            public float DistanceMeters { get; set; }
        }

        [Serializable]
        public sealed class GroupRecord
        {
            [JsonProperty("shots")]
            public List<ShotRecord> Shots { get; set; } = new List<ShotRecord>();
        }

        [Serializable]
        private sealed class PlayerDevicePayload
        {
            [JsonProperty("selectedTarget")]
            public TargetBindingRecord SelectedTarget { get; set; }

            [JsonProperty("activeGroupShots")]
            public List<ShotRecord> ActiveGroupShots { get; set; } = new List<ShotRecord>();

            [JsonProperty("savedGroups")]
            public List<GroupRecord> SavedGroups { get; set; } = new List<GroupRecord>();

            [JsonProperty("notesText")]
            public string NotesText { get; set; } = string.Empty;

            [JsonProperty("installedHooks")]
            public List<int> InstalledHooks { get; set; } = new List<int>();
        }

        public string ModuleKey => "PlayerDevice";
        public int ModuleVersion => 1;

        public TargetBindingRecord SelectedTarget { get; set; }
        public List<ShotRecord> ActiveGroupShots { get; } = new List<ShotRecord>();
        public List<GroupRecord> SavedGroups { get; } = new List<GroupRecord>();
        public string NotesText { get; set; } = string.Empty;
        public List<int> InstalledHooks { get; } = new List<int>();

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new PlayerDevicePayload
            {
                SelectedTarget = CloneTarget(SelectedTarget),
                ActiveGroupShots = CloneShots(ActiveGroupShots),
                SavedGroups = CloneGroups(SavedGroups),
                NotesText = NotesText ?? string.Empty,
                InstalledHooks = new List<int>(InstalledHooks)
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<PlayerDevicePayload>(payloadJson);

            SelectedTarget = null;
            ActiveGroupShots.Clear();
            SavedGroups.Clear();
            NotesText = string.Empty;
            InstalledHooks.Clear();

            if (payload == null)
            {
                return;
            }

            if (payload.SelectedTarget != null && !string.IsNullOrWhiteSpace(payload.SelectedTarget.TargetId))
            {
                SelectedTarget = CloneTarget(payload.SelectedTarget);
            }

            if (payload.ActiveGroupShots != null)
            {
                for (var i = 0; i < payload.ActiveGroupShots.Count; i++)
                {
                    var shot = payload.ActiveGroupShots[i];
                    if (shot == null || shot.DistanceMeters <= 0f)
                    {
                        continue;
                    }

                    ActiveGroupShots.Add(CloneShot(shot));
                }
            }

            if (payload.SavedGroups != null)
            {
                for (var i = 0; i < payload.SavedGroups.Count; i++)
                {
                    var group = payload.SavedGroups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    SavedGroups.Add(new GroupRecord
                    {
                        Shots = CloneShots(group.Shots)
                    });
                }
            }

            NotesText = payload.NotesText ?? string.Empty;

            if (payload.InstalledHooks != null)
            {
                for (var i = 0; i < payload.InstalledHooks.Count; i++)
                {
                    var hook = payload.InstalledHooks[i];
                    if (hook <= 0)
                    {
                        continue;
                    }

                    InstalledHooks.Add(hook);
                }
            }
        }

        public void ValidateModuleState()
        {
            if (SelectedTarget != null)
            {
                SaveValidation.EnsureRequiredString(SelectedTarget.TargetId, "PlayerDevice selected targetId is missing.");
                SaveValidation.Ensure(SelectedTarget.DistanceMeters >= 0f, "PlayerDevice selected target distanceMeters cannot be negative.");
            }

            ValidateShots("PlayerDevice activeGroupShots", ActiveGroupShots);

            for (var i = 0; i < SavedGroups.Count; i++)
            {
                var group = SavedGroups[i];
                if (group == null)
                {
                    throw new InvalidOperationException($"PlayerDevice saved group at index {i} is null.");
                }

                if (group.Shots == null)
                {
                    throw new InvalidOperationException($"PlayerDevice saved group shots are missing at index {i}.");
                }

                ValidateShots($"PlayerDevice savedGroups[{i}].shots", group.Shots);
            }

            if (InstalledHooks == null)
            {
                throw new InvalidOperationException("PlayerDevice installedHooks is missing.");
            }

            for (var i = 0; i < InstalledHooks.Count; i++)
            {
                if (InstalledHooks[i] <= 0)
                {
                    throw new InvalidOperationException($"PlayerDevice installedHooks contains invalid value '{InstalledHooks[i]}' at index {i}.");
                }
            }
        }

        private static void ValidateShots(string context, List<ShotRecord> shots)
        {
            if (shots == null)
            {
                throw new InvalidOperationException($"{context} is missing.");
            }

            for (var i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                if (shot == null)
                {
                    throw new InvalidOperationException($"{context} contains a null shot at index {i}.");
                }

                SaveValidation.Ensure(shot.DistanceMeters > 0f, $"{context} has invalid distanceMeters at index {i}.");
            }
        }

        private static TargetBindingRecord CloneTarget(TargetBindingRecord source)
        {
            if (source == null)
            {
                return null;
            }

            return new TargetBindingRecord
            {
                TargetId = source.TargetId,
                DisplayName = source.DisplayName,
                DistanceMeters = source.DistanceMeters
            };
        }

        private static ShotRecord CloneShot(ShotRecord source)
        {
            return new ShotRecord
            {
                TargetPlanePointXMeters = source.TargetPlanePointXMeters,
                TargetPlanePointYMeters = source.TargetPlanePointYMeters,
                DistanceMeters = source.DistanceMeters
            };
        }

        private static List<ShotRecord> CloneShots(List<ShotRecord> source)
        {
            var cloned = new List<ShotRecord>();
            if (source == null)
            {
                return cloned;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var shot = source[i];
                if (shot == null || shot.DistanceMeters <= 0f)
                {
                    continue;
                }

                cloned.Add(CloneShot(shot));
            }

            return cloned;
        }

        private static List<GroupRecord> CloneGroups(List<GroupRecord> source)
        {
            var cloned = new List<GroupRecord>();
            if (source == null)
            {
                return cloned;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var group = source[i];
                if (group == null)
                {
                    continue;
                }

                cloned.Add(new GroupRecord
                {
                    Shots = CloneShots(group.Shots)
                });
            }

            return cloned;
        }
    }
}
