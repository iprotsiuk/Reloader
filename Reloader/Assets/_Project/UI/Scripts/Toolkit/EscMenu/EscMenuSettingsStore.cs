using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.UI.Toolkit.EscMenu
{
    public readonly struct EscMenuResolutionOption
    {
        public EscMenuResolutionOption(int width, int height, int refreshRate)
        {
            Width = width;
            Height = height;
            RefreshRate = Math.Max(1, refreshRate);
        }

        public int Width { get; }
        public int Height { get; }
        public int RefreshRate { get; }

        public string ToDisplayString()
        {
            return Width + "x" + Height + " @ " + RefreshRate + "Hz";
        }
    }

    public interface IEscMenuSettingsRuntime
    {
        IReadOnlyList<EscMenuResolutionOption> GetAvailableResolutionOptions();
        float GetCurrentFov();
        float GetCurrentGlobalVolume();
        float GetCurrentMusicVolume();
        float GetCurrentSoundsVolume();
        void ApplyResolution(EscMenuResolutionOption option, int selectedIndex);
        void ApplyFov(float fov);
        void ApplyGlobalVolume(float volume);
        void ApplyMusicVolume(float volume);
        void ApplySoundsVolume(float volume);
    }

    public readonly struct EscMenuSettingsSnapshot
    {
        public EscMenuSettingsSnapshot(
            IReadOnlyList<string> resolutionOptions,
            int selectedResolutionIndex,
            float fov,
            float globalVolume,
            float musicVolume,
            float soundsVolume)
        {
            ResolutionOptions = resolutionOptions ?? Array.Empty<string>();
            SelectedResolutionIndex = selectedResolutionIndex;
            Fov = fov;
            GlobalVolume = globalVolume;
            MusicVolume = musicVolume;
            SoundsVolume = soundsVolume;
        }

        public IReadOnlyList<string> ResolutionOptions { get; }
        public int SelectedResolutionIndex { get; }
        public float Fov { get; }
        public float GlobalVolume { get; }
        public float MusicVolume { get; }
        public float SoundsVolume { get; }
    }

    public sealed class EscMenuSettingsStore
    {
        private const float MinFov = 50f;
        private const float MaxFov = 110f;

        private readonly IEscMenuSettingsRuntime _runtime;
        private readonly string _resolutionKey;
        private readonly string _fovKey;
        private readonly string _globalVolumeKey;
        private readonly string _musicVolumeKey;
        private readonly string _soundsVolumeKey;

        private readonly List<EscMenuResolutionOption> _resolutionOptions = new List<EscMenuResolutionOption>();
        private readonly List<string> _resolutionOptionLabels = new List<string>();

        private int _selectedResolutionIndex;
        private float _fov;
        private float _globalVolume;
        private float _musicVolume;
        private float _soundsVolume;

        public EscMenuSettingsStore(IEscMenuSettingsRuntime runtime = null, string playerPrefsKeyPrefix = "esc-menu")
        {
            _runtime = runtime ?? new UnityEscMenuSettingsRuntime();
            var prefix = string.IsNullOrWhiteSpace(playerPrefsKeyPrefix) ? "esc-menu" : playerPrefsKeyPrefix;
            _resolutionKey = prefix + ".resolution";
            _fovKey = prefix + ".fov";
            _globalVolumeKey = prefix + ".global";
            _musicVolumeKey = prefix + ".music";
            _soundsVolumeKey = prefix + ".sounds";

            LoadOptions();
            LoadPersistedValues();
            ApplyAll();
        }

        public EscMenuSettingsSnapshot CreateSnapshot()
        {
            return new EscMenuSettingsSnapshot(
                _resolutionOptionLabels,
                _selectedResolutionIndex,
                _fov,
                _globalVolume,
                _musicVolume,
                _soundsVolume);
        }

        public void SetSelectedResolutionIndex(int index)
        {
            if (_resolutionOptions.Count == 0)
            {
                _selectedResolutionIndex = 0;
                return;
            }

            _selectedResolutionIndex = Mathf.Clamp(index, 0, _resolutionOptions.Count - 1);
            _runtime.ApplyResolution(_resolutionOptions[_selectedResolutionIndex], _selectedResolutionIndex);
            PlayerPrefs.SetInt(_resolutionKey, _selectedResolutionIndex);
            PlayerPrefs.Save();
        }

        public void SetFov(float value)
        {
            _fov = Mathf.Clamp(value, MinFov, MaxFov);
            _runtime.ApplyFov(_fov);
            PlayerPrefs.SetFloat(_fovKey, _fov);
            PlayerPrefs.Save();
        }

        public void SetGlobalVolume(float value)
        {
            _globalVolume = Mathf.Clamp01(value);
            _runtime.ApplyGlobalVolume(_globalVolume);
            PlayerPrefs.SetFloat(_globalVolumeKey, _globalVolume);
            PlayerPrefs.Save();
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            _runtime.ApplyMusicVolume(_musicVolume);
            PlayerPrefs.SetFloat(_musicVolumeKey, _musicVolume);
            PlayerPrefs.Save();
        }

        public void SetSoundsVolume(float value)
        {
            _soundsVolume = Mathf.Clamp01(value);
            _runtime.ApplySoundsVolume(_soundsVolume);
            PlayerPrefs.SetFloat(_soundsVolumeKey, _soundsVolume);
            PlayerPrefs.Save();
        }

        private void LoadOptions()
        {
            _resolutionOptions.Clear();
            _resolutionOptionLabels.Clear();

            var options = _runtime.GetAvailableResolutionOptions();
            if (options != null)
            {
                for (var i = 0; i < options.Count; i++)
                {
                    _resolutionOptions.Add(options[i]);
                    _resolutionOptionLabels.Add(options[i].ToDisplayString());
                }
            }

            if (_resolutionOptions.Count == 0)
            {
                var fallback = new EscMenuResolutionOption(1920, 1080, 60);
                _resolutionOptions.Add(fallback);
                _resolutionOptionLabels.Add(fallback.ToDisplayString());
            }
        }

        private void LoadPersistedValues()
        {
            _selectedResolutionIndex = Mathf.Clamp(
                PlayerPrefs.GetInt(_resolutionKey, 0),
                0,
                Mathf.Max(0, _resolutionOptions.Count - 1));

            _fov = Mathf.Clamp(PlayerPrefs.GetFloat(_fovKey, _runtime.GetCurrentFov()), MinFov, MaxFov);
            _globalVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(_globalVolumeKey, _runtime.GetCurrentGlobalVolume()));
            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(_musicVolumeKey, _runtime.GetCurrentMusicVolume()));
            _soundsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(_soundsVolumeKey, _runtime.GetCurrentSoundsVolume()));
        }

        private void ApplyAll()
        {
            if (_resolutionOptions.Count > 0)
            {
                _runtime.ApplyResolution(_resolutionOptions[_selectedResolutionIndex], _selectedResolutionIndex);
            }

            _runtime.ApplyFov(_fov);
            _runtime.ApplyGlobalVolume(_globalVolume);
            _runtime.ApplyMusicVolume(_musicVolume);
            _runtime.ApplySoundsVolume(_soundsVolume);
        }
    }

    public sealed class UnityEscMenuSettingsRuntime : IEscMenuSettingsRuntime
    {
        private static float s_musicVolume = 1f;
        private static float s_soundsVolume = 1f;

        public IReadOnlyList<EscMenuResolutionOption> GetAvailableResolutionOptions()
        {
            var output = new List<EscMenuResolutionOption>();
            var seen = new HashSet<string>();
            var resolutions = Screen.resolutions;
            for (var i = 0; i < resolutions.Length; i++)
            {
                var resolution = resolutions[i];
                var refreshRate = Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
                var key = resolution.width + "x" + resolution.height + "@" + refreshRate;
                if (!seen.Add(key))
                {
                    continue;
                }

                output.Add(new EscMenuResolutionOption(resolution.width, resolution.height, refreshRate));
            }

            return output;
        }

        public float GetCurrentFov()
        {
            return Camera.main != null ? Camera.main.fieldOfView : 70f;
        }

        public float GetCurrentGlobalVolume()
        {
            return AudioListener.volume;
        }

        public float GetCurrentMusicVolume()
        {
            return s_musicVolume;
        }

        public float GetCurrentSoundsVolume()
        {
            return s_soundsVolume;
        }

        public void ApplyResolution(EscMenuResolutionOption option, int selectedIndex)
        {
            Screen.SetResolution(option.Width, option.Height, Screen.fullScreenMode);
        }

        public void ApplyFov(float fov)
        {
            if (Camera.main != null)
            {
                Camera.main.fieldOfView = fov;
            }
        }

        public void ApplyGlobalVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        public void ApplyMusicVolume(float volume)
        {
            s_musicVolume = Mathf.Clamp01(volume);
        }

        public void ApplySoundsVolume(float volume)
        {
            s_soundsVolume = Mathf.Clamp01(volume);
        }
    }
}
