using System;
using System.Collections.Generic;

namespace Reloader.Reloading.Runtime
{
    public sealed class WorkbenchCompatibilityEvaluator
    {
        public WorkbenchCompatibilityResult Evaluate(
            MountSlotDefinition slot,
            MountableItemDefinition candidate,
            params Func<MountSlotDefinition, MountableItemDefinition, WorkbenchCompatibilityResult>[] additionalRules)
        {
            if (slot == null)
            {
                return WorkbenchCompatibilityResult.Incompatible(
                    missingRequiredTags: null,
                    presentForbiddenTags: null,
                    diagnosticCodes: new[] { "slot.null" });
            }

            if (candidate == null)
            {
                return WorkbenchCompatibilityResult.Incompatible(
                    missingRequiredTags: null,
                    presentForbiddenTags: null,
                    diagnosticCodes: new[] { "candidate.null" });
            }

            var missingRequiredTags = new List<string>();
            var presentForbiddenTags = new List<string>();

            EvaluateTagSet(slot.RequiredTags, slot.ForbiddenTags, candidate.Tags, missingRequiredTags, presentForbiddenTags);
            slot.RuleSet.Evaluate(candidate.Tags, out var ruleMissing, out var ruleForbidden);
            missingRequiredTags.AddRange(ruleMissing);
            presentForbiddenTags.AddRange(ruleForbidden);

            var diagnostics = new List<string>();
            if (missingRequiredTags.Count > 0)
            {
                diagnostics.Add("tags.missing-required");
            }

            if (presentForbiddenTags.Count > 0)
            {
                diagnostics.Add("tags.present-forbidden");
            }

            var baseResult = new WorkbenchCompatibilityResult(
                isCompatible: missingRequiredTags.Count == 0 && presentForbiddenTags.Count == 0,
                missingRequiredTags: missingRequiredTags,
                presentForbiddenTags: presentForbiddenTags,
                diagnosticCodes: diagnostics);

            if (additionalRules == null)
            {
                return baseResult;
            }

            var combined = baseResult;
            for (var i = 0; i < additionalRules.Length; i++)
            {
                var rule = additionalRules[i];
                if (rule == null)
                {
                    continue;
                }

                combined = combined.Merge(rule(slot, candidate));
            }

            return combined;
        }

        private static void EvaluateTagSet(
            IReadOnlyList<string> required,
            IReadOnlyList<string> forbidden,
            IReadOnlyList<string> candidateTags,
            List<string> missingRequiredTags,
            List<string> presentForbiddenTags)
        {
            var normalized = new HashSet<string>(candidateTags ?? Array.Empty<string>(), StringComparer.Ordinal);

            for (var i = 0; i < required.Count; i++)
            {
                var requiredTag = required[i];
                if (!string.IsNullOrWhiteSpace(requiredTag) && !normalized.Contains(requiredTag))
                {
                    missingRequiredTags.Add(requiredTag);
                }
            }

            for (var i = 0; i < forbidden.Count; i++)
            {
                var forbiddenTag = forbidden[i];
                if (!string.IsNullOrWhiteSpace(forbiddenTag) && normalized.Contains(forbiddenTag))
                {
                    presentForbiddenTags.Add(forbiddenTag);
                }
            }
        }
    }
}
