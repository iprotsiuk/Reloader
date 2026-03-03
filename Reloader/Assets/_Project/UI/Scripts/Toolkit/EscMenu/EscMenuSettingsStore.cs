using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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
        EscMenuResolutionOption GetCurrentResolutionOption();
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
        private const float SaveDebounceSeconds = 0.35f;
        private const string PersistenceUpdaterName = "__EscMenuSettingsStorePersistenceUpdater";

        private readonly IEscMenuSettingsRuntime _runtime;
        private readonly Action _saveAction;
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
        private bool _hasPendingSave;
        private float _nextFlushAtTime;

        private static readonly List<WeakReference<EscMenuSettingsStore>> s_storeRefs = new List<WeakReference<EscMenuSettingsStore>>();
        private static PersistenceUpdater s_persistenceUpdater;
        private static bool s_quitHookRegistered;

        public EscMenuSettingsStore(IEscMenuSettingsRuntime runtime = null, string playerPrefsKeyPrefix = "esc-menu", Action saveAction = null)
        {
            _runtime = runtime ?? new UnityEscMenuSettingsRuntime();
            _saveAction = saveAction ?? PlayerPrefs.Save;
            var prefix = string.IsNullOrWhiteSpace(playerPrefsKeyPrefix) ? "esc-menu" : playerPrefsKeyPrefix;
            _resolutionKey = prefix + ".resolution";
            _fovKey = prefix + ".fov";
            _globalVolumeKey = prefix + ".global";
            _musicVolumeKey = prefix + ".music";
            _soundsVolumeKey = prefix + ".sounds";

            RegisterStore(this);
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
            MarkPersistenceDirty();
        }

        public void SetFov(float value)
        {
            _fov = Mathf.Clamp(value, MinFov, MaxFov);
            _runtime.ApplyFov(_fov);
            PlayerPrefs.SetFloat(_fovKey, _fov);
            MarkPersistenceDirty();
        }

        public void SetGlobalVolume(float value)
        {
            _globalVolume = Mathf.Clamp01(value);
            _runtime.ApplyGlobalVolume(_globalVolume);
            PlayerPrefs.SetFloat(_globalVolumeKey, _globalVolume);
            MarkPersistenceDirty();
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            _runtime.ApplyMusicVolume(_musicVolume);
            PlayerPrefs.SetFloat(_musicVolumeKey, _musicVolume);
            MarkPersistenceDirty();
        }

        public void SetSoundsVolume(float value)
        {
            _soundsVolume = Mathf.Clamp01(value);
            _runtime.ApplySoundsVolume(_soundsVolume);
            PlayerPrefs.SetFloat(_soundsVolumeKey, _soundsVolume);
            MarkPersistenceDirty();
        }

        public void FlushPendingPersistence()
        {
            FlushPendingPersistenceInternal(ignoreDebounce: true);
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
            if (PlayerPrefs.HasKey(_resolutionKey))
            {
                _selectedResolutionIndex = Mathf.Clamp(
                    PlayerPrefs.GetInt(_resolutionKey, 0),
                    0,
                    Mathf.Max(0, _resolutionOptions.Count - 1));
            }
            else
            {
                _selectedResolutionIndex = ResolveCurrentResolutionIndex();
            }

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

        private int ResolveCurrentResolutionIndex()
        {
            if (_resolutionOptions.Count == 0)
            {
                return 0;
            }

            var current = _runtime.GetCurrentResolutionOption();
            for (var i = 0; i < _resolutionOptions.Count; i++)
            {
                var option = _resolutionOptions[i];
                if (option.Width == current.Width
                    && option.Height == current.Height
                    && option.RefreshRate == current.RefreshRate)
                {
                    return i;
                }
            }

            for (var i = 0; i < _resolutionOptions.Count; i++)
            {
                var option = _resolutionOptions[i];
                if (option.Width == current.Width && option.Height == current.Height)
                {
                    return i;
                }
            }

            return 0;
        }

        private void MarkPersistenceDirty()
        {
            var now = Time.realtimeSinceStartup;
            _nextFlushAtTime = now + SaveDebounceSeconds;
            _hasPendingSave = true;
            EnsurePersistenceUpdater();
        }

        private bool TryFlushPendingPersistence(float now)
        {
            if (!_hasPendingSave || now < _nextFlushAtTime)
            {
                return false;
            }

            return FlushPendingPersistenceInternal(ignoreDebounce: false);
        }

        private bool FlushPendingPersistenceInternal(bool ignoreDebounce)
        {
            if (!_hasPendingSave)
            {
                return false;
            }

            if (!ignoreDebounce && Time.realtimeSinceStartup < _nextFlushAtTime)
            {
                return false;
            }

            _saveAction?.Invoke();
            _hasPendingSave = false;
            return true;
        }

        private static void RegisterStore(EscMenuSettingsStore store)
        {
            if (store == null)
            {
                return;
            }

            PruneDeadStores();
            s_storeRefs.Add(new WeakReference<EscMenuSettingsStore>(store));
            EnsurePersistenceUpdater();
            EnsureQuitHook();
        }

        private static void EnsurePersistenceUpdater()
        {
            if (s_persistenceUpdater != null)
            {
                return;
            }

            var existing = GameObject.Find(PersistenceUpdaterName);
            if (existing != null)
            {
                s_persistenceUpdater = existing.GetComponent<PersistenceUpdater>();
                if (s_persistenceUpdater != null)
                {
                    return;
                }
            }

            var updaterGo = new GameObject(PersistenceUpdaterName);
            updaterGo.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(updaterGo);
            s_persistenceUpdater = updaterGo.AddComponent<PersistenceUpdater>();
        }

        private static void EnsureQuitHook()
        {
            if (s_quitHookRegistered)
            {
                return;
            }

            Application.quitting += FlushAllPendingPersistence;
            s_quitHookRegistered = true;
        }

        private static void FlushDueStores()
        {
            if (s_storeRefs.Count == 0)
            {
                return;
            }

            var now = Time.realtimeSinceStartup;
            for (var i = s_storeRefs.Count - 1; i >= 0; i--)
            {
                if (!s_storeRefs[i].TryGetTarget(out var store) || store == null)
                {
                    s_storeRefs.RemoveAt(i);
                    continue;
                }

                store.TryFlushPendingPersistence(now);
            }
        }

        private static void FlushAllPendingPersistence()
        {
            if (s_storeRefs.Count == 0)
            {
                return;
            }

            for (var i = s_storeRefs.Count - 1; i >= 0; i--)
            {
                if (!s_storeRefs[i].TryGetTarget(out var store) || store == null)
                {
                    s_storeRefs.RemoveAt(i);
                    continue;
                }

                store.FlushPendingPersistenceInternal(ignoreDebounce: true);
            }
        }

        private static void PruneDeadStores()
        {
            for (var i = s_storeRefs.Count - 1; i >= 0; i--)
            {
                if (!s_storeRefs[i].TryGetTarget(out _))
                {
                    s_storeRefs.RemoveAt(i);
                }
            }
        }

        private sealed class PersistenceUpdater : MonoBehaviour
        {
            private void LateUpdate()
            {
                FlushDueStores();
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                if (pauseStatus)
                {
                    FlushAllPendingPersistence();
                }
            }

            private void OnApplicationQuit()
            {
                FlushAllPendingPersistence();
            }
        }
    }

    public sealed class UnityEscMenuSettingsRuntime : IEscMenuSettingsRuntime
    {
        private const string AudioUpdaterName = "__EscMenuAudioScalingUpdater";
        private const float AudioRescanIntervalSeconds = 0.25f;
        private static float s_musicVolume = 1f;
        private static float s_soundsVolume = 1f;
        private static float s_nextAudioRescanAtTime;
        private static AudioScalingUpdater s_audioScalingUpdater;
        private static readonly Dictionary<int, SourceVolumeState> s_sourceVolumeStates = new Dictionary<int, SourceVolumeState>();
        private static readonly string[] s_musicTokens = { "music", "bgm", "theme", "ambientmusic", "soundtrack" };
        private static readonly string[] s_soundsTokens = { "sfx", "sound", "sounds", "ui", "voice", "vo", "fx", "effect", "effects", "gun", "shot" };

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

        public EscMenuResolutionOption GetCurrentResolutionOption()
        {
            var current = Screen.currentResolution;
            var refreshRate = Mathf.RoundToInt((float)current.refreshRateRatio.value);
            return new EscMenuResolutionOption(current.width, current.height, refreshRate);
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
            Screen.SetResolution(option.Width, option.Height, Screen.fullScreenMode, option.RefreshRate);
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
            ApplyRuntimeSourceVolumes();
            EnsureAudioScalingUpdater();
        }

        public void ApplySoundsVolume(float volume)
        {
            s_soundsVolume = Mathf.Clamp01(volume);
            ApplyRuntimeSourceVolumes();
            EnsureAudioScalingUpdater();
        }

        private static void ApplyRuntimeSourceVolumes()
        {
            var sources = FindAllAudioSources();
            if (sources == null || sources.Length == 0)
            {
                return;
            }

            var activeIds = new HashSet<int>();
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (source == null)
                {
                    continue;
                }

                var sourceId = source.GetInstanceID();
                activeIds.Add(sourceId);

                var channel = ResolveChannel(source);
                var channelVolume = channel == AudioChannel.Music ? s_musicVolume : s_soundsVolume;

                var baseVolume = ResolveBaseVolume(sourceId, source, channelVolume);
                var appliedVolume = Mathf.Clamp01(baseVolume * channelVolume);
                source.volume = appliedVolume;
                s_sourceVolumeStates[sourceId] = new SourceVolumeState(
                    unscaledBaseVolume: baseVolume,
                    lastAppliedChannelVolume: channelVolume,
                    lastAppliedOutputVolume: appliedVolume);
            }

            if (s_sourceVolumeStates.Count == 0)
            {
                return;
            }

            var staleIds = new List<int>();
            foreach (var key in s_sourceVolumeStates.Keys)
            {
                if (!activeIds.Contains(key))
                {
                    staleIds.Add(key);
                }
            }

            for (var i = 0; i < staleIds.Count; i++)
            {
                s_sourceVolumeStates.Remove(staleIds[i]);
            }
        }

        private static float ResolveBaseVolume(int sourceId, AudioSource source, float channelVolume)
        {
            if (!s_sourceVolumeStates.TryGetValue(sourceId, out var state))
            {
                return Mathf.Clamp01(source.volume);
            }

            if (Mathf.Abs(source.volume - state.LastAppliedOutputVolume) > 0.0001f)
            {
                if (state.LastAppliedChannelVolume > 0.0001f)
                {
                    return Mathf.Clamp01(source.volume / state.LastAppliedChannelVolume);
                }

                return Mathf.Clamp01(source.volume);
            }

            if (state.LastAppliedChannelVolume > 0.0001f)
            {
                return Mathf.Clamp01(source.volume / state.LastAppliedChannelVolume);
            }

            return state.UnscaledBaseVolume;
        }

        private static void EnsureAudioScalingUpdater()
        {
            if (s_audioScalingUpdater != null)
            {
                return;
            }

            var existing = GameObject.Find(AudioUpdaterName);
            if (existing != null)
            {
                s_audioScalingUpdater = existing.GetComponent<AudioScalingUpdater>();
                if (s_audioScalingUpdater != null)
                {
                    return;
                }
            }

            var updaterGo = new GameObject(AudioUpdaterName);
            updaterGo.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(updaterGo);
            s_audioScalingUpdater = updaterGo.AddComponent<AudioScalingUpdater>();
        }

        private static void RefreshRuntimeSourcesIfNeeded()
        {
            if (Mathf.Approximately(s_musicVolume, 1f) && Mathf.Approximately(s_soundsVolume, 1f))
            {
                return;
            }

            var now = Time.realtimeSinceStartup;
            if (now < s_nextAudioRescanAtTime)
            {
                return;
            }

            s_nextAudioRescanAtTime = now + AudioRescanIntervalSeconds;
            ApplyRuntimeSourceVolumes();
        }

        private static AudioSource[] FindAllAudioSources()
        {
#if UNITY_2023_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<AudioSource>(true);
#endif
        }

        private static AudioChannel ResolveChannel(AudioSource source)
        {
            if (source == null)
            {
                return AudioChannel.Sounds;
            }

            var mixerName = GetMixerGroupName(source.outputAudioMixerGroup);
            if (ContainsAnyToken(mixerName, s_musicTokens))
            {
                return AudioChannel.Music;
            }

            if (ContainsAnyToken(mixerName, s_soundsTokens))
            {
                return AudioChannel.Sounds;
            }

            if (ContainsAnyToken(source.name, s_musicTokens) || ContainsAnyToken(source.gameObject.name, s_musicTokens))
            {
                return AudioChannel.Music;
            }

            if (ContainsAnyToken(source.name, s_soundsTokens) || ContainsAnyToken(source.gameObject.name, s_soundsTokens))
            {
                return AudioChannel.Sounds;
            }

            return AudioChannel.Sounds;
        }

        private static bool ContainsAnyToken(string value, string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(value) || tokens == null || tokens.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetMixerGroupName(AudioMixerGroup mixerGroup)
        {
            return mixerGroup != null ? mixerGroup.name : string.Empty;
        }

        private enum AudioChannel
        {
            Music,
            Sounds
        }

        private readonly struct SourceVolumeState
        {
            public SourceVolumeState(float unscaledBaseVolume, float lastAppliedChannelVolume, float lastAppliedOutputVolume)
            {
                UnscaledBaseVolume = Mathf.Clamp01(unscaledBaseVolume);
                LastAppliedChannelVolume = Mathf.Clamp01(lastAppliedChannelVolume);
                LastAppliedOutputVolume = Mathf.Clamp01(lastAppliedOutputVolume);
            }

            public float UnscaledBaseVolume { get; }
            public float LastAppliedChannelVolume { get; }
            public float LastAppliedOutputVolume { get; }
        }

        private sealed class AudioScalingUpdater : MonoBehaviour
        {
            private void LateUpdate()
            {
                RefreshRuntimeSourcesIfNeeded();
            }
        }
    }
}
