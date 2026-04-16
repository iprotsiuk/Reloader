using Reloader.UI.Toolkit.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.HealthHud
{
    public sealed class HealthHudViewBinder : IUiViewBinder
    {
        private VisualElement _root;
        private Label _valueLabel;
        private Label _statusLabel;
        private VisualElement _fill;
        private VisualElement _flash;

        public event System.Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            if (root != null)
            {
                root.pickingMode = PickingMode.Ignore;
            }

            _root = root?.Q<VisualElement>("health-hud__root") ?? root;
            _valueLabel = root?.Q<Label>("health-hud__value-label");
            _statusLabel = root?.Q<Label>("health-hud__status-label");
            _fill = root?.Q<VisualElement>("health-hud__fill");
            _flash = root?.Q<VisualElement>("health-hud__flash");
        }

        public void Render(UiRenderState state)
        {
            if (state is not HealthHudUiState healthState)
            {
                return;
            }

            ApplyVisibility(healthState.IsVisible);
            ApplyValueText(healthState.HealthValueText);
            ApplyFillFraction(healthState.HealthFraction);
            ApplyStateClasses(healthState);
            ApplyStatusText(healthState);
            ApplyFlashVisibility(healthState);
        }

        private void ApplyVisibility(bool isVisible)
        {
            if (_root == null)
            {
                return;
            }

            _root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyValueText(string valueText)
        {
            if (_valueLabel != null)
            {
                _valueLabel.text = valueText;
            }
        }

        private void ApplyFillFraction(float healthFraction)
        {
            if (_fill == null)
            {
                return;
            }

            _fill.style.width = Length.Percent(Mathf.Clamp01(healthFraction) * 100f);
        }

        private void ApplyStateClasses(HealthHudUiState healthState)
        {
            SetClass(_root, "is-low-health", healthState.IsVisible && healthState.IsLowHealth);
            SetClass(_root, "is-critical", healthState.IsVisible && healthState.IsCritical);
            SetClass(_root, "is-dead", healthState.IsVisible && healthState.IsDead);
            SetClass(_root, "is-damage-flash", healthState.IsVisible && healthState.IsDamageFlashVisible);

            SetClass(_fill, "is-low-health", healthState.IsVisible && healthState.IsLowHealth);
            SetClass(_fill, "is-critical", healthState.IsVisible && healthState.IsCritical);
            SetClass(_fill, "is-dead", healthState.IsVisible && healthState.IsDead);
        }

        private void ApplyStatusText(HealthHudUiState healthState)
        {
            if (_statusLabel == null)
            {
                return;
            }

            var statusText = ResolveStatusText(healthState);
            _statusLabel.text = statusText;
            _statusLabel.style.display = string.IsNullOrWhiteSpace(statusText)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        private void ApplyFlashVisibility(HealthHudUiState healthState)
        {
            if (_flash == null)
            {
                return;
            }

            _flash.style.display = healthState.IsVisible && healthState.IsDamageFlashVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private static string ResolveStatusText(HealthHudUiState healthState)
        {
            if (healthState.IsDead)
            {
                return "DEAD";
            }

            if (healthState.IsCritical)
            {
                return "CRITICAL";
            }

            if (healthState.IsLowHealth)
            {
                return "LOW";
            }

            return string.Empty;
        }

        private static void SetClass(VisualElement element, string className, bool enabled)
        {
            if (element != null)
            {
                element.EnableInClassList(className, enabled);
            }
        }
    }
}
