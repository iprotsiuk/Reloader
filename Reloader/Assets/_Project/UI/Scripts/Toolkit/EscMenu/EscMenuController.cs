using System;
using Reloader.Inventory;
using Reloader.Core.Runtime;
using Reloader.UI.Toolkit.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.UI.Toolkit.EscMenu
{
    public interface IEscMenuKeySource
    {
        bool ConsumeEscapePressedThisFrame();
    }

    public sealed class EscMenuController : MonoBehaviour, IUiController
    {
        private EscMenuViewBinder _viewBinder;
        private IEscMenuKeySource _keySource;
        private EscMenuSettingsStore _settingsStore;
        private IUiStateEvents _uiStateEvents;
        private bool _useRuntimeKernelUiStateEvents = true;

        private bool _isOpen;
        private EscMenuScreen _screen = EscMenuScreen.Main;
        private EscMenuSettingsTab _settingsTab = EscMenuSettingsTab.Game;
        private bool _nonEscMenuWasOpenLastFrame;

        public event Action QuitRequested;

        private void Awake()
        {
            _keySource ??= new KeyboardEscMenuKeySource();
            _settingsStore ??= new EscMenuSettingsStore();
        }

        private void OnEnable()
        {
            SubscribeToRuntimeHubReconfigure();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            SetMenuOpen(false);
        }

        private void Update()
        {
            Tick();
        }

        public void Configure(IUiStateEvents uiStateEvents = null)
        {
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
        }

        public void SetEscKeySource(IEscMenuKeySource keySource)
        {
            _keySource = keySource;
        }

        public void SetSettingsStore(EscMenuSettingsStore settingsStore)
        {
            _settingsStore = settingsStore ?? new EscMenuSettingsStore();
            Refresh();
        }

        public void SetViewBinder(EscMenuViewBinder viewBinder)
        {
            _viewBinder = viewBinder;
            Refresh();
        }

        public void Tick()
        {
            _keySource ??= new KeyboardEscMenuKeySource();
            var uiStateEvents = ResolveUiStateEvents();
            var storageMenuOpenNow = StorageUiSession.IsOpen;
            var nonEscMenuOpenNow = uiStateEvents != null
                && (uiStateEvents.IsShopTradeMenuOpen || uiStateEvents.IsWorkbenchMenuVisible || uiStateEvents.IsTabInventoryVisible || storageMenuOpenNow);
            if (uiStateEvents == null)
            {
                nonEscMenuOpenNow = storageMenuOpenNow;
            }

            if (!_keySource.ConsumeEscapePressedThisFrame())
            {
                _nonEscMenuWasOpenLastFrame = nonEscMenuOpenNow;
                return;
            }

            if (_isOpen)
            {
                CloseMenu();
                _nonEscMenuWasOpenLastFrame = nonEscMenuOpenNow;
                return;
            }

            // Escape should not open ESC menu in the same frame another menu was closed.
            if (nonEscMenuOpenNow || _nonEscMenuWasOpenLastFrame || (uiStateEvents?.IsAnyMenuOpen == true))
            {
                _nonEscMenuWasOpenLastFrame = nonEscMenuOpenNow;
                return;
            }

            SetMenuOpen(true);
            _screen = EscMenuScreen.Main;
            _settingsTab = EscMenuSettingsTab.Game;
            Refresh();
            _nonEscMenuWasOpenLastFrame = nonEscMenuOpenNow;
        }

        public void HandleIntent(UiIntent intent)
        {
            if (intent.Key == "esc.menu.resume")
            {
                CloseMenu();
                return;
            }

            if (intent.Key == "esc.menu.settings")
            {
                _screen = EscMenuScreen.Settings;
                Refresh();
                return;
            }

            if (intent.Key == "esc.menu.keybindings")
            {
                _screen = EscMenuScreen.KeyBindings;
                Refresh();
                return;
            }

            if (intent.Key == "esc.menu.back")
            {
                _screen = EscMenuScreen.Main;
                Refresh();
                return;
            }

            if (intent.Key == "esc.menu.quit")
            {
                QuitRequested?.Invoke();
                Application.Quit();
                return;
            }

            if (intent.Key == "esc.menu.settings.tab")
            {
                _settingsTab = ParseSettingsTab(intent.Payload as string);
                Refresh();
                return;
            }

            if (intent.Key == "esc.menu.settings.video.resolution.changed" && intent.Payload is int resolutionIndex)
            {
                ResolveSettingsStore().SetSelectedResolutionIndex(resolutionIndex);
                Refresh();
                return;
            }

            if (intent.Key == "esc.menu.settings.video.fov.changed" && TryGetFloat(intent.Payload, out var fov))
            {
                ResolveSettingsStore().SetFov(fov);
                Refresh();
                return;
            }

            if (intent.Key == "esc.menu.settings.audio.global.changed" && TryGetFloat(intent.Payload, out var globalVolume))
            {
                ResolveSettingsStore().SetGlobalVolume(globalVolume);
                Refresh();
                return;
            }

            if (intent.Key == "esc.menu.settings.audio.music.changed" && TryGetFloat(intent.Payload, out var musicVolume))
            {
                ResolveSettingsStore().SetMusicVolume(musicVolume);
                Refresh();
                return;
            }

            if (intent.Key == "esc.menu.settings.audio.sounds.changed" && TryGetFloat(intent.Payload, out var soundsVolume))
            {
                ResolveSettingsStore().SetSoundsVolume(soundsVolume);
                Refresh();
            }
        }

        private void CloseMenu()
        {
            SetMenuOpen(false);
            _screen = EscMenuScreen.Main;
            Refresh();
        }

        private void Refresh()
        {
            if (_viewBinder == null)
            {
                return;
            }

            var snapshot = ResolveSettingsStore().CreateSnapshot();
            _viewBinder.Render(EscMenuUiState.Create(
                isOpen: _isOpen,
                screen: _screen,
                settingsTab: _settingsTab,
                resolutionOptions: snapshot.ResolutionOptions,
                selectedResolutionIndex: snapshot.SelectedResolutionIndex,
                fov: snapshot.Fov,
                globalVolume: snapshot.GlobalVolume,
                musicVolume: snapshot.MusicVolume,
                soundsVolume: snapshot.SoundsVolume));
        }

        private EscMenuSettingsStore ResolveSettingsStore()
        {
            _settingsStore ??= new EscMenuSettingsStore();
            return _settingsStore;
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                _uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            }

            return _uiStateEvents;
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                _uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
                _uiStateEvents?.RaiseEscMenuVisibilityChanged(_isOpen);
            }
        }

        private void SetMenuOpen(bool isOpen)
        {
            if (_isOpen == isOpen)
            {
                return;
            }

            _isOpen = isOpen;
            ResolveUiStateEvents()?.RaiseEscMenuVisibilityChanged(_isOpen);
        }

        private void SubscribeToRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void UnsubscribeFromRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
        }

        private static EscMenuSettingsTab ParseSettingsTab(string tab)
        {
            if (string.Equals(tab, "video", StringComparison.OrdinalIgnoreCase))
            {
                return EscMenuSettingsTab.Video;
            }

            if (string.Equals(tab, "audio", StringComparison.OrdinalIgnoreCase))
            {
                return EscMenuSettingsTab.Audio;
            }

            return EscMenuSettingsTab.Game;
        }

        private static bool TryGetFloat(object payload, out float value)
        {
            if (payload is float floatValue)
            {
                value = floatValue;
                return true;
            }

            value = 0f;
            return false;
        }

        private sealed class KeyboardEscMenuKeySource : IEscMenuKeySource
        {
            public bool ConsumeEscapePressedThisFrame()
            {
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            }
        }
    }
}
