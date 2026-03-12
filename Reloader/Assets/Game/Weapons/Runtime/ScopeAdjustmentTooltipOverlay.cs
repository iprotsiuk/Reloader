using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class ScopeAdjustmentTooltipOverlay : MonoBehaviour
    {
        [SerializeField] private Rect _screenRect = new Rect(24f, 24f, 240f, 28f);

        public bool IsVisible { get; private set; }
        public string CurrentText { get; private set; } = string.Empty;

        public void SetState(bool isVisible, int windageClicks, int elevationClicks)
        {
            IsVisible = isVisible;
            CurrentText = isVisible
                ? $"ELEV {FormatSignedClicks(elevationClicks)}   WIND {FormatSignedClicks(windageClicks)}"
                : string.Empty;
        }

        private void OnGUI()
        {
            if (!IsVisible || string.IsNullOrWhiteSpace(CurrentText))
            {
                return;
            }

            GUI.Box(_screenRect, CurrentText);
        }

        private static string FormatSignedClicks(int clicks)
        {
            return clicks >= 0 ? $"+{clicks}" : clicks.ToString();
        }
    }
}
