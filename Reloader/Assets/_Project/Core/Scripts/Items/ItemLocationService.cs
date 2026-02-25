using System.Collections.Generic;

namespace Reloader.Core.Items
{
    public sealed class ItemLocationService
    {
        private readonly Dictionary<string, ItemLocation> _locations = new Dictionary<string, ItemLocation>();

        public bool Move(string instanceId, ItemLocation location)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            _locations[instanceId] = location;
            return true;
        }

        public bool TryGet(string instanceId, out ItemLocation location)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                location = default;
                return false;
            }

            return _locations.TryGetValue(instanceId, out location);
        }

        public bool Remove(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            return _locations.Remove(instanceId);
        }
    }
}
