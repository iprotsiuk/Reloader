using System;
using System.Collections.Generic;

namespace Reloader.Core.Items
{
    public sealed class ItemRegistry
    {
        private readonly Dictionary<string, ItemInstance> _instances = new Dictionary<string, ItemInstance>();

        public ItemInstance Create(
            string definitionId,
            int quantity = 1,
            float? durability01 = null,
            int conditionFlags = 0,
            string runtimeStateJson = "{}",
            long createdAtTick = 0)
        {
            var instanceId = Guid.NewGuid().ToString("N");
            var instance = new ItemInstance(
                instanceId,
                definitionId,
                quantity,
                durability01,
                conditionFlags,
                runtimeStateJson,
                createdAtTick,
                createdAtTick);

            _instances[instanceId] = instance;
            return instance;
        }

        public bool TryAdd(ItemInstance instance)
        {
            if (instance == null || string.IsNullOrWhiteSpace(instance.InstanceId))
            {
                return false;
            }

            if (_instances.ContainsKey(instance.InstanceId))
            {
                return false;
            }

            _instances.Add(instance.InstanceId, instance);
            return true;
        }

        public bool TryGet(string instanceId, out ItemInstance instance)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                instance = null;
                return false;
            }

            return _instances.TryGetValue(instanceId, out instance);
        }

        public bool Remove(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            return _instances.Remove(instanceId);
        }
    }
}
