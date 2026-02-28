using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class WorldObjectStateModule : ISaveDomainModule
    {
        [Serializable]
        public sealed class WorldObjectRecord
        {
            [JsonProperty("objectId")]
            public string ObjectId { get; set; } = string.Empty;

            [JsonProperty("consumed")]
            public bool Consumed { get; set; }

            [JsonProperty("destroyed")]
            public bool Destroyed { get; set; }

            [JsonProperty("hasTransformOverride")]
            public bool HasTransformOverride { get; set; }

            [JsonProperty("positionX")]
            public float PositionX { get; set; }

            [JsonProperty("positionY")]
            public float PositionY { get; set; }

            [JsonProperty("positionZ")]
            public float PositionZ { get; set; }

            [JsonProperty("rotationX")]
            public float RotationX { get; set; }

            [JsonProperty("rotationY")]
            public float RotationY { get; set; }

            [JsonProperty("rotationZ")]
            public float RotationZ { get; set; }

            [JsonProperty("rotationW")]
            public float RotationW { get; set; } = 1f;

            [JsonProperty("lastUpdatedDay")]
            public int LastUpdatedDay { get; set; }

            [JsonProperty("itemInstanceId")]
            public string ItemInstanceId { get; set; } = string.Empty;
        }

        [Serializable]
        public sealed class SceneObjectStateRecord
        {
            [JsonProperty("scenePath")]
            public string ScenePath { get; set; } = string.Empty;

            [JsonProperty("records")]
            public List<WorldObjectRecord> Records { get; set; } = new List<WorldObjectRecord>();
        }

        [Serializable]
        public sealed class ReclaimRecord
        {
            [JsonProperty("scenePath")]
            public string ScenePath { get; set; } = string.Empty;

            [JsonProperty("objectId")]
            public string ObjectId { get; set; } = string.Empty;

            [JsonProperty("itemInstanceId")]
            public string ItemInstanceId { get; set; } = string.Empty;

            [JsonProperty("cleanedOnDay")]
            public int CleanedOnDay { get; set; }
        }

        [Serializable]
        private sealed class WorldObjectStatePayload
        {
            [JsonProperty("sceneObjectStates")]
            public List<SceneObjectStateRecord> SceneObjectStates { get; set; } = new List<SceneObjectStateRecord>();

            [JsonProperty("reclaimEntries")]
            public List<ReclaimRecord> ReclaimEntries { get; set; } = new List<ReclaimRecord>();
        }

        public string ModuleKey => "WorldObjectState";
        public int ModuleVersion => 1;

        public List<SceneObjectStateRecord> SceneObjectStates { get; } = new List<SceneObjectStateRecord>();
        public List<ReclaimRecord> ReclaimEntries { get; } = new List<ReclaimRecord>();

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new WorldObjectStatePayload
            {
                SceneObjectStates = CloneSceneStateRecords(SceneObjectStates),
                ReclaimEntries = CloneReclaimRecords(ReclaimEntries)
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<WorldObjectStatePayload>(payloadJson);

            SceneObjectStates.Clear();
            ReclaimEntries.Clear();

            if (payload?.SceneObjectStates != null)
            {
                for (var i = 0; i < payload.SceneObjectStates.Count; i++)
                {
                    var sceneRecord = payload.SceneObjectStates[i];
                    if (sceneRecord == null || string.IsNullOrWhiteSpace(sceneRecord.ScenePath) || sceneRecord.Records == null)
                    {
                        continue;
                    }

                    var normalizedRecords = new List<WorldObjectRecord>();
                    for (var j = 0; j < sceneRecord.Records.Count; j++)
                    {
                        var record = sceneRecord.Records[j];
                        if (record == null || string.IsNullOrWhiteSpace(record.ObjectId))
                        {
                            continue;
                        }

                        normalizedRecords.Add(CloneRecord(record));
                    }

                    SceneObjectStates.Add(new SceneObjectStateRecord
                    {
                        ScenePath = sceneRecord.ScenePath,
                        Records = normalizedRecords
                    });
                }
            }

            if (payload?.ReclaimEntries == null)
            {
                return;
            }

            for (var i = 0; i < payload.ReclaimEntries.Count; i++)
            {
                var reclaimRecord = payload.ReclaimEntries[i];
                if (reclaimRecord == null
                    || string.IsNullOrWhiteSpace(reclaimRecord.ScenePath)
                    || string.IsNullOrWhiteSpace(reclaimRecord.ObjectId)
                    || string.IsNullOrWhiteSpace(reclaimRecord.ItemInstanceId)
                    || reclaimRecord.CleanedOnDay < 0)
                {
                    continue;
                }

                ReclaimEntries.Add(CloneReclaimRecord(reclaimRecord));
            }
        }

        public void ValidateModuleState()
        {
            for (var i = 0; i < SceneObjectStates.Count; i++)
            {
                var sceneRecord = SceneObjectStates[i];
                if (sceneRecord == null)
                {
                    throw new InvalidOperationException($"WorldObjectState scene record at index {i} is null.");
                }

                SaveValidation.EnsureRequiredString(sceneRecord.ScenePath, $"WorldObjectState scenePath is missing at index {i}.");

                if (sceneRecord.Records == null)
                {
                    throw new InvalidOperationException($"WorldObjectState records are missing for scene '{sceneRecord.ScenePath}'.");
                }

                for (var j = 0; j < sceneRecord.Records.Count; j++)
                {
                    var record = sceneRecord.Records[j];
                    if (record == null)
                    {
                        throw new InvalidOperationException($"WorldObjectState record is null at scene '{sceneRecord.ScenePath}', index {j}.");
                    }

                    SaveValidation.EnsureRequiredString(record.ObjectId, $"WorldObjectState objectId is missing at scene '{sceneRecord.ScenePath}', index {j}.");
                    SaveValidation.EnsureNonNegative(record.LastUpdatedDay, $"WorldObjectState lastUpdatedDay is negative for '{record.ObjectId}'.");
                }
            }

            for (var i = 0; i < ReclaimEntries.Count; i++)
            {
                var reclaimRecord = ReclaimEntries[i];
                if (reclaimRecord == null)
                {
                    throw new InvalidOperationException($"WorldObjectState reclaim record at index {i} is null.");
                }

                SaveValidation.EnsureRequiredString(reclaimRecord.ScenePath, $"WorldObjectState reclaim scenePath is missing at index {i}.");
                SaveValidation.EnsureRequiredString(reclaimRecord.ObjectId, $"WorldObjectState reclaim objectId is missing at index {i}.");
                SaveValidation.EnsureRequiredString(reclaimRecord.ItemInstanceId, $"WorldObjectState reclaim itemInstanceId is missing at index {i}.");
                SaveValidation.EnsureNonNegative(reclaimRecord.CleanedOnDay, $"WorldObjectState reclaim cleanedOnDay is negative for '{reclaimRecord.ItemInstanceId}'.");
            }
        }

        private static List<SceneObjectStateRecord> CloneSceneStateRecords(List<SceneObjectStateRecord> source)
        {
            var cloned = new List<SceneObjectStateRecord>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var sceneRecord = source[i];
                if (sceneRecord == null)
                {
                    continue;
                }

                cloned.Add(new SceneObjectStateRecord
                {
                    ScenePath = sceneRecord.ScenePath,
                    Records = CloneRecords(sceneRecord.Records)
                });
            }

            return cloned;
        }

        private static List<WorldObjectRecord> CloneRecords(List<WorldObjectRecord> source)
        {
            var cloned = new List<WorldObjectRecord>();
            if (source == null)
            {
                return cloned;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var record = source[i];
                if (record == null)
                {
                    continue;
                }

                cloned.Add(CloneRecord(record));
            }

            return cloned;
        }

        private static List<ReclaimRecord> CloneReclaimRecords(List<ReclaimRecord> source)
        {
            var cloned = new List<ReclaimRecord>();
            if (source == null)
            {
                return cloned;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var record = source[i];
                if (record == null)
                {
                    continue;
                }

                cloned.Add(CloneReclaimRecord(record));
            }

            return cloned;
        }

        private static WorldObjectRecord CloneRecord(WorldObjectRecord source)
        {
            return new WorldObjectRecord
            {
                ObjectId = source.ObjectId,
                Consumed = source.Consumed,
                Destroyed = source.Destroyed,
                HasTransformOverride = source.HasTransformOverride,
                PositionX = source.PositionX,
                PositionY = source.PositionY,
                PositionZ = source.PositionZ,
                RotationX = source.RotationX,
                RotationY = source.RotationY,
                RotationZ = source.RotationZ,
                RotationW = source.RotationW,
                LastUpdatedDay = source.LastUpdatedDay,
                ItemInstanceId = source.ItemInstanceId
            };
        }

        private static ReclaimRecord CloneReclaimRecord(ReclaimRecord source)
        {
            return new ReclaimRecord
            {
                ScenePath = source.ScenePath,
                ObjectId = source.ObjectId,
                ItemInstanceId = source.ItemInstanceId,
                CleanedOnDay = source.CleanedOnDay
            };
        }
    }
}
