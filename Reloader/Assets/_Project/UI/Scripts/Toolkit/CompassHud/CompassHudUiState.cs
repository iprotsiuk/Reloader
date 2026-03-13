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
            public EntryState(EntryKind kind, string label, float signedAngleDeltaDegrees, bool isVisible = true)
            {
                Kind = kind;
                Label = label ?? string.Empty;
                SignedAngleDeltaDegrees = signedAngleDeltaDegrees;
                IsVisible = isVisible;
            }

            public EntryKind Kind { get; }
            public string Label { get; }
            public float SignedAngleDeltaDegrees { get; }
            public bool IsVisible { get; }
        }

        private readonly EntryState[] _entries;

        private CompassHudUiState(IEnumerable<EntryState> entries)
            : base(Runtime.UiRuntimeCompositionIds.ScreenIds.CompassHud)
        {
            _entries = entries == null ? Array.Empty<EntryState>() : new List<EntryState>(entries).ToArray();
        }

        public IReadOnlyList<EntryState> Entries => _entries;

        public static CompassHudUiState Create(IEnumerable<EntryState> entries)
        {
            return new CompassHudUiState(entries);
        }
    }
}
