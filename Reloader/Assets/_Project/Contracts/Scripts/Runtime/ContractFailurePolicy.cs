using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Reloader.Contracts.Runtime
{
    [Serializable]
    public sealed class ContractFailurePolicy
    {
        [SerializeField] private List<AssassinationContractFailureRule> _failureRules = new();

        public IReadOnlyList<AssassinationContractFailureRule> FailureRules => _failureRules;

        public bool ContainsRule(AssassinationContractFailureRuleType ruleType)
        {
            for (var i = 0; i < _failureRules.Count; i++)
            {
                var rule = _failureRules[i];
                if (rule != null && rule.RuleType == ruleType)
                {
                    return true;
                }
            }

            return false;
        }

        public string BuildRestrictionsText()
        {
            var builder = new StringBuilder();
            AppendRestrictionText(builder, AssassinationContractFailureRuleType.WrongTargetKill, "Wrong target fails contract");
            return builder.Length == 0 ? string.Empty : builder.ToString();
        }

        public string BuildFailureConditionsText()
        {
            var builder = new StringBuilder();
            AppendFailureText(builder, AssassinationContractFailureRuleType.WrongTargetKill, "Wrong target kill");
            return builder.Length == 0 ? string.Empty : builder.ToString();
        }

        private void AppendRestrictionText(StringBuilder builder, AssassinationContractFailureRuleType ruleType, string text)
        {
            if (!ContainsRule(ruleType))
            {
                return;
            }

            AppendSegment(builder, text);
        }

        private void AppendFailureText(StringBuilder builder, AssassinationContractFailureRuleType ruleType, string text)
        {
            if (!ContainsRule(ruleType))
            {
                return;
            }

            AppendSegment(builder, text);
        }

        private static void AppendSegment(StringBuilder builder, string text)
        {
            if (builder.Length > 0)
            {
                builder.Append(" • ");
            }

            builder.Append(text);
        }
    }
}
