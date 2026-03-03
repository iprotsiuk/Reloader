using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.EscMenu
{
    public sealed class EscMenuViewBinder : IUiViewBinder
    {
        private VisualElement _panel;
        private VisualElement _mainScreen;
        private VisualElement _settingsScreen;
        private VisualElement _keyBindingsScreen;

        private Button _resumeButton;
        private Button _settingsButton;
        private Button _keyBindingsButton;
        private Button _quitButton;
        private Button _settingsBackButton;
        private Button _keyBindingsBackButton;

        private Button _gameTabButton;
        private Button _videoTabButton;
        private Button _audioTabButton;
        private VisualElement _gameTabPanel;
        private VisualElement _videoTabPanel;
        private VisualElement _audioTabPanel;

        private DropdownField _resolutionDropdown;
        private Slider _fovSlider;
        private Slider _globalVolumeSlider;
        private Slider _musicVolumeSlider;
        private Slider _soundsVolumeSlider;

        private bool _suppressCallbacks;

        public event Action<UiIntent> IntentRaised;

        public void Initialize(VisualElement root)
        {
            UnregisterCallbacks();

            _panel = root?.Q<VisualElement>("esc-menu__panel");
            _mainScreen = root?.Q<VisualElement>("esc-menu__screen-main");
            _settingsScreen = root?.Q<VisualElement>("esc-menu__screen-settings");
            _keyBindingsScreen = root?.Q<VisualElement>("esc-menu__screen-keybindings");

            _resumeButton = root?.Q<Button>("esc-menu__resume");
            _settingsButton = root?.Q<Button>("esc-menu__settings");
            _keyBindingsButton = root?.Q<Button>("esc-menu__keybindings");
            _quitButton = root?.Q<Button>("esc-menu__quit");
            _settingsBackButton = root?.Q<Button>("esc-menu__settings-back");
            _keyBindingsBackButton = root?.Q<Button>("esc-menu__keybindings-back");

            _gameTabButton = root?.Q<Button>("esc-menu__tab-game");
            _videoTabButton = root?.Q<Button>("esc-menu__tab-video");
            _audioTabButton = root?.Q<Button>("esc-menu__tab-audio");
            _gameTabPanel = root?.Q<VisualElement>("esc-menu__tab-panel-game");
            _videoTabPanel = root?.Q<VisualElement>("esc-menu__tab-panel-video");
            _audioTabPanel = root?.Q<VisualElement>("esc-menu__tab-panel-audio");

            _resolutionDropdown = root?.Q<DropdownField>("esc-menu__resolution");
            _fovSlider = root?.Q<Slider>("esc-menu__fov");
            _globalVolumeSlider = root?.Q<Slider>("esc-menu__global-volume");
            _musicVolumeSlider = root?.Q<Slider>("esc-menu__music-volume");
            _soundsVolumeSlider = root?.Q<Slider>("esc-menu__sounds-volume");

            RegisterCallbacks();
        }

        public void Render(UiRenderState state)
        {
            if (state is not EscMenuUiState escState)
            {
                return;
            }

            if (_panel != null)
            {
                _panel.style.display = escState.IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }

            ApplyScreenVisibility(escState.Screen);
            ApplySettingsTabVisibility(escState.SettingsTab);

            _suppressCallbacks = true;
            try
            {
                if (_resolutionDropdown != null)
                {
                    _resolutionDropdown.choices = new List<string>(escState.ResolutionOptions);
                    if (_resolutionDropdown.choices.Count > 0)
                    {
                        var selectedIndex = Math.Max(0, Math.Min(escState.SelectedResolutionIndex, _resolutionDropdown.choices.Count - 1));
                        _resolutionDropdown.index = selectedIndex;
                        _resolutionDropdown.SetValueWithoutNotify(_resolutionDropdown.choices[selectedIndex]);
                    }
                }

                _fovSlider?.SetValueWithoutNotify(escState.Fov);
                _globalVolumeSlider?.SetValueWithoutNotify(escState.GlobalVolume);
                _musicVolumeSlider?.SetValueWithoutNotify(escState.MusicVolume);
                _soundsVolumeSlider?.SetValueWithoutNotify(escState.SoundsVolume);
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        private void RegisterCallbacks()
        {
            if (_resumeButton != null)
            {
                _resumeButton.clicked += HandleResumeClicked;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked += HandleSettingsClicked;
            }

            if (_keyBindingsButton != null)
            {
                _keyBindingsButton.clicked += HandleKeyBindingsClicked;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked += HandleQuitClicked;
            }

            if (_settingsBackButton != null)
            {
                _settingsBackButton.clicked += HandleBackClicked;
            }

            if (_keyBindingsBackButton != null)
            {
                _keyBindingsBackButton.clicked += HandleBackClicked;
            }

            if (_gameTabButton != null)
            {
                _gameTabButton.clicked += HandleGameTabClicked;
            }

            if (_videoTabButton != null)
            {
                _videoTabButton.clicked += HandleVideoTabClicked;
            }

            if (_audioTabButton != null)
            {
                _audioTabButton.clicked += HandleAudioTabClicked;
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.RegisterValueChangedCallback(HandleResolutionChanged);
            }

            if (_fovSlider != null)
            {
                _fovSlider.RegisterValueChangedCallback(HandleFovChanged);
            }

            if (_globalVolumeSlider != null)
            {
                _globalVolumeSlider.RegisterValueChangedCallback(HandleGlobalVolumeChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.RegisterValueChangedCallback(HandleMusicVolumeChanged);
            }

            if (_soundsVolumeSlider != null)
            {
                _soundsVolumeSlider.RegisterValueChangedCallback(HandleSoundsVolumeChanged);
            }
        }

        private void UnregisterCallbacks()
        {
            if (_resumeButton != null)
            {
                _resumeButton.clicked -= HandleResumeClicked;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked -= HandleSettingsClicked;
            }

            if (_keyBindingsButton != null)
            {
                _keyBindingsButton.clicked -= HandleKeyBindingsClicked;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked -= HandleQuitClicked;
            }

            if (_settingsBackButton != null)
            {
                _settingsBackButton.clicked -= HandleBackClicked;
            }

            if (_keyBindingsBackButton != null)
            {
                _keyBindingsBackButton.clicked -= HandleBackClicked;
            }

            if (_gameTabButton != null)
            {
                _gameTabButton.clicked -= HandleGameTabClicked;
            }

            if (_videoTabButton != null)
            {
                _videoTabButton.clicked -= HandleVideoTabClicked;
            }

            if (_audioTabButton != null)
            {
                _audioTabButton.clicked -= HandleAudioTabClicked;
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.UnregisterValueChangedCallback(HandleResolutionChanged);
            }

            if (_fovSlider != null)
            {
                _fovSlider.UnregisterValueChangedCallback(HandleFovChanged);
            }

            if (_globalVolumeSlider != null)
            {
                _globalVolumeSlider.UnregisterValueChangedCallback(HandleGlobalVolumeChanged);
            }

            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.UnregisterValueChangedCallback(HandleMusicVolumeChanged);
            }

            if (_soundsVolumeSlider != null)
            {
                _soundsVolumeSlider.UnregisterValueChangedCallback(HandleSoundsVolumeChanged);
            }
        }

        private void ApplyScreenVisibility(EscMenuScreen screen)
        {
            if (_mainScreen != null)
            {
                _mainScreen.style.display = screen == EscMenuScreen.Main ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_settingsScreen != null)
            {
                _settingsScreen.style.display = screen == EscMenuScreen.Settings ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_keyBindingsScreen != null)
            {
                _keyBindingsScreen.style.display = screen == EscMenuScreen.KeyBindings ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplySettingsTabVisibility(EscMenuSettingsTab tab)
        {
            SetTabActive(_gameTabButton, tab == EscMenuSettingsTab.Game);
            SetTabActive(_videoTabButton, tab == EscMenuSettingsTab.Video);
            SetTabActive(_audioTabButton, tab == EscMenuSettingsTab.Audio);

            if (_gameTabPanel != null)
            {
                _gameTabPanel.style.display = tab == EscMenuSettingsTab.Game ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_videoTabPanel != null)
            {
                _videoTabPanel.style.display = tab == EscMenuSettingsTab.Video ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_audioTabPanel != null)
            {
                _audioTabPanel.style.display = tab == EscMenuSettingsTab.Audio ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static void SetTabActive(VisualElement button, bool isActive)
        {
            button?.EnableInClassList("is-active", isActive);
        }

        private void HandleResumeClicked() => IntentRaised?.Invoke(new UiIntent("esc.menu.resume"));
        private void HandleSettingsClicked() => IntentRaised?.Invoke(new UiIntent("esc.menu.settings"));
        private void HandleKeyBindingsClicked() => IntentRaised?.Invoke(new UiIntent("esc.menu.keybindings"));
        private void HandleQuitClicked() => IntentRaised?.Invoke(new UiIntent("esc.menu.quit"));
        private void HandleBackClicked() => IntentRaised?.Invoke(new UiIntent("esc.menu.back"));
        private void HandleGameTabClicked() => IntentRaised?.Invoke(new UiIntent("esc.menu.settings.tab", "game"));
        private void HandleVideoTabClicked() => IntentRaised?.Invoke(new UiIntent("esc.menu.settings.tab", "video"));
        private void HandleAudioTabClicked() => IntentRaised?.Invoke(new UiIntent("esc.menu.settings.tab", "audio"));

        private void HandleResolutionChanged(ChangeEvent<string> evt)
        {
            if (_suppressCallbacks || _resolutionDropdown == null)
            {
                return;
            }

            var index = _resolutionDropdown.index;
            if (index < 0 && _resolutionDropdown.choices != null)
            {
                index = _resolutionDropdown.choices.IndexOf(evt.newValue);
            }

            IntentRaised?.Invoke(new UiIntent("esc.menu.settings.video.resolution.changed", Math.Max(0, index)));
        }

        private void HandleFovChanged(ChangeEvent<float> evt)
        {
            if (!_suppressCallbacks)
            {
                IntentRaised?.Invoke(new UiIntent("esc.menu.settings.video.fov.changed", evt.newValue));
            }
        }

        private void HandleGlobalVolumeChanged(ChangeEvent<float> evt)
        {
            if (!_suppressCallbacks)
            {
                IntentRaised?.Invoke(new UiIntent("esc.menu.settings.audio.global.changed", evt.newValue));
            }
        }

        private void HandleMusicVolumeChanged(ChangeEvent<float> evt)
        {
            if (!_suppressCallbacks)
            {
                IntentRaised?.Invoke(new UiIntent("esc.menu.settings.audio.music.changed", evt.newValue));
            }
        }

        private void HandleSoundsVolumeChanged(ChangeEvent<float> evt)
        {
            if (!_suppressCallbacks)
            {
                IntentRaised?.Invoke(new UiIntent("esc.menu.settings.audio.sounds.changed", evt.newValue));
            }
        }
    }
}
