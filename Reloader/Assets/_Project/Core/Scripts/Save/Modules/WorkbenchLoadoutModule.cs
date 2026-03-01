using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class WorkbenchLoadoutModule : ISaveDomainModule
    {
        [Serializable]
        public sealed class SlotNodeRecord
        {
            [JsonProperty("slotId")]
            public string SlotId { get; set; } = string.Empty;

            [JsonProperty("mountedItemId")]
            public string MountedItemId { get; set; }

            [JsonProperty("childSlots")]
            public List<SlotNodeRecord> ChildSlots { get; set; } = new List<SlotNodeRecord>();
        }

        [Serializable]
        public sealed class WorkbenchRecord
        {
            [JsonProperty("workbenchId")]
            public string WorkbenchId { get; set; } = string.Empty;

            [JsonProperty("slotNodes")]
            public List<SlotNodeRecord> SlotNodes { get; set; } = new List<SlotNodeRecord>();
        }

        [Serializable]
        private sealed class WorkbenchLoadoutPayload
        {
            [JsonProperty("workbenches")]
            public List<WorkbenchRecord> Workbenches { get; set; } = new List<WorkbenchRecord>();
        }

        public string ModuleKey => "WorkbenchLoadout";
        public int ModuleVersion => 1;

        public List<WorkbenchRecord> Workbenches { get; } = new List<WorkbenchRecord>();

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new WorkbenchLoadoutPayload
            {
                Workbenches = CloneWorkbenches(Workbenches)
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<WorkbenchLoadoutPayload>(payloadJson);

            Workbenches.Clear();
            if (payload?.Workbenches == null)
            {
                return;
            }

            for (var i = 0; i < payload.Workbenches.Count; i++)
            {
                var normalized = NormalizeWorkbench(payload.Workbenches[i]);
                if (normalized == null)
                {
                    continue;
                }

                Workbenches.Add(normalized);
            }
        }

        public void ValidateModuleState()
        {
            if (Workbenches == null)
            {
                throw new InvalidOperationException("WorkbenchLoadout workbenches collection is missing.");
            }

            var seenWorkbenchIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Workbenches.Count; i++)
            {
                var workbench = Workbenches[i];
                if (workbench == null)
                {
                    throw new InvalidOperationException($"WorkbenchLoadout workbench record at index {i} is null.");
                }

                SaveValidation.EnsureRequiredString(workbench.WorkbenchId, $"WorkbenchLoadout workbenchId is missing at index {i}.");
                if (!seenWorkbenchIds.Add(workbench.WorkbenchId))
                {
                    throw new InvalidOperationException($"WorkbenchLoadout contains duplicate workbenchId '{workbench.WorkbenchId}'.");
                }

                ValidateSlotNodes(workbench.SlotNodes, $"WorkbenchLoadout workbench '{workbench.WorkbenchId}'");
            }
        }

        private static WorkbenchRecord NormalizeWorkbench(WorkbenchRecord source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.WorkbenchId))
            {
                return null;
            }

            var normalized = new WorkbenchRecord
            {
                WorkbenchId = source.WorkbenchId,
                SlotNodes = new List<SlotNodeRecord>()
            };

            if (source.SlotNodes != null)
            {
                for (var i = 0; i < source.SlotNodes.Count; i++)
                {
                    var child = NormalizeSlotNode(source.SlotNodes[i]);
                    if (child != null)
                    {
                        normalized.SlotNodes.Add(child);
                    }
                }
            }

            return normalized;
        }

        private static SlotNodeRecord NormalizeSlotNode(SlotNodeRecord source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.SlotId))
            {
                return null;
            }

            var normalized = new SlotNodeRecord
            {
                SlotId = source.SlotId,
                MountedItemId = string.IsNullOrWhiteSpace(source.MountedItemId) ? null : source.MountedItemId,
                ChildSlots = new List<SlotNodeRecord>()
            };

            if (source.ChildSlots != null)
            {
                for (var i = 0; i < source.ChildSlots.Count; i++)
                {
                    var child = NormalizeSlotNode(source.ChildSlots[i]);
                    if (child != null)
                    {
                        normalized.ChildSlots.Add(child);
                    }
                }
            }

            return normalized;
        }

        private static List<WorkbenchRecord> CloneWorkbenches(List<WorkbenchRecord> source)
        {
            var cloned = new List<WorkbenchRecord>();
            if (source == null)
            {
                return cloned;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var normalized = NormalizeWorkbench(source[i]);
                if (normalized != null)
                {
                    cloned.Add(normalized);
                }
            }

            return cloned;
        }

        private static void ValidateSlotNodes(List<SlotNodeRecord> slotNodes, string context)
        {
            if (slotNodes == null)
            {
                throw new InvalidOperationException($"{context} slotNodes collection is missing.");
            }

            for (var i = 0; i < slotNodes.Count; i++)
            {
                ValidateSlotNode(slotNodes[i], $"{context} slotNodes[{i}]");
            }
        }

        private static void ValidateSlotNode(SlotNodeRecord slotNode, string context)
        {
            if (slotNode == null)
            {
                throw new InvalidOperationException($"{context} is null.");
            }

            SaveValidation.EnsureRequiredString(slotNode.SlotId, $"{context} has missing slotId.");
            if (slotNode.MountedItemId != null && string.IsNullOrWhiteSpace(slotNode.MountedItemId))
            {
                throw new InvalidOperationException($"{context} has invalid mountedItemId.");
            }

            if (slotNode.ChildSlots == null)
            {
                throw new InvalidOperationException($"{context} childSlots collection is missing.");
            }

            for (var i = 0; i < slotNode.ChildSlots.Count; i++)
            {
                ValidateSlotNode(slotNode.ChildSlots[i], $"{context}.childSlots[{i}]");
            }
        }
    }
}
