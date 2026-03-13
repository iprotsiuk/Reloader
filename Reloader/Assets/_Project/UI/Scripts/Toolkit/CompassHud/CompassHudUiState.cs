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

        private CompassHudUiState(IEnumerable<EntryState> entries, float visibleHalfAngleDegrees, bool isVisible)
            : base(Runtime.UiRuntimeCompositionIds.ScreenIds.CompassHud)
        {
            _entries = entries == null ? Array.Empty<EntryState>() : new List<EntryState>(entries).ToArray();
            _visibleHalfAngleDegrees = Math.Max(1f, visibleHalfAngleDegrees);
            IsVisible = isVisible;
        }

        public IReadOnlyList<EntryState> Entries => _entries;
        public float VisibleHalfAngleDegrees => _visibleHalfAngleDegrees;
        public bool IsVisible { get; }

        public static CompassHudUiState Create(IEnumerable<EntryState> entries, float visibleHalfAngleDegrees, bool isVisible)
        {
            return new CompassHudUiState(entries, visibleHalfAngleDegrees, isVisible);
        }
    }
}
