using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Reloader.Contracts.Runtime
{
    [Serializable]
    public sealed class ContractObjectivePolicy
    {
        [SerializeField] private List<AssassinationContractObjectiveRule> _objectiveRules = new();

        public IReadOnlyList<AssassinationContractObjectiveRule> ObjectiveRules => _objectiveRules;

        public string BuildRestrictionsText()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < _objectiveRules.Count; i++)
            {
                var rule = _objectiveRules[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.DisplayText))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(" • ");
                }

                builder.Append(rule.DisplayText);
            }

            return builder.ToString();
        }
    }
}
