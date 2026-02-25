using System;
using UnityEngine;

namespace Reloader.Core.Items
{
    [Serializable]
    public sealed class ItemInstance
    {
        public ItemInstance(
            string instanceId,
            string definitionId,
            int quantity,
            float? durability01,
            int conditionFlags,
            string runtimeStateJson,
            long createdAtTick = 0,
            long lastModifiedTick = 0)
        {
            InstanceId = instanceId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            Quantity = Mathf.Max(1, quantity);
            Durability01 = durability01.HasValue ? Mathf.Clamp01(durability01.Value) : null;
            ConditionFlags = conditionFlags;
            RuntimeStateJson = string.IsNullOrWhiteSpace(runtimeStateJson) ? "{}" : runtimeStateJson;
            CreatedAtTick = createdAtTick;
            LastModifiedTick = lastModifiedTick;
        }

        public string InstanceId { get; }
        public string DefinitionId { get; }
        public int Quantity { get; private set; }
        public float? Durability01 { get; private set; }
        public int ConditionFlags { get; private set; }
        public string RuntimeStateJson { get; private set; }
        public long CreatedAtTick { get; private set; }
        public long LastModifiedTick { get; private set; }

        public void UpdateQuantity(int quantity, long modifiedAtTick = 0)
        {
            Quantity = Mathf.Max(1, quantity);
            LastModifiedTick = modifiedAtTick;
        }

        public void UpdateDurability(float? durability01, long modifiedAtTick = 0)
        {
            Durability01 = durability01.HasValue ? Mathf.Clamp01(durability01.Value) : null;
            LastModifiedTick = modifiedAtTick;
        }

        public void UpdateConditionFlags(int conditionFlags, long modifiedAtTick = 0)
        {
            ConditionFlags = conditionFlags;
            LastModifiedTick = modifiedAtTick;
        }

        public void UpdateRuntimeState(string runtimeStateJson, long modifiedAtTick = 0)
        {
            RuntimeStateJson = string.IsNullOrWhiteSpace(runtimeStateJson) ? "{}" : runtimeStateJson;
            LastModifiedTick = modifiedAtTick;
        }
    }
}
