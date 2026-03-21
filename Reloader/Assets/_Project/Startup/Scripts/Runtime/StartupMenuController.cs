using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.Startup.Runtime
{
    [DisallowMultipleComponent]
    public sealed class StartupMenuController : MonoBehaviour
    {
        [SerializeField] private PanelSettings _panelSettings;

        private UIDocument _document;
        private IStartupMenuFlow _flow;
        private StartupMenuState _state = StartupMenuState.Empty;
        private VisualElement _settingsPanel;
        private Button _continueButton;
        private Label _statusLabel;
        private bool _uiBuilt;

        public StartupMenuState CurrentState => _state;

        public void SetFlowForTests(IStartupMenuFlow flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            RefreshState();
        }

        private void Awake()
        {
            EnsureDocument();
            BuildUiIfNeeded();
        }

        private void OnEnable()
        {
            EnsureDocument();
            BuildUiIfNeeded();
            RefreshState();
        }

        public void RefreshState()
        {
            _flow ??= new StartupMenuFlow();
            _state = _flow.RefreshState();
            UpdateStateUi();
        }

        public void HandleNewGameClicked()
        {
            _flow ??= new StartupMenuFlow();
            if (_flow.TryStartNewGame())
            {
                return;
            }

            SetStatusText("New Game could not start.");
        }

        public void HandleContinueClicked()
        {
            if (!_state.CanContinue)
            {
                SetStatusText("No save found.");
                return;
            }

            _flow ??= new StartupMenuFlow();
            if (_flow.TryContinueLatest())
            {
                return;
            }

            SetStatusText("Continue could not load the latest save.");
        }

        public void HandleSettingsClicked()
        {
            if (_settingsPanel == null)
            {
                return;
            }

            var isVisible = _settingsPanel.style.display.value != DisplayStyle.None;
            _settingsPanel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void HandleQuitClicked()
        {
            Application.Quit();
        }

        private void EnsureDocument()
        {
            _document ??= GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            if (_panelSettings != null)
            {
                _document.panelSettings = _panelSettings;
            }
        }

        private void BuildUiIfNeeded()
        {
            if (_uiBuilt)
            {
                return;
            }

            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            root.Clear();
            root.style.flexGrow = 1f;
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;
            root.style.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);

            var shell = new VisualElement
            {
                name = "bootstrap-menu__shell"
            };
            shell.style.width = 560f;
            shell.style.minHeight = 360f;
            shell.style.paddingLeft = 18f;
            shell.style.paddingRight = 18f;
            shell.style.paddingTop = 18f;
            shell.style.paddingBottom = 18f;
            shell.style.flexDirection = FlexDirection.Column;
            shell.style.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 0.94f);
            shell.style.borderTopLeftRadius = 12f;
            shell.style.borderTopRightRadius = 12f;
            shell.style.borderBottomLeftRadius = 12f;
            shell.style.borderBottomRightRadius = 12f;
            shell.style.borderTopWidth = 1f;
            shell.style.borderRightWidth = 1f;
            shell.style.borderBottomWidth = 1f;
            shell.style.borderLeftWidth = 1f;
            shell.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
            shell.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
            shell.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
            shell.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);

            var titleRow = new VisualElement
            {
                name = "bootstrap-menu__title-row"
            };
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.justifyContent = Justify.SpaceBetween;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 6f;

            var title = new Label("Reloader");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 20f;
            title.style.color = new Color(0.94f, 0.95f, 0.96f, 1f);

            var subtitle = new Label("Bootstrap Front Door");
            subtitle.style.color = new Color(0.72f, 0.75f, 0.79f, 1f);
            subtitle.style.fontSize = 11f;

            titleRow.Add(title);
            titleRow.Add(subtitle);

            var buttons = new VisualElement
            {
                name = "bootstrap-menu__buttons"
            };
            buttons.style.flexDirection = FlexDirection.Column;

            _continueButton = CreateActionButton("bootstrap-menu__continue", "Continue", HandleContinueClicked);
            var newGameButton = CreateActionButton("bootstrap-menu__new-game", "New Game", HandleNewGameClicked);
            var settingsButton = CreateActionButton("bootstrap-menu__settings", "Settings", HandleSettingsClicked);
            var quitButton = CreateActionButton("bootstrap-menu__quit", "Quit", HandleQuitClicked);

            buttons.Add(newGameButton);
            buttons.Add(_continueButton);
            buttons.Add(settingsButton);
            buttons.Add(quitButton);

            _statusLabel = new Label
            {
                name = "bootstrap-menu__status",
                text = "No save found."
            };
            _statusLabel.style.color = new Color(0.82f, 0.84f, 0.87f, 1f);
            _statusLabel.style.fontSize = 12f;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            _statusLabel.style.marginTop = 4f;

            _settingsPanel = BuildSettingsPanel();

            shell.Add(titleRow);
            shell.Add(buttons);
            shell.Add(_statusLabel);
            shell.Add(_settingsPanel);
            root.Add(shell);

            _uiBuilt = true;
            UpdateStateUi();
        }

        private static Button CreateActionButton(string name, string text, Action clicked)
        {
            var button = new Button(clicked)
            {
                name = name,
                text = text
            };
            button.style.height = 34f;
            button.style.fontSize = 13f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.backgroundColor = new Color(0.18f, 0.2f, 0.24f, 1f);
            button.style.color = new Color(0.95f, 0.96f, 0.97f, 1f);
            button.style.borderTopWidth = 1f;
            button.style.borderRightWidth = 1f;
            button.style.borderBottomWidth = 1f;
            button.style.borderLeftWidth = 1f;
            button.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
            button.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
            button.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
            button.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);
            button.style.marginBottom = 8f;
            return button;
        }

        private VisualElement BuildSettingsPanel()
        {
            var panel = new VisualElement
            {
                name = "bootstrap-menu__settings-panel"
            };
            panel.style.display = DisplayStyle.None;
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.paddingTop = 10f;
            panel.style.paddingRight = 10f;
            panel.style.paddingBottom = 10f;
            panel.style.paddingLeft = 10f;
            panel.style.backgroundColor = new Color(1f, 1f, 1f, 0.04f);
            panel.style.borderTopLeftRadius = 8f;
            panel.style.borderTopRightRadius = 8f;
            panel.style.borderBottomLeftRadius = 8f;
            panel.style.borderBottomRightRadius = 8f;
            panel.style.borderTopWidth = 1f;
            panel.style.borderRightWidth = 1f;
            panel.style.borderBottomWidth = 1f;
            panel.style.borderLeftWidth = 1f;
            panel.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
            panel.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
            panel.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
            panel.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);

            var title = new Label("Settings");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14f;
            title.style.color = new Color(0.94f, 0.95f, 0.96f, 1f);
            title.style.marginBottom = 4f;

            var note = new Label("Settings are exposed in-game for now. This front door stays focused on starting and continuing.");
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.color = new Color(0.78f, 0.8f, 0.83f, 1f);
            note.style.fontSize = 12f;
            note.style.marginBottom = 6f;

            var backButton = CreateActionButton("bootstrap-menu__settings-back", "Back", HandleSettingsClicked);

            panel.Add(title);
            panel.Add(note);
            panel.Add(backButton);
            return panel;
        }

        private void UpdateStateUi()
        {
            if (_continueButton != null)
            {
                _continueButton.SetEnabled(_state.CanContinue);
            }

            if (_statusLabel != null)
            {
                _statusLabel.text = _state.StatusMessage;
            }
        }

        private void SetStatusText(string statusText)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = statusText;
            }
        }
    }
}
