using System;

namespace Reloader.Reloading.Runtime
{
    public sealed class ReloadingWorkbenchUiSnapshot
    {
        public readonly struct OperationGateSnapshot
        {
            public OperationGateSnapshot(string label, bool isEnabled, string diagnostic)
            {
                Label = label ?? string.Empty;
                IsEnabled = isEnabled;
                Diagnostic = diagnostic ?? string.Empty;
            }

            public string Label { get; }

            public bool IsEnabled { get; }

            public string Diagnostic { get; }
        }

        public ReloadingWorkbenchUiSnapshot(string[] setupSlots, OperationGateSnapshot[] operationStatuses)
        {
            SetupSlots = Copy(setupSlots);
            OperationStatuses = Copy(operationStatuses);
        }

        public string[] SetupSlots { get; }

        public OperationGateSnapshot[] OperationStatuses { get; }

        public ReloadingWorkbenchUiSnapshot Clone()
        {
            return new ReloadingWorkbenchUiSnapshot(SetupSlots, OperationStatuses);
        }

        private static string[] Copy(string[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new string[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                copy[i] = source[i] ?? string.Empty;
            }

            return copy;
        }

        private static OperationGateSnapshot[] Copy(OperationGateSnapshot[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<OperationGateSnapshot>();
            }

            var copy = new OperationGateSnapshot[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}
