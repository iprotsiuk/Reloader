using System;
using System.Collections.Generic;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.EscMenu
{
    public enum EscMenuScreen
    {
        Main,
        Settings,
        KeyBindings
    }

    public enum EscMenuSettingsTab
    {
        Game,
        Video,
        Audio
    }

    public sealed class EscMenuUiState : UiRenderState
    {
        private readonly string[] _resolutionOptions;

        private EscMenuUiState(
            bool isOpen,
            EscMenuScreen screen,
            EscMenuSettingsTab settingsTab,
            string[] resolutionOptions,
            int selectedResolutionIndex,
            float fov,
            float globalVolume,
            float musicVolume,
            float soundsVolume)
            : base("esc-menu")
        {
            IsOpen = isOpen;
            Screen = screen;
            SettingsTab = settingsTab;
            _resolutionOptions = resolutionOptions ?? Array.Empty<string>();
            SelectedResolutionIndex = Math.Max(0, selectedResolutionIndex);
            Fov = fov;
            GlobalVolume = globalVolume;
            MusicVolume = musicVolume;
            SoundsVolume = soundsVolume;
        }

        public bool IsOpen { get; }
        public EscMenuScreen Screen { get; }
        public EscMenuSettingsTab SettingsTab { get; }
        public IReadOnlyList<string> ResolutionOptions => _resolutionOptions;
        public int SelectedResolutionIndex { get; }
        public float Fov { get; }
        public float GlobalVolume { get; }
        public float MusicVolume { get; }
        public float SoundsVolume { get; }

        public static EscMenuUiState Create(
            bool isOpen,
            EscMenuScreen screen,
            EscMenuSettingsTab settingsTab,
            IEnumerable<string> resolutionOptions,
            int selectedResolutionIndex,
            float fov,
            float globalVolume,
            float musicVolume,
            float soundsVolume)
        {
            var options = resolutionOptions == null ? Array.Empty<string>() : new List<string>(resolutionOptions).ToArray();
            return new EscMenuUiState(
                isOpen,
                screen,
                settingsTab,
                options,
                selectedResolutionIndex,
                fov,
                globalVolume,
                musicVolume,
                soundsVolume);
        }
    }
}
