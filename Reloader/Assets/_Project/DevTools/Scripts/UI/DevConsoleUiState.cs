using System.Collections.Generic;
using Reloader.DevTools.Runtime;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.Runtime;

namespace Reloader.UI.Toolkit.DevConsole
{
    public sealed class DevConsoleUiState : UiRenderState
    {
        public static readonly DevConsoleUiState Hidden = new(false, ">", string.Empty, string.Empty, System.Array.Empty<DevConsoleSuggestion>(), 0);

        public DevConsoleUiState(
            bool isVisible,
            string promptText,
            string inputText,
            string statusText,
            IReadOnlyList<DevConsoleSuggestion> suggestions,
            int highlightedSuggestionIndex)
            : base(UiRuntimeCompositionIds.ScreenIds.DevConsole)
        {
            IsVisible = isVisible;
            PromptText = promptText ?? ">";
            InputText = inputText ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            Suggestions = suggestions ?? System.Array.Empty<DevConsoleSuggestion>();
            HighlightedSuggestionIndex = highlightedSuggestionIndex;
        }

        public bool IsVisible { get; }
        public string PromptText { get; }
        public string InputText { get; }
        public string StatusText { get; }
        public IReadOnlyList<DevConsoleSuggestion> Suggestions { get; }
        public int HighlightedSuggestionIndex { get; }
    }
}
