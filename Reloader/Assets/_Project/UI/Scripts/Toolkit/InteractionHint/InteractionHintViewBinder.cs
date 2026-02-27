using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.InteractionHint
{
    public sealed class InteractionHintViewBinder : IUiViewBinder
    {
        private VisualElement _root;
        private VisualElement _hintRoot;
        private Label _textLabel;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            _root = root;
            _hintRoot = root?.Q<VisualElement>("interaction-hint__root");
            _textLabel = root?.Q<Label>("interaction-hint__text");
        }

        public void Render(UiRenderState state)
        {
            if (state is not InteractionHintUiState interactionHintState)
            {
                return;
            }

            if (_textLabel != null)
            {
                _textLabel.text = interactionHintState.Text;
            }

            var visibilityTarget = _hintRoot ?? _root;
            if (visibilityTarget != null)
            {
                visibilityTarget.style.display = interactionHintState.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
