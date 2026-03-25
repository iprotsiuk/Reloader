using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.UI.Toolkit.Contracts;
using Reloader.UI.Toolkit.EscMenu;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class EscMenuUiToolkitPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            ResetConsumedEscapeFrame();
        }

        [TearDown]
        public void TearDown()
        {
            ResetConsumedEscapeFrame();
        }

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
            controller.Tick();

            ResetConsumedEscapeFrame();
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
            ResetConsumedEscapeFrame();
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

            ResetConsumedEscapeFrame();
            keySource.PressedThisFrame = true;
            controller.Tick();

            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EscMenuController_Tick_EscapeClose_DoesNotLetCursorLockProcessSamePress()
        {
            var previousLockState = UnityEngine.Cursor.lockState;
            var previousVisible = UnityEngine.Cursor.visible;
            var escMenuGo = new GameObject("esc-menu-controller-close-guard");
            var cursorGo = new GameObject("cursor-lock-controller-close-guard");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var uiStateEvents = new TestUiStateEvents();
            var escKeySource = new TestEscKeySource();
            var cursorEscapeSource = new TestCursorEscapeKeySource();

            var escController = escMenuGo.AddComponent<EscMenuController>();
            escController.Configure(uiStateEvents);
            escController.SetEscKeySource(escKeySource);
            escController.SetViewBinder(binder);

            var cursorController = cursorGo.AddComponent<PlayerCursorLockController>();
            cursorController.Configure(uiStateEvents);
            cursorController.SetEscapeKeySource(cursorEscapeSource);
            cursorController.LockCursor();

            escKeySource.PressedThisFrame = true;
            escController.Tick();
            Assert.That(uiStateEvents.IsEscMenuVisible, Is.True);

            ResetConsumedEscapeFrame();
            escKeySource.PressedThisFrame = true;
            escController.Tick();
            Assert.That(uiStateEvents.IsEscMenuVisible, Is.False);

            cursorEscapeSource.PressedThisFrame = true;
            cursorController.SendMessage("Update");

            Assert.That(cursorController.IsCursorLockRequested, Is.True);

            UnityEngine.Object.DestroyImmediate(cursorGo);
            UnityEngine.Object.DestroyImmediate(escMenuGo);
            UnityEngine.Cursor.lockState = previousLockState;
            UnityEngine.Cursor.visible = previousVisible;
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
                lookSensitivity: 1.2f,
                adsSensitivity: 0.85f,
                scopedPipResolutionPercent: 135,
                peripheralBlurPercent: 65,
                globalVolume: 0.8f,
                musicVolume: 0.2f,
                soundsVolume: 0.4f));

            var settingsScreen = root.Q<VisualElement>("esc-menu__screen-settings");
            var videoPanel = root.Q<VisualElement>("esc-menu__tab-panel-video");
            var audioPanel = root.Q<VisualElement>("esc-menu__tab-panel-audio");
            var resolutionDropdown = root.Q<DropdownField>("esc-menu__resolution");
            var fovSlider = root.Q<Slider>("esc-menu__fov");
            var scopedPipResolutionSlider = root.Q<Slider>("esc-menu__scoped-pip-resolution");
            var peripheralBlurSlider = root.Q<Slider>("esc-menu__peripheral-blur");

            Assert.That(settingsScreen.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(videoPanel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(audioPanel.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(resolutionDropdown.index, Is.EqualTo(1));
            Assert.That(fovSlider.value, Is.EqualTo(87f).Within(0.001f));
            Assert.That(scopedPipResolutionSlider.value, Is.EqualTo(135f).Within(0.001f));
            Assert.That(peripheralBlurSlider.value, Is.EqualTo(65f).Within(0.001f));
        }

        [Test]
        public void EscMenuViewBinder_Render_TogglesDocumentRootPickingWithOpenState()
        {
            var documentRoot = new VisualElement { name = "document-root" };
            var escRoot = BuildEscRoot();
            documentRoot.Add(escRoot);

            var binder = new EscMenuViewBinder();
            binder.Initialize(documentRoot);

            binder.Render(EscMenuUiState.Create(
                isOpen: true,
                screen: EscMenuScreen.Main,
                settingsTab: EscMenuSettingsTab.Game,
                resolutionOptions: Array.Empty<string>(),
                selectedResolutionIndex: 0,
                fov: 80f,
                lookSensitivity: 1f,
                adsSensitivity: 1f,
                scopedPipResolutionPercent: 100,
                peripheralBlurPercent: 50,
                globalVolume: 1f,
                musicVolume: 1f,
                soundsVolume: 1f));

            Assert.That(documentRoot.pickingMode, Is.EqualTo(PickingMode.Position));

            binder.Render(EscMenuUiState.Create(
                isOpen: false,
                screen: EscMenuScreen.Main,
                settingsTab: EscMenuSettingsTab.Game,
                resolutionOptions: Array.Empty<string>(),
                selectedResolutionIndex: 0,
                fov: 80f,
                lookSensitivity: 1f,
                adsSensitivity: 1f,
                scopedPipResolutionPercent: 100,
                peripheralBlurPercent: 50,
                globalVolume: 1f,
                musicVolume: 1f,
                soundsVolume: 1f));

            Assert.That(documentRoot.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        [Test]
        public void EscMenuSettingsStore_SettingChangesApplyImmediatelyAndPersist()
        {
            var runtime = new TestSettingsRuntime();
            var prefsKeyPrefix = "esc-menu-tests";
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".resolution");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".fov");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".scoped-pip-resolution-percent");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".peripheral-blur-percent");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".global");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".music");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".sounds");

            var store = new EscMenuSettingsStore(runtime, prefsKeyPrefix);
            store.SetSelectedResolutionIndex(1);
            store.SetFov(95f);
            store.SetScopedPipResolutionPercent(175);
            store.SetPeripheralBlurPercent(70);
            store.SetGlobalVolume(0.33f);
            store.SetMusicVolume(0.44f);
            store.SetSoundsVolume(0.55f);

            Assert.That(runtime.LastResolutionIndex, Is.EqualTo(1));
            Assert.That(runtime.Fov, Is.EqualTo(95f).Within(0.001f));
            Assert.That(store.GetScopedPipResolutionPercent(), Is.EqualTo(175));
            Assert.That(store.GetPeripheralBlurPercent(), Is.EqualTo(70));
            Assert.That(runtime.GlobalVolume, Is.EqualTo(0.33f).Within(0.001f));
            Assert.That(runtime.MusicVolume, Is.EqualTo(0.44f).Within(0.001f));
            Assert.That(runtime.SoundsVolume, Is.EqualTo(0.55f).Within(0.001f));

            var reloaded = new EscMenuSettingsStore(runtime, prefsKeyPrefix);
            var snapshot = reloaded.CreateSnapshot();
            Assert.That(snapshot.SelectedResolutionIndex, Is.EqualTo(1));
            Assert.That(snapshot.Fov, Is.EqualTo(95f).Within(0.001f));
            Assert.That(snapshot.ScopedPipResolutionPercent, Is.EqualTo(175));
            Assert.That(snapshot.PeripheralBlurPercent, Is.EqualTo(70));
            Assert.That(snapshot.GlobalVolume, Is.EqualTo(0.33f).Within(0.001f));
            Assert.That(snapshot.MusicVolume, Is.EqualTo(0.44f).Within(0.001f));
            Assert.That(snapshot.SoundsVolume, Is.EqualTo(0.55f).Within(0.001f));
        }

        [Test]
        public void EscMenuSettingsStore_SensitivityChangesApplyImmediatelyAndPersist()
        {
            var runtime = new TestSettingsRuntime();
            var prefsKeyPrefix = "esc-menu-tests-sensitivity";
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".look-sensitivity");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".ads-sensitivity");

            var store = new EscMenuSettingsStore(runtime, prefsKeyPrefix);
            InvokeSingleFloatMethod(store, "SetLookSensitivity", 1.6f);
            InvokeSingleFloatMethod(store, "SetAdsSensitivity", 0.7f);

            Assert.That(GetSingleFloatProperty(runtime, "LastAppliedLookSensitivity"), Is.EqualTo(1.6f).Within(0.001f));
            Assert.That(GetSingleFloatProperty(runtime, "LastAppliedAdsSensitivity"), Is.EqualTo(0.7f).Within(0.001f));

            var snapshot = store.CreateSnapshot();
            Assert.That(GetSingleFloatProperty(snapshot, "LookSensitivity"), Is.EqualTo(1.6f).Within(0.001f));
            Assert.That(GetSingleFloatProperty(snapshot, "AdsSensitivity"), Is.EqualTo(0.7f).Within(0.001f));

            var reloaded = new EscMenuSettingsStore(runtime, prefsKeyPrefix);
            var reloadedSnapshot = reloaded.CreateSnapshot();
            Assert.That(GetSingleFloatProperty(reloadedSnapshot, "LookSensitivity"), Is.EqualTo(1.6f).Within(0.001f));
            Assert.That(GetSingleFloatProperty(reloadedSnapshot, "AdsSensitivity"), Is.EqualTo(0.7f).Within(0.001f));
        }

        [Test]
        public void EscMenuSettingsStore_SettingChanges_DoNotSaveSynchronouslyAndCanBeFlushedExplicitly()
        {
            var runtime = new TestSettingsRuntime();
            var prefsKeyPrefix = "esc-menu-tests-deferred-save";
            var saveCalls = 0;
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".resolution");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".fov");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".scoped-pip-resolution-percent");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".peripheral-blur-percent");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".global");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".music");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".sounds");

            var store = new EscMenuSettingsStore(runtime, prefsKeyPrefix, () => saveCalls++);
            store.SetFov(92f);
            store.SetScopedPipResolutionPercent(120);
            store.SetPeripheralBlurPercent(45);
            store.SetMusicVolume(0.2f);
            store.SetSoundsVolume(0.7f);

            Assert.That(saveCalls, Is.EqualTo(0));

            store.FlushPendingPersistence();
            Assert.That(saveCalls, Is.EqualTo(1));

            store.FlushPendingPersistence();
            Assert.That(saveCalls, Is.EqualTo(1));
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
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".scoped-pip-resolution-percent");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".peripheral-blur-percent");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".global");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".music");
            PlayerPrefs.DeleteKey(prefsKeyPrefix + ".sounds");

            var store = new EscMenuSettingsStore(runtime, prefsKeyPrefix);
            var snapshot = store.CreateSnapshot();
            Assert.That(snapshot.SelectedResolutionIndex, Is.EqualTo(1));
            Assert.That(runtime.LastResolutionIndex, Is.EqualTo(1));
            Assert.That(snapshot.ScopedPipResolutionPercent, Is.EqualTo(ScopedOpticsSettings.DefaultPipResolutionPercent));
            Assert.That(snapshot.PeripheralBlurPercent, Is.EqualTo(ScopedOpticsSettings.DefaultPeripheralBlurPercent));
        }

        [Test]
        public void EscMenuController_HandleIntent_VideoScopedOpticsChangesUpdateRenderedSliders()
        {
            var go = new GameObject("esc-menu-controller-scoped-optics");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var controller = go.AddComponent<EscMenuController>();
            controller.SetViewBinder(binder);

            controller.HandleIntent(new UiIntent("esc.menu.settings"));
            controller.HandleIntent(new UiIntent("esc.menu.settings.tab", "video"));
            controller.HandleIntent(new UiIntent("esc.menu.settings.video.scoped-pip-resolution.changed", 180f));
            controller.HandleIntent(new UiIntent("esc.menu.settings.video.peripheral-blur.changed", 72f));

            var scopedPipResolutionSlider = root.Q<Slider>("esc-menu__scoped-pip-resolution");
            var peripheralBlurSlider = root.Q<Slider>("esc-menu__peripheral-blur");

            Assert.That(scopedPipResolutionSlider.value, Is.EqualTo(180f).Within(0.001f));
            Assert.That(peripheralBlurSlider.value, Is.EqualTo(72f).Within(0.001f));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EscMenuController_HandleIntent_GameSensitivityChangesUpdateRenderedSliders()
        {
            var go = new GameObject("esc-menu-controller-sensitivity");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var controller = go.AddComponent<EscMenuController>();
            controller.SetViewBinder(binder);

            controller.HandleIntent(new UiIntent("esc.menu.settings"));
            controller.HandleIntent(new UiIntent("esc.menu.settings.tab", "game"));
            controller.HandleIntent(new UiIntent("esc.menu.settings.game.look-sensitivity.changed", 1.6f));
            controller.HandleIntent(new UiIntent("esc.menu.settings.game.ads-sensitivity.changed", 0.7f));

            var lookSensitivitySlider = root.Q<Slider>("esc-menu__look-sensitivity");
            var adsSensitivitySlider = root.Q<Slider>("esc-menu__ads-sensitivity");

            Assert.That(lookSensitivitySlider, Is.Not.Null);
            Assert.That(adsSensitivitySlider, Is.Not.Null);
            Assert.That(lookSensitivitySlider.value, Is.EqualTo(1.6f).Within(0.001f));
            Assert.That(adsSensitivitySlider.value, Is.EqualTo(0.7f).Within(0.001f));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void EscMenuViewBinder_GameSensitivitySliders_RaiseExpectedIntents()
        {
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            UiIntent? lookIntent = null;
            UiIntent? adsIntent = null;
            binder.IntentRaised += intent =>
            {
                if (intent.Key == "esc.menu.settings.game.look-sensitivity.changed")
                {
                    lookIntent = intent;
                }

                if (intent.Key == "esc.menu.settings.game.ads-sensitivity.changed")
                {
                    adsIntent = intent;
                }
            };

            var lookSensitivitySlider = root.Q<Slider>("esc-menu__look-sensitivity");
            var adsSensitivitySlider = root.Q<Slider>("esc-menu__ads-sensitivity");
            InvokePrivateChangeEventHandler(binder, "HandleLookSensitivityChanged", 0.1f, 1.6f);
            InvokePrivateChangeEventHandler(binder, "HandleAdsSensitivityChanged", 1.6f, 0.7f);

            Assert.That(lookIntent.HasValue, Is.True);
            Assert.That(adsIntent.HasValue, Is.True);
            Assert.That(Convert.ToSingle(lookIntent.Value.Payload), Is.EqualTo(1.6f).Within(0.001f));
            Assert.That(Convert.ToSingle(adsIntent.Value.Payload), Is.EqualTo(0.7f).Within(0.001f));
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

                Assert.That(musicSource.volume, Is.EqualTo(0.2275f).Within(0.001f));
                Assert.That(soundsSource.volume, Is.EqualTo(0.73f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(musicRoot);
                UnityEngine.Object.DestroyImmediate(soundsRoot);
            }
        }

        [Test]
        public void EscMenuController_Tick_ConsumedEscapeDoesNotOpenMenu()
        {
            var go = new GameObject("esc-menu-controller-consumed-escape");
            var root = BuildEscRoot();
            var binder = new EscMenuViewBinder();
            binder.Initialize(root);

            var uiStateEvents = new TestUiStateEvents();
            var keySource = new TestEscKeySource { PressedThisFrame = true };

            var controller = go.AddComponent<EscMenuController>();
            controller.Configure(uiStateEvents);
            controller.SetEscKeySource(keySource);
            controller.SetViewBinder(binder);

            PlayerCursorLockController.MarkEscapeConsumedThisFrame();
            controller.Tick();

            var panel = root.Q<VisualElement>("esc-menu__panel");
            Assert.That(uiStateEvents.IsEscMenuVisible, Is.False);
            Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.None));

            UnityEngine.Object.DestroyImmediate(go);
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
                Assert.That(soundsSource.volume, Is.EqualTo(0.352f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(musicRoot);
                UnityEngine.Object.DestroyImmediate(soundsRoot);
            }
        }

        [UnityTest]
        public IEnumerator UnityEscMenuSettingsRuntime_AppliesChannelScalingToAudioSourcesCreatedAfterSliderChange()
        {
            var runtime = new UnityEscMenuSettingsRuntime();
            runtime.ApplyMusicVolume(0.25f);
            runtime.ApplySoundsVolume(0.4f);

            var lateMusicRoot = new GameObject("late-music-channel-root");
            var lateSoundsRoot = new GameObject("late-sounds-channel-root");
            var lateMusicSource = lateMusicRoot.AddComponent<AudioSource>();
            var lateSoundsSource = lateSoundsRoot.AddComponent<AudioSource>();
            lateMusicSource.volume = 0.8f;
            lateSoundsSource.volume = 0.5f;

            try
            {
                yield return new WaitForSecondsRealtime(0.35f);

                Assert.That(lateMusicSource.volume, Is.EqualTo(0.2f).Within(0.02f));
                Assert.That(lateSoundsSource.volume, Is.EqualTo(0.2f).Within(0.02f));
            }
            finally
            {
                runtime.ApplyMusicVolume(1f);
                runtime.ApplySoundsVolume(1f);
                UnityEngine.Object.DestroyImmediate(lateMusicRoot);
                UnityEngine.Object.DestroyImmediate(lateSoundsRoot);
            }
        }

        [UnityTest]
        public IEnumerator UnityEscMenuSettingsRuntime_Rescan_DoesNotOverwriteRuntimeVolumeChanges()
        {
            var runtime = new UnityEscMenuSettingsRuntime();
            runtime.ApplyMusicVolume(1f);
            runtime.ApplySoundsVolume(0.5f);

            var soundsRoot = new GameObject("rescan-sounds-channel-root");
            var soundsSource = soundsRoot.AddComponent<AudioSource>();
            soundsSource.volume = 1f;

            try
            {
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(soundsSource.volume, Is.EqualTo(0.5f).Within(0.02f));

                // Simulate runtime systems (fade/ducking) changing the source volume while channel scaling is active.
                soundsSource.volume = 0.2f;
                yield return new WaitForSecondsRealtime(0.35f);

                Assert.That(soundsSource.volume, Is.EqualTo(0.2f).Within(0.02f));
            }
            finally
            {
                runtime.ApplyMusicVolume(1f);
                runtime.ApplySoundsVolume(1f);
                UnityEngine.Object.DestroyImmediate(soundsRoot);
            }
        }

        [UnityTest]
        public IEnumerator UnityEscMenuSettingsRuntime_MutedChannel_PreservesRuntimeVolumeChangesForUnmute()
        {
            var runtime = new UnityEscMenuSettingsRuntime();
            runtime.ApplyMusicVolume(1f);
            runtime.ApplySoundsVolume(0f);

            var soundsRoot = new GameObject("muted-sounds-channel-root");
            var soundsSource = soundsRoot.AddComponent<AudioSource>();
            soundsSource.volume = 1f;

            try
            {
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(soundsSource.volume, Is.EqualTo(0f).Within(0.02f));

                soundsSource.volume = 0.3f;
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(soundsSource.volume, Is.EqualTo(0f).Within(0.02f));

                runtime.ApplySoundsVolume(1f);
                yield return null;
                Assert.That(soundsSource.volume, Is.EqualTo(0.3f).Within(0.02f));
            }
            finally
            {
                runtime.ApplyMusicVolume(1f);
                runtime.ApplySoundsVolume(1f);
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

            var gameTabPanel = new VisualElement { name = "esc-menu__tab-panel-game" };
            gameTabPanel.Add(new Slider { name = "esc-menu__look-sensitivity", lowValue = 0.1f, highValue = 2f });
            gameTabPanel.Add(new Slider { name = "esc-menu__ads-sensitivity", lowValue = 0.1f, highValue = 2f });
            settingsScreen.Add(gameTabPanel);
            settingsScreen.Add(new VisualElement { name = "esc-menu__tab-panel-video" });
            settingsScreen.Add(new VisualElement { name = "esc-menu__tab-panel-audio" });

            settingsScreen.Add(new DropdownField { name = "esc-menu__resolution" });
            settingsScreen.Add(new Slider { name = "esc-menu__fov", lowValue = 50f, highValue = 110f });
            settingsScreen.Add(new Slider
            {
                name = "esc-menu__scoped-pip-resolution",
                lowValue = ScopedOpticsSettings.MinPipResolutionPercent,
                highValue = ScopedOpticsSettings.MaxPipResolutionPercent
            });
            settingsScreen.Add(new Slider
            {
                name = "esc-menu__peripheral-blur",
                lowValue = ScopedOpticsSettings.MinPeripheralBlurPercent,
                highValue = ScopedOpticsSettings.MaxPeripheralBlurPercent
            });
            settingsScreen.Add(new Slider { name = "esc-menu__global-volume", lowValue = 0f, highValue = 1f });
            settingsScreen.Add(new Slider { name = "esc-menu__music-volume", lowValue = 0f, highValue = 1f });
            settingsScreen.Add(new Slider { name = "esc-menu__sounds-volume", lowValue = 0f, highValue = 1f });
            panel.Add(settingsScreen);

            var keyBindingsScreen = new VisualElement { name = "esc-menu__screen-keybindings" };
            keyBindingsScreen.Add(new Button { name = "esc-menu__keybindings-back" });
            panel.Add(keyBindingsScreen);

            return root;
        }

        private static void ResetConsumedEscapeFrame()
        {
            var field = typeof(PlayerCursorLockController).GetField(
                "_escapeConsumedFrame",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(null, -1);
        }

        private static void InvokeSingleFloatMethod(object target, string methodName, float value)
        {
            var method = target.GetType().GetMethod(methodName);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, new object[] { value });
        }

        private static float GetSingleFloatProperty(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName);
            Assert.That(property, Is.Not.Null, propertyName);
            return Convert.ToSingle(property.GetValue(target));
        }

        private static void InvokePrivateChangeEventHandler(object target, string methodName, float previousValue, float newValue)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);

            using var evt = ChangeEvent<float>.GetPooled(previousValue, newValue);
            method.Invoke(target, new object[] { evt });
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

        private sealed class TestCursorEscapeKeySource : IPlayerCursorEscapeKeySource
        {
            public bool PressedThisFrame;

            public bool WasEscapePressedThisFrame()
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
            public bool IsDevConsoleVisible { get; set; }
            public bool IsAnyMenuOpen => IsShopTradeMenuOpen || IsWorkbenchMenuVisible || IsTabInventoryVisible || IsEscMenuVisible || IsDevConsoleVisible;

            public event Action<bool> OnWorkbenchMenuVisibilityChanged;
            public event Action<bool> OnTabInventoryVisibilityChanged;
            public event Action<bool> OnEscMenuVisibilityChanged;
            public event Action<bool> OnDevConsoleVisibilityChanged;

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

            public void RaiseDevConsoleVisibilityChanged(bool isVisible)
            {
                IsDevConsoleVisible = isVisible;
                OnDevConsoleVisibilityChanged?.Invoke(isVisible);
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
            public float LastAppliedLookSensitivity { get; private set; } = 1f;
            public float LastAppliedAdsSensitivity { get; private set; } = 1f;
            public float GlobalVolume { get; private set; } = 1f;
            public float MusicVolume { get; private set; } = 1f;
            public float SoundsVolume { get; private set; } = 1f;

            public IReadOnlyList<EscMenuResolutionOption> GetAvailableResolutionOptions() => _resolutions;
            public EscMenuResolutionOption GetCurrentResolutionOption() => _resolutions[Mathf.Clamp(CurrentResolutionIndex, 0, _resolutions.Length - 1)];
            public float GetCurrentFov() => Fov;
            public float GetCurrentUserLookSensitivityMultiplier() => LastAppliedLookSensitivity;
            public float GetCurrentUserAdsSensitivityMultiplier() => LastAppliedAdsSensitivity;
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

            public void ApplyUserLookSensitivityMultiplier(float multiplier)
            {
                LastAppliedLookSensitivity = multiplier;
            }

            public void ApplyUserAdsSensitivityMultiplier(float multiplier)
            {
                LastAppliedAdsSensitivity = multiplier;
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
