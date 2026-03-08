using System;
using UnityEngine;

namespace Reloader.Contracts.Runtime
{
    [Serializable]
    public sealed class AssassinationContractObjectiveRule
    {
        [SerializeField] private AssassinationContractObjectiveRuleType _ruleType = AssassinationContractObjectiveRuleType.None;
        [SerializeField] private string _displayText = string.Empty;

        public AssassinationContractObjectiveRuleType RuleType => _ruleType;
        public string DisplayText => _displayText ?? string.Empty;
    }
}
