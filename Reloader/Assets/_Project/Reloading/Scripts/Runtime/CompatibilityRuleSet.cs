using System;
using System.Collections.Generic;

namespace Reloader.Reloading.Runtime
{
    [Serializable]
    public sealed class CompatibilityRuleSet
    {
        [UnityEngine.SerializeField] private List<string> _requiredTags = new List<string>();
        [UnityEngine.SerializeField] private List<string> _forbiddenTags = new List<string>();

        public CompatibilityRuleSet()
        {
        }

        public CompatibilityRuleSet(IEnumerable<string> requiredTags, IEnumerable<string> forbiddenTags)
        {
            _requiredTags = ToList(requiredTags);
            _forbiddenTags = ToList(forbiddenTags);
        }

        public IReadOnlyList<string> RequiredTags => _requiredTags;

        public IReadOnlyList<string> ForbiddenTags => _forbiddenTags;

        public bool AreSatisfiedBy(IEnumerable<string> candidateTags)
        {
            Evaluate(candidateTags, out var missingRequiredTags, out var presentForbiddenTags);
            return missingRequiredTags.Count == 0 && presentForbiddenTags.Count == 0;
        }

        public void Evaluate(
            IEnumerable<string> candidateTags,
            out List<string> missingRequiredTags,
            out List<string> presentForbiddenTags)
        {
            var normalizedTags = new HashSet<string>(candidateTags ?? Array.Empty<string>(), StringComparer.Ordinal);

            missingRequiredTags = new List<string>();
            for (var i = 0; i < _requiredTags.Count; i++)
            {
                var tag = _requiredTags[i];
                if (!string.IsNullOrEmpty(tag) && !normalizedTags.Contains(tag))
                {
                    missingRequiredTags.Add(tag);
                }
            }

            presentForbiddenTags = new List<string>();
            for (var i = 0; i < _forbiddenTags.Count; i++)
            {
                var tag = _forbiddenTags[i];
                if (!string.IsNullOrEmpty(tag) && normalizedTags.Contains(tag))
                {
                    presentForbiddenTags.Add(tag);
                }
            }
        }

        private static List<string> ToList(IEnumerable<string> source)
        {
            if (source == null)
            {
                return new List<string>();
            }

            var result = new List<string>();
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
