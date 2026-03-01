using System;
using System.Collections.Generic;

namespace Reloader.Reloading.Runtime
{
    public sealed class ReloadingOperationGate
    {
        private const string MissingCapabilitiesCode = "gate.missing-capabilities";

        private static readonly Dictionary<ReloadingOperationType, string[]> DefaultRequirements =
            new Dictionary<ReloadingOperationType, string[]>
            {
                { ReloadingOperationType.ResizeCase, new[] { "cap.press", "cap.die.resize" } },
                { ReloadingOperationType.PrimeCase, new[] { "cap.prime" } },
                { ReloadingOperationType.ChargePowder, new[] { "cap.powder" } },
                { ReloadingOperationType.SeatBullet, new[] { "cap.press", "cap.die.seat" } }
            };

        private readonly WorkbenchLoadoutController _loadoutController;
        private readonly IReadOnlyDictionary<ReloadingOperationType, string[]> _requirements;

        public ReloadingOperationGate(
            WorkbenchLoadoutController loadoutController,
            IReadOnlyDictionary<ReloadingOperationType, string[]> requirements = null)
        {
            _loadoutController = loadoutController;
            _requirements = requirements ?? DefaultRequirements;
        }

        public bool IsOperationAllowed(ReloadingOperationType operation, out OperationGateStatus status)
        {
            if (_loadoutController == null)
            {
                status = OperationGateStatus.Allowed();
                return true;
            }

            if (!_requirements.TryGetValue(operation, out var requiredCapabilities) || requiredCapabilities == null || requiredCapabilities.Length == 0)
            {
                status = OperationGateStatus.Allowed();
                return true;
            }

            var availableCapabilities = _loadoutController.BuildCapabilitySet();
            var availableSet = new HashSet<string>(availableCapabilities, StringComparer.Ordinal);
            var missing = new List<string>();

            for (var i = 0; i < requiredCapabilities.Length; i++)
            {
                var required = requiredCapabilities[i];
                if (!string.IsNullOrWhiteSpace(required) && !availableSet.Contains(required))
                {
                    missing.Add(required);
                }
            }

            if (missing.Count == 0)
            {
                status = OperationGateStatus.Allowed();
                return true;
            }

            status = OperationGateStatus.Blocked(MissingCapabilitiesCode, missing);
            return false;
        }

        public readonly struct OperationGateStatus
        {
            public OperationGateStatus(bool isAllowed, string diagnosticCode, IReadOnlyList<string> missingCapabilities)
            {
                IsAllowed = isAllowed;
                DiagnosticCode = diagnosticCode;
                MissingCapabilities = missingCapabilities ?? Array.Empty<string>();
            }

            public bool IsAllowed { get; }

            public string DiagnosticCode { get; }

            public IReadOnlyList<string> MissingCapabilities { get; }

            public static OperationGateStatus Allowed()
            {
                return new OperationGateStatus(true, null, Array.Empty<string>());
            }

            public static OperationGateStatus Blocked(string diagnosticCode, IReadOnlyList<string> missingCapabilities)
            {
                return new OperationGateStatus(false, diagnosticCode, missingCapabilities ?? Array.Empty<string>());
            }
        }
    }
}
