using System.Collections.Generic;

namespace Reloader.Reloading.Runtime
{
    public sealed class WorkbenchCompatibilityResult
    {
        public WorkbenchCompatibilityResult(
            bool isCompatible,
            IEnumerable<string> missingRequiredTags,
            IEnumerable<string> presentForbiddenTags,
            IEnumerable<string> diagnosticCodes)
        {
            IsCompatible = isCompatible;
            MissingRequiredTags = ToList(missingRequiredTags);
            PresentForbiddenTags = ToList(presentForbiddenTags);
            DiagnosticCodes = ToList(diagnosticCodes);
        }

        public bool IsCompatible { get; }

        public IReadOnlyList<string> MissingRequiredTags { get; }

        public IReadOnlyList<string> PresentForbiddenTags { get; }

        public IReadOnlyList<string> DiagnosticCodes { get; }

        public static WorkbenchCompatibilityResult Compatible()
        {
            return new WorkbenchCompatibilityResult(true, null, null, null);
        }

        public static WorkbenchCompatibilityResult Incompatible(
            IEnumerable<string> missingRequiredTags,
            IEnumerable<string> presentForbiddenTags,
            IEnumerable<string> diagnosticCodes)
        {
            return new WorkbenchCompatibilityResult(false, missingRequiredTags, presentForbiddenTags, diagnosticCodes);
        }

        public WorkbenchCompatibilityResult Merge(WorkbenchCompatibilityResult other)
        {
            if (other == null)
            {
                return this;
            }

            var missing = new List<string>(MissingRequiredTags);
            missing.AddRange(other.MissingRequiredTags);

            var forbidden = new List<string>(PresentForbiddenTags);
            forbidden.AddRange(other.PresentForbiddenTags);

            var diagnostics = new List<string>(DiagnosticCodes);
            diagnostics.AddRange(other.DiagnosticCodes);

            return new WorkbenchCompatibilityResult(
                IsCompatible && other.IsCompatible && missing.Count == 0 && forbidden.Count == 0,
                missing,
                forbidden,
                diagnostics);
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
