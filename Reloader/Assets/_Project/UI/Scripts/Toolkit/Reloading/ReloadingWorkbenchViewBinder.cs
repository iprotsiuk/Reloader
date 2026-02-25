using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.Reloading
{
    public sealed class ReloadingWorkbenchViewBinder : IUiViewBinder
    {
        private VisualElement _root;
        private VisualElement[] _operationElements = Array.Empty<VisualElement>();
        private Button _executeButton;
        private Label _resultLabel;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int operationCount)
        {
            _root = root;
            _operationElements = new VisualElement[Math.Max(0, operationCount)];
            for (var i = 0; i < _operationElements.Length; i++)
            {
                var operationElement = root?.Q<VisualElement>($"reloading__operation-{i}");
                _operationElements[i] = operationElement;
                if (operationElement != null)
                {
                    var captured = i;
                    operationElement.RegisterCallback<ClickEvent>(_ => IntentRaised?.Invoke(new UiIntent("reloading.operation.select", captured)));
                    operationElement.RegisterCallback<PointerUpEvent>(_ => IntentRaised?.Invoke(new UiIntent("reloading.operation.select", captured)));
                }
            }

            _executeButton = root?.Q<Button>("reloading__execute");
            if (_executeButton != null)
            {
                _executeButton.clicked += () => IntentRaised?.Invoke(new UiIntent("reloading.operation.execute"));
            }

            _resultLabel = root?.Q<Label>("reloading__result-label");
        }

        public void Render(UiRenderState state)
        {
            if (state is not ReloadingWorkbenchUiState workbenchState)
            {
                return;
            }

            var limit = Math.Min(_operationElements.Length, workbenchState.Operations.Count);
            for (var i = 0; i < limit; i++)
            {
                var opElement = _operationElements[i];
                if (opElement == null)
                {
                    continue;
                }

                opElement.EnableInClassList("is-selected", workbenchState.Operations[i].IsSelected);
            }

            if (_resultLabel != null)
            {
                _resultLabel.text = workbenchState.ResultText;
            }
        }

        public bool TryRaiseSelectOperationIntent(int operationIndex)
        {
            if (operationIndex < 0 || operationIndex >= _operationElements.Length)
            {
                return false;
            }

            IntentRaised?.Invoke(new UiIntent("reloading.operation.select", operationIndex));
            return true;
        }

        public bool TryRaiseExecuteIntent()
        {
            IntentRaised?.Invoke(new UiIntent("reloading.operation.execute"));
            return true;
        }

        public void SetVisible(bool isVisible)
        {
            if (_root == null)
            {
                return;
            }

            _root.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
