using System;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class VendorTradeCapability : MonoBehaviour, INpcCapability, INpcActionProvider
    {
        public const string ActionKey = "vendor.trade.open";

        [SerializeField] private MonoBehaviour _vendorTargetBehaviour;
        [SerializeField] private string _displayName = "Trade";
        [SerializeField] private int _priority = 10;

        private NpcAgent _agent;
        private IShopVendorTarget _vendorTarget;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.VendorTrade;

        public void Initialize(NpcAgent agent)
        {
            _agent = agent;
            ResolveVendorTarget();
        }

        public void Shutdown()
        {
            _agent = null;
            _vendorTarget = null;
        }

        public NpcActionDefinition[] GetActions()
        {
            if (!TryResolveVendorId(out var vendorId))
            {
                return Array.Empty<NpcActionDefinition>();
            }

            return new[]
            {
                new NpcActionDefinition(ActionKey, _displayName, _priority, vendorId)
            };
        }

        private bool TryResolveVendorId(out string vendorId)
        {
            ResolveVendorTarget();
            vendorId = _vendorTarget?.VendorId ?? string.Empty;
            return !string.IsNullOrWhiteSpace(vendorId);
        }

        private void ResolveVendorTarget()
        {
            _vendorTarget ??= _vendorTargetBehaviour as IShopVendorTarget;
            if (_vendorTarget != null)
            {
                return;
            }

            _vendorTarget = FindInterface(GetComponents<MonoBehaviour>())
                ?? (_agent != null ? FindInterface(_agent.GetComponents<MonoBehaviour>()) : null)
                ?? FindInterface(GetComponentsInParent<MonoBehaviour>(true))
                ?? FindInterface(GetComponentsInChildren<MonoBehaviour>(true));
        }

        private static IShopVendorTarget FindInterface(MonoBehaviour[] behaviours)
        {
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IShopVendorTarget vendorTarget)
                {
                    return vendorTarget;
                }
            }

            return null;
        }
    }
}
