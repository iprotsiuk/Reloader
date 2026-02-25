using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.AmmoHud
{
    public sealed class AmmoHudViewBinder : IUiViewBinder
    {
        private VisualElement _root;
        private Label _countLabel;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            _root = root;
            _countLabel = root?.Q<Label>("ammo__count-label");
        }

        public void Render(UiRenderState state)
        {
            if (state is not AmmoHudUiState ammoState)
            {
                return;
            }

            if (_countLabel != null)
            {
                _countLabel.text = ammoState.LabelText;
            }

            if (_root != null)
            {
                _root.style.display = ammoState.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
