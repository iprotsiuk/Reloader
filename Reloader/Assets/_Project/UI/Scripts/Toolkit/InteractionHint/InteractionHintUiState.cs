using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.InteractionHint
{
    public sealed class InteractionHintUiState : UiRenderState
    {
        public InteractionHintUiState(string text, bool isVisible)
            : base("interaction-hint")
        {
            Text = text ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Text { get; }
        public bool IsVisible { get; }
    }
}
