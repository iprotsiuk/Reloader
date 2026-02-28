using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Reloader.Core.Save.Modules
{
    public sealed class ContainerStorageModule : ISaveDomainModule
    {
        [Serializable]
        public sealed class ContainerRecord
        {
            [JsonProperty("containerId")]
            public string ContainerId { get; set; } = string.Empty;

            [JsonProperty("policy")]
            public string Policy { get; set; } = string.Empty;

            [JsonProperty("slotItemIds")]
            public List<string> SlotItemIds { get; set; } = new List<string>();
        }

        [Serializable]
        private sealed class ContainerStoragePayload
        {
            [JsonProperty("containers")]
            public List<ContainerRecord> Containers { get; set; } = new List<ContainerRecord>();
        }

        public string ModuleKey => "ContainerStorage";
        public int ModuleVersion => 1;

        public List<ContainerRecord> Containers { get; } = new List<ContainerRecord>();

        public string CaptureModuleStateJson()
        {
            return JsonConvert.SerializeObject(new ContainerStoragePayload
            {
                Containers = CloneContainerRecords(Containers)
            });
        }

        public void RestoreModuleStateFromJson(string payloadJson)
        {
            var payload = JsonConvert.DeserializeObject<ContainerStoragePayload>(payloadJson);

            Containers.Clear();
            if (payload?.Containers == null)
            {
                return;
            }

            for (var i = 0; i < payload.Containers.Count; i++)
            {
                var container = payload.Containers[i];
                if (container == null
                    || string.IsNullOrWhiteSpace(container.ContainerId)
                    || string.IsNullOrWhiteSpace(container.Policy))
                {
                    continue;
                }

                Containers.Add(new ContainerRecord
                {
                    ContainerId = container.ContainerId,
                    Policy = container.Policy,
                    SlotItemIds = container.SlotItemIds != null
                        ? new List<string>(container.SlotItemIds)
                        : new List<string>()
                });
            }
        }

        public void ValidateModuleState()
        {
            for (var i = 0; i < Containers.Count; i++)
            {
                var container = Containers[i];
                if (container == null)
                {
                    throw new InvalidOperationException($"ContainerStorage record at index {i} is null.");
                }

                SaveValidation.EnsureRequiredString(container.ContainerId, $"ContainerStorage containerId is missing at index {i}.");
                SaveValidation.EnsureRequiredString(container.Policy, $"ContainerStorage policy is missing for container '{container.ContainerId}'.");

                if (container.SlotItemIds == null)
                {
                    throw new InvalidOperationException($"ContainerStorage slotItemIds is missing for container '{container.ContainerId}'.");
                }

                for (var j = 0; j < container.SlotItemIds.Count; j++)
                {
                    var itemId = container.SlotItemIds[j];
                    if (itemId != null && string.IsNullOrWhiteSpace(itemId))
                    {
                        throw new InvalidOperationException(
                            $"ContainerStorage slot item ID is invalid for container '{container.ContainerId}', slot {j}.");
                    }
                }
            }
        }

        private static List<ContainerRecord> CloneContainerRecords(List<ContainerRecord> source)
        {
            var cloned = new List<ContainerRecord>();
            if (source == null)
            {
                return cloned;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var container = source[i];
                if (container == null)
                {
                    continue;
                }

                cloned.Add(new ContainerRecord
                {
                    ContainerId = container.ContainerId,
                    Policy = container.Policy,
                    SlotItemIds = container.SlotItemIds != null
                        ? new List<string>(container.SlotItemIds)
                        : new List<string>()
                });
            }

            return cloned;
        }
    }
}
