using System;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.Reloading
{
    public sealed class ReloadingWorkbenchViewBinder : IUiViewBinder
    {
        private VisualElement _root;
        private VisualElement[] _operationElements = Array.Empty<VisualElement>();
        private Label[] _operationLabels = Array.Empty<Label>();
        private Button _executeButton;
        private Label _resultLabel;
        private Button _setupModeButton;
        private Button _operateModeButton;
        private VisualElement _setupPanel;
        private VisualElement _operatePanel;
        private Label _setupSlotsLabel;
        private Label _setupDiagnosticsLabel;
        private Label _operateDiagnosticsLabel;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root, int operationCount)
        {
            _root = root;
            _operationElements = new VisualElement[Math.Max(0, operationCount)];
            _operationLabels = new Label[Math.Max(0, operationCount)];
            for (var i = 0; i < _operationElements.Length; i++)
            {
                var operationElement = root?.Q<VisualElement>($"reloading__operation-{i}");
                _operationElements[i] = operationElement;
                _operationLabels[i] = root?.Q<Label>($"reloading__operation-label-{i}");
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
            _setupModeButton = root?.Q<Button>("reloading__mode-setup");
            if (_setupModeButton != null)
            {
                _setupModeButton.clicked += () => IntentRaised?.Invoke(new UiIntent("reloading.mode.setup"));
            }

            _operateModeButton = root?.Q<Button>("reloading__mode-operate");
            if (_operateModeButton != null)
            {
                _operateModeButton.clicked += () => IntentRaised?.Invoke(new UiIntent("reloading.mode.operate"));
            }

            _setupPanel = root?.Q<VisualElement>("reloading__setup-panel");
            _operatePanel = root?.Q<VisualElement>("reloading__operate-panel");
            _setupSlotsLabel = root?.Q<Label>("reloading__setup-slots");
            _setupDiagnosticsLabel = root?.Q<Label>("reloading__setup-diagnostics");
            _operateDiagnosticsLabel = root?.Q<Label>("reloading__operate-diagnostics");
        }

        public void Render(UiRenderState state)
        {
            if (state is not ReloadingWorkbenchUiState workbenchState)
            {
                return;
            }

            var isSetupMode = workbenchState.Mode == ReloadingWorkbenchMode.Setup;
            if (_setupPanel != null)
            {
                _setupPanel.style.display = isSetupMode ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_operatePanel != null)
            {
                _operatePanel.style.display = isSetupMode ? DisplayStyle.None : DisplayStyle.Flex;
            }

            _setupModeButton?.EnableInClassList("is-selected", isSetupMode);
            _operateModeButton?.EnableInClassList("is-selected", !isSetupMode);

            var limit = Math.Min(_operationElements.Length, workbenchState.Operations.Count);
            for (var i = 0; i < limit; i++)
            {
                var opElement = _operationElements[i];
                if (opElement == null)
                {
                    continue;
                }

                var opState = workbenchState.Operations[i];
                opElement.EnableInClassList("is-selected", opState.IsSelected);
                opElement.EnableInClassList("is-disabled", !opState.IsEnabled);
                var opLabel = _operationLabels[i];
                if (opLabel != null)
                {
                    opLabel.text = opState.Label;
                }
            }

            if (_resultLabel != null)
            {
                _resultLabel.text = workbenchState.ResultText;
            }

            if (_setupSlotsLabel != null)
            {
                _setupSlotsLabel.text = workbenchState.SetupSlotsText;
            }

            if (_setupDiagnosticsLabel != null)
            {
                _setupDiagnosticsLabel.text = workbenchState.SetupDiagnosticsText;
            }

            if (_operateDiagnosticsLabel != null)
            {
                _operateDiagnosticsLabel.text = workbenchState.OperateDiagnosticsText;
            }

            if (_executeButton != null)
            {
                var canExecute = false;
                for (var i = 0; i < limit; i++)
                {
                    var operation = workbenchState.Operations[i];
                    if (operation.IsSelected)
                    {
                        canExecute = operation.IsEnabled;
                        break;
                    }
                }

                _executeButton.SetEnabled(!isSetupMode && canExecute);
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

        public bool TryRaiseSetupModeIntent()
        {
            IntentRaised?.Invoke(new UiIntent("reloading.mode.setup"));
            return true;
        }

        public bool TryRaiseOperateModeIntent()
        {
            IntentRaised?.Invoke(new UiIntent("reloading.mode.operate"));
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
