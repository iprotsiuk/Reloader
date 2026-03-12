using System.Collections;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.DevTools.Runtime;
using Reloader.Player;
using Reloader.UI.Toolkit.DevConsole;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public sealed class DevConsoleScreenPlayModeTests
    {
        [UnityTest]
        public IEnumerator OpeningConsole_RaisesUiStateAndBlocksGameplayInput()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var cursorLockGo = new GameObject("CursorLock");
            var cursorLockController = cursorLockGo.AddComponent<PlayerCursorLockController>();
            cursorLockController.Configure(runtimeEvents, runtimeEvents);

            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.Configure(runtimeEvents);
            controller.SetViewBinder(binder);

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();

            Assert.That(runtimeEvents.IsDevConsoleVisible, Is.True);
            Assert.That(runtime.IsConsoleVisible, Is.True);
            Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.True);
            Assert.That(PlayerCursorLockController.IsGameplayInputBlocked, Is.True);

            Object.DestroyImmediate(consoleGo);
            Object.DestroyImmediate(cursorLockGo);
        }

        [UnityTest]
        public IEnumerator ClosingConsole_ClearsUiStateAndRestoresGameplayInput()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var cursorLockGo = new GameObject("CursorLock");
            var cursorLockController = cursorLockGo.AddComponent<PlayerCursorLockController>();
            cursorLockController.Configure(runtimeEvents, runtimeEvents);

            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.Configure(runtimeEvents);
            controller.SetViewBinder(binder);

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();
            input.QueueDevConsoleToggle();
            controller.Tick();

            Assert.That(runtimeEvents.IsDevConsoleVisible, Is.False);
            Assert.That(runtime.IsConsoleVisible, Is.False);
            Assert.That(PlayerCursorLockController.IsAnyMenuOpen, Is.False);
            Assert.That(PlayerCursorLockController.IsGameplayInputBlocked, Is.False);

            Object.DestroyImmediate(consoleGo);
            Object.DestroyImmediate(cursorLockGo);
        }

        [UnityTest]
        public IEnumerator OpenConsole_ConsumesAutocompleteAndSuggestionNavigationInput()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.Configure(runtimeEvents);
            controller.SetViewBinder(binder);

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();

            input.QueueSuggestionDelta(-1);
            controller.Tick();

            input.QueueAutocomplete();
            controller.Tick();

            Assert.That(input.SuggestionDeltaConsumeCount, Is.EqualTo(1));
            Assert.That(input.AutocompleteConsumeCount, Is.EqualTo(1));

            Object.DestroyImmediate(consoleGo);
        }

        [UnityTest]
        public IEnumerator OpenConsole_RendersSuggestionsAndAutocompleteAppliesHighlightedToken()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.Configure(runtimeEvents);
            controller.SetViewBinder(binder);

            binder.SetCommandText("n");

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();

            var suggestionsRoot = binder.Root.Q<VisualElement>("dev-console__suggestions");
            Assert.That(suggestionsRoot.childCount, Is.GreaterThan(0));
            Assert.That(((Label)suggestionsRoot[0]).text, Does.Contain("noclip"));

            input.QueueAutocomplete();
            controller.Tick();

            Assert.That(binder.GetCommandText(), Is.EqualTo("noclip"));

            Object.DestroyImmediate(consoleGo);
        }

        [UnityTest]
        public IEnumerator OpenConsole_SubmitAndCancelUseInjectedConsoleKeySource()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var commandKeys = new TestConsoleKeySource();
            var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.SetConsoleKeySource(commandKeys);
            controller.Configure(runtimeEvents);
            controller.SetViewBinder(binder);

            binder.SetCommandText("noclip");

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();
            commandKeys.QueueSubmit();
            controller.Tick();

            var status = binder.Root.Q<Label>("dev-console__status");
            Assert.That(status.text, Does.Contain("Command 'noclip' is registered."));

            commandKeys.QueueCancel();
            controller.Tick();

            Assert.That(runtime.IsConsoleVisible, Is.False);
            Assert.That(runtimeEvents.IsDevConsoleVisible, Is.False);

            Object.DestroyImmediate(consoleGo);
        }

        [UnityTest]
        public IEnumerator RuntimeEventsReconfigured_RePublishesOpenConsoleVisibility()
        {
            var previousEvents = RuntimeKernelBootstrapper.Events;
            var initialEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = initialEvents;

            var cursorLockGo = new GameObject("CursorLock");
            var cursorLockController = cursorLockGo.AddComponent<PlayerCursorLockController>();
            cursorLockController.Configure(initialEvents, initialEvents);

            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.SetViewBinder(binder);

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();
            Assert.That(initialEvents.IsDevConsoleVisible, Is.True);

            var replacementEvents = new DefaultRuntimeEvents();
            RuntimeKernelBootstrapper.Events = replacementEvents;

            yield return null;

            Assert.That(replacementEvents.IsDevConsoleVisible, Is.True);
            Assert.That(PlayerCursorLockController.IsGameplayInputBlocked, Is.True);

            Object.DestroyImmediate(consoleGo);
            Object.DestroyImmediate(cursorLockGo);
            RuntimeKernelBootstrapper.Events = previousEvents;
        }

        private static DevConsoleViewBinder BuildBinder()
        {
            var screen = new VisualElement { name = "dev-console__screen" };
            var panel = new VisualElement { name = "dev-console__panel" };
            var prompt = new Label { name = "dev-console__prompt" };
            var command = new TextField { name = "dev-console__command" };
            var suggestions = new VisualElement { name = "dev-console__suggestions" };
            var status = new Label { name = "dev-console__status" };
            panel.Add(prompt);
            panel.Add(command);
            panel.Add(suggestions);
            panel.Add(status);
            screen.Add(panel);

            var binder = new DevConsoleViewBinder();
            binder.Initialize(screen);
            return binder;
        }

        private sealed class TestInputSource : IPlayerInputSource
        {
            private bool _devConsoleToggleQueued;
            private bool _autocompleteQueued;
            private int _suggestionDeltaQueued;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => Vector2.zero;
            public bool SprintHeld => false;
            public bool AimHeld => false;
            public int AutocompleteConsumeCount { get; private set; }
            public int SuggestionDeltaConsumeCount { get; private set; }

            public void QueueDevConsoleToggle()
            {
                _devConsoleToggleQueued = true;
            }

            public void QueueAutocomplete()
            {
                _autocompleteQueued = true;
            }

            public void QueueSuggestionDelta(int delta)
            {
                _suggestionDeltaQueued = delta;
            }

            public bool ConsumeJumpPressed() => false;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeFirePressed() => false;
            public bool ConsumeReloadPressed() => false;
            public bool ConsumePickupPressed() => false;
            public float ConsumeZoomInput() => 0f;
            public int ConsumeZeroAdjustStep() => 0;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;

            public bool ConsumeDevConsoleTogglePressed()
            {
                if (!_devConsoleToggleQueued)
                {
                    return false;
                }

                _devConsoleToggleQueued = false;
                return true;
            }

            public bool ConsumeAutocompletePressed()
            {
                if (!_autocompleteQueued)
                {
                    return false;
                }

                _autocompleteQueued = false;
                AutocompleteConsumeCount++;
                return true;
            }

            public int ConsumeSuggestionDelta()
            {
                if (_suggestionDeltaQueued == 0)
                {
                    return 0;
                }

                var delta = _suggestionDeltaQueued;
                _suggestionDeltaQueued = 0;
                SuggestionDeltaConsumeCount++;
                return delta;
            }
        }

        private sealed class TestConsoleKeySource : IDevConsoleKeySource
        {
            private bool _cancelQueued;
            private bool _submitQueued;

            public void QueueCancel()
            {
                _cancelQueued = true;
            }

            public void QueueSubmit()
            {
                _submitQueued = true;
            }

            public bool ConsumeCancelPressed()
            {
                if (!_cancelQueued)
                {
                    return false;
                }

                _cancelQueued = false;
                return true;
            }

            public bool ConsumeSubmitPressed()
            {
                if (!_submitQueued)
                {
                    return false;
                }

                _submitQueued = false;
                return true;
            }
        }
    }
}
