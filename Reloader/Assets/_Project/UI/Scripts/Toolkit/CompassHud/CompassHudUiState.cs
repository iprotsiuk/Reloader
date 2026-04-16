using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.CompassHud
{
    public sealed class CompassHudUiState : UiRenderState
    {
        public enum EntryKind
        {
            Cardinal,
            Marker
        }

        public readonly struct EntryState
        {
            public EntryState(string key, EntryKind kind, string label, float signedAngleDeltaDegrees, bool isVisible = true)
            {
                Key = key ?? string.Empty;
                Kind = kind;
                Label = label ?? string.Empty;
                SignedAngleDeltaDegrees = signedAngleDeltaDegrees;
                IsVisible = isVisible;
            }

            public string Key { get; }
            public EntryKind Kind { get; }
            public string Label { get; }
            public float SignedAngleDeltaDegrees { get; }
            public bool IsVisible { get; }
        }

        private readonly EntryState[] _entries;
        private readonly float _visibleHalfAngleDegrees;
        private readonly string _policeStatusText;
        private readonly int _policeResponderCount;
        private readonly int _policeWantedLevel;
        private readonly bool _isPoliceStatusVisible;

        private CompassHudUiState(
            IEnumerable<EntryState> entries,
            float visibleHalfAngleDegrees,
            bool isVisible,
            string policeStatusText,
            int policeResponderCount,
            int policeWantedLevel,
            bool isPoliceStatusVisible)
            : base(Runtime.UiRuntimeCompositionIds.ScreenIds.CompassHud)
        {
            _entries = entries == null ? Array.Empty<EntryState>() : new List<EntryState>(entries).ToArray();
            _visibleHalfAngleDegrees = Math.Max(1f, visibleHalfAngleDegrees);
            _policeStatusText = string.IsNullOrWhiteSpace(policeStatusText) ? string.Empty : policeStatusText.Trim();
            _policeResponderCount = Math.Max(0, policeResponderCount);
            _policeWantedLevel = Math.Max(0, policeWantedLevel);
            _isPoliceStatusVisible = isPoliceStatusVisible && !string.IsNullOrWhiteSpace(_policeStatusText);
            IsVisible = isVisible;
        }

        public IReadOnlyList<EntryState> Entries => _entries;
        public float VisibleHalfAngleDegrees => _visibleHalfAngleDegrees;
        public bool IsVisible { get; }
        public string PoliceStatusText => _policeStatusText;
        public int PoliceResponderCount => _policeResponderCount;
        public int PoliceWantedLevel => _policeWantedLevel;
        public bool IsPoliceStatusVisible => _isPoliceStatusVisible;

        public static CompassHudUiState Create(
            IEnumerable<EntryState> entries,
            float visibleHalfAngleDegrees,
            bool isVisible,
            string policeStatusText = "",
            int policeResponderCount = 0,
            int policeWantedLevel = 0,
            bool isPoliceStatusVisible = false)
        {
            return new CompassHudUiState(
                entries,
                visibleHalfAngleDegrees,
                isVisible,
                policeStatusText,
                policeResponderCount,
                policeWantedLevel,
                isPoliceStatusVisible);
        }
    }
}
