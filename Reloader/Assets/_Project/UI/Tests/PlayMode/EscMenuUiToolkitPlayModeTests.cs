using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.EscMenu;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class EscMenuUiToolkitPlayModeTests
    {
        [Test]
        public void EscMenuController_Tick_EscapeOpensOnlyWhenNoOtherMenuOpen()
        {
            var go = new GameObject("esc-menu-controller");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var uiStateEvents = new TestUiStateEvents();
            var keySource = new TestEscKeySource();

            var controller = go.AddComponent<EscMenuController>();
            controller.Configure(uiStateEvents);
            controller.SetEscKeySource(keySource);
            controller.SetViewBinder(binder);

            var panel = root.Q<VisualElement>("esc-menu__panel");
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            uiStateEvents.IsTabInventoryVisible = true;
            keySource.PressedThisFrame = true;
            controller.Tick();

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            uiStateEvents.IsTabInventoryVisible = false;
            keySource.PressedThisFrame = true;
            controller.Tick();

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EscMenuController_Tick_EscapeAfterOtherMenuClosedSameFrame_DoesNotOpenEscMenu()
        {
            var go = new GameObject("esc-menu-controller-late-close");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var uiStateEvents = new TestUiStateEvents();
            var keySource = new TestEscKeySource();

            var controller = go.AddComponent<EscMenuController>();
            controller.Configure(uiStateEvents);
            controller.SetEscKeySource(keySource);
            controller.SetViewBinder(binder);

            // Frame N: another menu is open.
            uiStateEvents.IsTabInventoryVisible = true;
            controller.Tick();

            // Frame N+1: another controller closed that menu via Escape before ESC menu Tick.
            uiStateEvents.IsTabInventoryVisible = false;
            keySource.PressedThisFrame = true;
            controller.Tick();

            var panel = root.Q<VisualElement>("esc-menu__panel");
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            // Next frame Escape can open ESC menu normally.
            keySource.PressedThisFrame = true;
            controller.Tick();
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EscMenuController_Tick_StorageMenuOpen_DoesNotOpenEscMenu()
        {
            var go = new GameObject("esc-menu-controller-storage-open");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var keySource = new TestEscKeySource();
            var controller = go.AddComponent<EscMenuController>();
            controller.SetEscKeySource(keySource);
            controller.SetViewBinder(binder);

            StorageUiSession.Open("storage-test");
            keySource.PressedThisFrame = true;
            controller.Tick();

            var panel = root.Q<VisualElement>("esc-menu__panel");
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            StorageUiSession.Close();
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EscMenuController_Tick_EscapeClosesWhenOpen()
        {
            var go = new GameObject("esc-menu-controller-close");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var uiStateEvents = new TestUiStateEvents();
            var keySource = new TestEscKeySource();

            var controller = go.AddComponent<EscMenuController>();
            controller.Configure(uiStateEvents);
            controller.SetEscKeySource(keySource);
            controller.SetViewBinder(binder);

            keySource.PressedThisFrame = true;
            controller.Tick();

            var panel = root.Q<VisualElement>("esc-menu__panel");
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            keySource.PressedThisFrame = true;
            controller.Tick();

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EscMenuController_HandleIntent_ResumeClosesMenu()
        {
            var go = new GameObject("esc-menu-controller-resume");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var keySource = new TestEscKeySource { PressedThisFrame = true };
            var controller = go.AddComponent<EscMenuController>();
            controller.SetEscKeySource(keySource);
            controller.SetViewBinder(binder);
            controller.Tick();

            var panel = root.Q<VisualElement>("esc-menu__panel");
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            controller.HandleIntent(new UiIntent("esc.menu.resume"));

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EscMenuViewBinder_Render_ShowsSettingsVideoTabAndAppliesControlValues()
        {
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            binder.Render(EscMenuUiState.Create(
                isOpen: true,
                screen: EscMenuScreen.Settings,
                settingsTab: EscMenuSettingsTab.Video,
                resolutionOptions: new[] { "1280x720", "1920x1080" },
                selectedResolutionIndex: 1,
                fov: 87f,
                globalVolume: 0.8f,
                musicVolume: 0.2f,
                soundsVolume: 0.4f));

            var settingsScreen = root.Q<VisualElement>("esc-menu__screen-settings");
            var videoPanel = root.Q<VisualElement>("esc-menu__tab-panel-video");
            var audioPanel = root.Q<VisualElement>("esc-menu__tab-panel-audio");
            var resolutionDropdown = root.Q<DropdownField>("esc-menu__resolution");
            var fovSlider = root.Q<Slider>("esc-menu__fov");

            Assert.That(settingsScreen.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(videoPanel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(audioPanel.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(resolutionDropdown.index, Is.EqualTo(1));
            Assert.That(fovSlider.value, Is.EqualTo(87f).Within(0.001f));
        }

        [Test]
        public void EscMenuSettingsStore_SettingChangesApplyImmediatelyAndPersist()
        {
            var runtime = new TestSettingsRuntime();
            var prefsKeyPrefix = "esc-menu-tests";
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".resolution");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".fov");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".global");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".music");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".sounds");

            var store = new EscMenuSettingsStore(runtime, prefsKeyPrefix);
            store.SetSelectedResolutionIndex(1);
            store.SetFov(95f);
            store.SetGlobalVolume(0.33f);
            store.SetMusicVolume(0.44f);
            store.SetSoundsVolume(0.55f);

            Assert.That(runtime.LastResolutionIndex, Is.EqualTo(1));
            Assert.That(runtime.Fov, Is.EqualTo(95f).Within(0.001f));
            Assert.That(runtime.GlobalVolume, Is.EqualTo(0.33f).Within(0.001f));
            Assert.That(runtime.MusicVolume, Is.EqualTo(0.44f).Within(0.001f));
            Assert.That(runtime.SoundsVolume, Is.EqualTo(0.55f).Within(0.001f));

            var reloaded = new EscMenuSettingsStore(runtime, prefsKeyPrefix);
            var snapshot = reloaded.CreateSnapshot();
            Assert.That(snapshot.SelectedResolutionIndex, Is.EqualTo(1));
            Assert.That(snapshot.Fov, Is.EqualTo(95f).Within(0.001f));
            Assert.That(snapshot.GlobalVolume, Is.EqualTo(0.33f).Within(0.001f));
            Assert.That(snapshot.MusicVolume, Is.EqualTo(0.44f).Within(0.001f));
            Assert.That(snapshot.SoundsVolume, Is.EqualTo(0.55f).Within(0.001f));
        }

        [Test]
        public void EscMenuSettingsStore_NoStoredResolution_UsesCurrentResolutionIndex()
        {
            var runtime = new TestSettingsRuntime
            {
                CurrentResolutionIndex = 1
            };
            var prefsKeyPrefix = "esc-menu-tests-current-resolution";
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".resolution");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".fov");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".global");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".music");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".sounds");

            var store = new EscMenuSettingsStore(runtime, prefsKeyPrefix);
            var snapshot = store.CreateSnapshot();
            Assert.That(snapshot.SelectedResolutionIndex, Is.EqualTo(1));
            Assert.That(runtime.LastResolutionIndex, Is.EqualTo(1));
        }

        [Test]
        public void UnityEscMenuSettingsRuntime_ApplyMusicVolume_UpdatesLiveMusicChannelAudioSource()
        {
            var runtime = new UnityEscMenuSettingsRuntime();
            runtime.ApplyMusicVolume(1f);
            runtime.ApplySoundsVolume(1f);

            var musicRoot = new GameObject("music-channel-root");
            var soundsRoot = new GameObject("sounds-channel-root");
            var musicSource = musicRoot.AddComponent<AudioSource>();
            var soundsSource = soundsRoot.AddComponent<AudioSource>();
            musicSource.volume = 0.91f;
            soundsSource.volume = 0.73f;

            try
            {
                runtime.ApplyMusicVolume(0.25f);

                Assert.That(musicSource.volume, Is.EqualTo(0.25f).Within(0.001f));
                Assert.That(soundsSource.volume, Is.EqualTo(0.73f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(musicRoot);
                UnityEngine.Object.DestroyImmediate(soundsRoot);
            }
        }

        [Test]
        public void UnityEscMenuSettingsRuntime_ApplySoundsVolume_UpdatesLiveSoundsChannelAudioSource()
        {
            var runtime = new UnityEscMenuSettingsRuntime();
            runtime.ApplyMusicVolume(1f);
            runtime.ApplySoundsVolume(1f);

            var musicRoot = new GameObject("music-channel-root");
            var soundsRoot = new GameObject("sounds-channel-root");
            var musicSource = musicRoot.AddComponent<AudioSource>();
            var soundsSource = soundsRoot.AddComponent<AudioSource>();
            musicSource.volume = 0.66f;
            soundsSource.volume = 0.88f;

            try
            {
                runtime.ApplySoundsVolume(0.4f);

                Assert.That(musicSource.volume, Is.EqualTo(0.66f).Within(0.001f));
                Assert.That(soundsSource.volume, Is.EqualTo(0.4f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(musicRoot);
                UnityEngine.Object.DestroyImmediate(soundsRoot);
            }
        }

        private static VisualElement BuildEscRoot()
        {
            var root = new VisualElement { name = "esc-menu__root" };
            var panel = new VisualElement { name = "esc-menu__panel" };
            root.Add(panel);

            var mainScreen = new VisualElement { name = "esc-menu__screen-main" };
            mainScreen.Add(new Button { name = "esc-menu__resume" });
            mainScreen.Add(new Button { name = "esc-menu__settings" });
            mainScreen.Add(new Button { name = "esc-menu__keybindings" });
            mainScreen.Add(new Button { name = "esc-menu__quit" });
            panel.Add(mainScreen);

            var settingsScreen = new VisualElement { name = "esc-menu__screen-settings" };
            settingsScreen.Add(new Button { name = "esc-menu__settings-back" });
            settingsScreen.Add(new Button { name = "esc-menu__tab-game" });
            settingsScreen.Add(new Button { name = "esc-menu__tab-video" });
            settingsScreen.Add(new Button { name = "esc-menu__tab-audio" });

            settingsScreen.Add(new VisualElement { name = "esc-menu__tab-panel-game" });
            settingsScreen.Add(new VisualElement { name = "esc-menu__tab-panel-video" });
            settingsScreen.Add(new VisualElement { name = "esc-menu__tab-panel-audio" });

            settingsScreen.Add(new DropdownField { name = "esc-menu__resolution" });
            settingsScreen.Add(new Slider { name = "esc-menu__fov", lowValue = 50f, highValue = 110f });
            settingsScreen.Add(new Slider { name = "esc-menu__global-volume", lowValue = 0f, highValue = 1f });
            settingsScreen.Add(new Slider { name = "esc-menu__music-volume", lowValue = 0f, highValue = 1f });
            settingsScreen.Add(new Slider { name = "esc-menu__sounds-volume", lowValue = 0f, highValue = 1f });
            panel.Add(settingsScreen);

            var keyBindingsScreen = new VisualElement { name = "esc-menu__screen-keybindings" };
            keyBindingsScreen.Add(new Button { name = "esc-menu__keybindings-back" });
            panel.Add(keyBindingsScreen);

            return root;
        }

        private sealed class TestEscKeySource : IEscMenuKeySource
        {
            public bool PressedThisFrame;

            public bool ConsumeEscapePressedThisFrame()
            {
                if (!PressedThisFrame)
                {
                    return false;
                }

                PressedThisFrame = false;
                return true;
            }
        }

        private sealed class TestUiStateEvents : IUiStateEvents
        {
            public bool IsShopTradeMenuOpen { get; set; }
            public bool IsWorkbenchMenuVisible { get; set; }
            public bool IsTabInventoryVisible { get; set; }
            public bool IsEscMenuVisible { get; set; }
            public bool IsAnyMenuOpen => IsShopTradeMenuOpen || IsWorkbenchMenuVisible || IsTabInventoryVisible || IsEscMenuVisible;

            public event Action<bool> OnWorkbenchMenuVisibilityChanged;
            public event Action<bool> OnTabInventoryVisibilityChanged;
            public event Action<bool> OnEscMenuVisibilityChanged;

            public void RaiseWorkbenchMenuVisibilityChanged(bool isVisible)
            {
                IsWorkbenchMenuVisible = isVisible;
                OnWorkbenchMenuVisibilityChanged?.Invoke(isVisible);
            }

            public void RaiseTabInventoryVisibilityChanged(bool isVisible)
            {
                IsTabInventoryVisible = isVisible;
                OnTabInventoryVisibilityChanged?.Invoke(isVisible);
            }

            public void RaiseEscMenuVisibilityChanged(bool isVisible)
            {
                IsEscMenuVisible = isVisible;
                OnEscMenuVisibilityChanged?.Invoke(isVisible);
            }
        }

        private sealed class TestSettingsRuntime : IEscMenuSettingsRuntime
        {
            private readonly EscMenuResolutionOption[] _resolutions =
            {
                new EscMenuResolutionOption(1280, 720, 60),
                new EscMenuResolutionOption(1920, 1080, 60)
            };

            public int LastResolutionIndex { get; private set; }
            public int CurrentResolutionIndex { get; set; }
            public float Fov { get; private set; } = 70f;
            public float GlobalVolume { get; private set; } = 1f;
            public float MusicVolume { get; private set; } = 1f;
            public float SoundsVolume { get; private set; } = 1f;

            public IReadOnlyList<EscMenuResolutionOption> GetAvailableResolutionOptions() => _resolutions;
            public EscMenuResolutionOption GetCurrentResolutionOption() => _resolutions[Mathf.Clamp(CurrentResolutionIndex, 0, _resolutions.Length - 1)];
            public float GetCurrentFov() => Fov;
            public float GetCurrentGlobalVolume() => GlobalVolume;
            public float GetCurrentMusicVolume() => MusicVolume;
            public float GetCurrentSoundsVolume() => SoundsVolume;

            public void ApplyResolution(EscMenuResolutionOption option, int selectedIndex)
            {
                LastResolutionIndex = selectedIndex;
            }

            public void ApplyFov(float fov)
            {
                Fov = fov;
            }

            public void ApplyGlobalVolume(float volume)
            {
                GlobalVolume = volume;
            }

            public void ApplyMusicVolume(float volume)
            {
                MusicVolume = volume;
            }

            public void ApplySoundsVolume(float volume)
            {
                SoundsVolume = volume;
            }
        }
    }
}
