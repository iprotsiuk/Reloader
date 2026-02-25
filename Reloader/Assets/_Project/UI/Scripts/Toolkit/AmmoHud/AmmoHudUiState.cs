using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.AmmoHud
{
    public sealed class AmmoHudUiState : UiRenderState
    {
        public AmmoHudUiState(string labelText, bool isVisible)
            : base("ammo-hud")
        {
            LabelText = string.IsNullOrWhiteSpace(labelText) ? "-- 0/0" : labelText;
            IsVisible = isVisible;
        }

        public string LabelText { get; }
        public bool IsVisible { get; }
    }
}
