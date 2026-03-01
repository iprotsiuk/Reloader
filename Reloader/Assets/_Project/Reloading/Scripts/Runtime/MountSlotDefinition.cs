using System;
using System.Collections.Generic;

namespace Reloader.Reloading.Runtime
{
    [Serializable]
    public sealed class MountSlotDefinition
    {
        [UnityEngine.SerializeField] private string _slotId;
        [UnityEngine.SerializeField] private List<string> _requiredTags = new List<string>();
        [UnityEngine.SerializeField] private List<string> _forbiddenTags = new List<string>();
        [UnityEngine.SerializeField] private CompatibilityRuleSet _ruleSet = new CompatibilityRuleSet();

        public MountSlotDefinition()
        {
        }

        public MountSlotDefinition(
            string slotId,
            IEnumerable<string> requiredTags = null,
            IEnumerable<string> forbiddenTags = null,
            CompatibilityRuleSet ruleSet = null)
        {
            _slotId = slotId;
            _requiredTags = ToList(requiredTags);
            _forbiddenTags = ToList(forbiddenTags);
            _ruleSet = ruleSet ?? new CompatibilityRuleSet();
        }

        public string SlotId => _slotId;

        public IReadOnlyList<string> RequiredTags => _requiredTags;

        public IReadOnlyList<string> ForbiddenTags => _forbiddenTags;

        public CompatibilityRuleSet RuleSet => _ruleSet;

        public bool CanAccept(MountableItemDefinition candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var effectiveRules = new CompatibilityRuleSet(_requiredTags, _forbiddenTags);
            return effectiveRules.AreSatisfiedBy(candidate.Tags) && _ruleSet.AreSatisfiedBy(candidate.Tags);
        }

        private static List<string> ToList(IEnumerable<string> source)
        {
            var result = new List<string>();
            if (source == null)
            {
                return result;
            }

            foreach (var value in source)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result.Add(value);
                }
            }

            return result;
        }
    }
}
