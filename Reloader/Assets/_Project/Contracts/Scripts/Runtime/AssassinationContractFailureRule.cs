using System;
using UnityEngine;

namespace Reloader.Contracts.Runtime
{
    [Serializable]
    public sealed class AssassinationContractFailureRule
    {
        [SerializeField] private AssassinationContractFailureRuleType _ruleType = AssassinationContractFailureRuleType.WrongTargetKill;

        public AssassinationContractFailureRuleType RuleType => _ruleType;
    }
}
