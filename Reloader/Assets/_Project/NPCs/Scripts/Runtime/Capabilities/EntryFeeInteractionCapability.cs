using System;
using UnityEngine;

namespace Reloader.NPCs.Runtime.Capabilities
{
    public sealed class EntryFeeInteractionCapability : MonoBehaviour, INpcCapability, INpcActionProvider, INpcActionExecutor
    {
        public const string ActionKey = "npc.entry-fee.interact";

        [SerializeField] private string _displayName = "Pay Entry Fee";
        [SerializeField] private int _priority = 15;
        [SerializeField] private int _defaultEntryFeeAmount = 25;

        private bool _hasPaidEntryFee;
        private int _lastPaidAmount;

        public NpcCapabilityKind CapabilityKind => NpcCapabilityKind.EntryFeeInteraction;
        public bool HasPaidEntryFee => _hasPaidEntryFee;
        public int LastPaidAmount => _lastPaidAmount;

        public event Action<NpcActionExecutionResult> EntryFeeProcessed;

        public void Initialize(NpcAgent agent)
        {
        }

        public void Shutdown()
        {
        }

        public NpcActionDefinition[] GetActions()
        {
            var amount = Mathf.Max(1, _defaultEntryFeeAmount);
            return new[]
            {
                new NpcActionDefinition(ActionKey, _displayName, _priority, "amount:" + amount)
            };
        }

        public bool CanExecuteAction(string actionKey)
        {
            return string.Equals(actionKey, ActionKey, StringComparison.Ordinal);
        }

        public bool TryExecuteAction(in NpcActionExecutionContext context, out NpcActionExecutionResult result)
        {
            if (!CanExecuteAction(context.ActionKey))
            {
                result = new NpcActionExecutionResult(context.ActionKey, false, "entry-fee.invalid-action");
                return false;
            }

            if (!TryParseAmount(context.Payload, out var amount) || amount <= 0)
            {
                result = new NpcActionExecutionResult(ActionKey, false, "entry-fee.invalid-amount", context.Payload);
                return false;
            }

            _hasPaidEntryFee = true;
            _lastPaidAmount = amount;

            var payload = "entry-fee.paid:" + amount;
            result = new NpcActionExecutionResult(ActionKey, true, "entry-fee.paid", payload);
            EntryFeeProcessed?.Invoke(result);
            return true;
        }

        private static bool TryParseAmount(string payload, out int amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            var normalized = payload.Trim();
            const string amountPrefix = "amount:";
            if (normalized.StartsWith(amountPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(amountPrefix.Length);
            }

            return int.TryParse(normalized, out amount);
        }
    }
}
