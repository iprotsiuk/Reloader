using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.HealthHud
{
    public sealed class HealthHudUiState : UiRenderState
    {
        public HealthHudUiState(
            string healthValueText,
            float healthFraction,
            bool isVisible,
            bool isLowHealth,
            bool isCritical,
            bool isDead,
            bool isDamageFlashVisible)
            : base(Reloader.UI.Toolkit.Runtime.UiRuntimeCompositionIds.ScreenIds.HealthHud)
        {
            HealthValueText = string.IsNullOrWhiteSpace(healthValueText) ? "-- / --" : healthValueText.Trim();
            HealthFraction = Clamp01(healthFraction);
            IsVisible = isVisible;
            IsLowHealth = isLowHealth;
            IsCritical = isCritical;
            IsDead = isDead;
            IsDamageFlashVisible = isDamageFlashVisible;
        }

        public string HealthValueText { get; }
        public float HealthFraction { get; }
        public bool IsVisible { get; }
        public bool IsLowHealth { get; }
        public bool IsCritical { get; }
        public bool IsDead { get; }
        public bool IsDamageFlashVisible { get; }

        public static HealthHudUiState Create(
            string healthValueText,
            float healthFraction,
            bool isVisible,
            bool isLowHealth,
            bool isCritical,
            bool isDead,
            bool isDamageFlashVisible)
        {
            return new HealthHudUiState(
                healthValueText,
                healthFraction,
                isVisible,
                isLowHealth,
                isCritical,
                isDead,
                isDamageFlashVisible);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }
}
