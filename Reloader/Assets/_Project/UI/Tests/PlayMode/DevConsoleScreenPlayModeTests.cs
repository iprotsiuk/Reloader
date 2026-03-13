using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Runtime;
using Reloader.DevTools.Runtime;
using Reloader.Player;
using Reloader.UI.Toolkit.Runtime;
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
            using var runtime = new DevToolsRuntime();
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

            UnityEngine.Object.DestroyImmediate(consoleGo);
            UnityEngine.Object.DestroyImmediate(cursorLockGo);
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
            using var runtime = new DevToolsRuntime();
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

            UnityEngine.Object.DestroyImmediate(consoleGo);
            UnityEngine.Object.DestroyImmediate(cursorLockGo);
        }

        [UnityTest]
        public IEnumerator OpenConsole_ConsumesAutocompleteAndSuggestionNavigationInput()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            using var runtime = new DevToolsRuntime();
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

            UnityEngine.Object.DestroyImmediate(consoleGo);
        }

        [UnityTest]
        public IEnumerator OpenConsole_RendersSuggestionsAndAutocompleteAppliesHighlightedToken()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            using var runtime = new DevToolsRuntime();
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

            UnityEngine.Object.DestroyImmediate(consoleGo);
        }

        [UnityTest]
        public IEnumerator OpenConsole_EnterAcceptsHighlightedSuggestionWithoutExecuting()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var commandKeys = new TestConsoleKeySource();
            using var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.SetConsoleKeySource(commandKeys);
            controller.Configure(runtimeEvents);
            controller.SetViewBinder(binder);

            binder.SetCommandText("n");

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();

            var suggestionsRoot = binder.Root.Q<VisualElement>("dev-console__suggestions");
            var status = binder.Root.Q<Label>("dev-console__status");
            var commandField = binder.Root.Q<TextField>("dev-console__command");
            Assert.That(suggestionsRoot, Is.Not.Null);
            Assert.That(status, Is.Not.Null);
            Assert.That(commandField, Is.Not.Null);
            Assert.That(suggestionsRoot!.childCount, Is.GreaterThan(0));
            Assert.That(((Label)suggestionsRoot[0]).text, Does.Contain("noclip"));

            commandKeys.QueueSubmit();
            controller.Tick();

            Assert.That(binder.GetCommandText(), Is.EqualTo("noclip"));
            Assert.That(status!.text, Is.EqualTo("Developer tools active"));
            AssertCaretAtEndOfLine(commandField!, "noclip");

            commandKeys.QueueSubmit();
            controller.Tick();

            Assert.That(status.text, Does.Contain("Noclip enabled."));
            Assert.That(binder.GetCommandText(), Is.EqualTo("noclip"));
            AssertCaretAtEndOfLine(commandField, "noclip");

            UnityEngine.Object.DestroyImmediate(consoleGo);
        }

        [UnityTest]
        public IEnumerator OpenConsole_EnterAcceptsHighlightedSuggestionThatAddsArgumentsWithoutExecuting()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var commandKeys = new TestConsoleKeySource();
            using var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.SetConsoleKeySource(commandKeys);
            controller.Configure(runtimeEvents);
            controller.SetViewBinder(binder);

            binder.SetCommandText("give");

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();

            var suggestionsRoot = binder.Root.Q<VisualElement>("dev-console__suggestions");
            var status = binder.Root.Q<Label>("dev-console__status");
            var commandField = binder.Root.Q<TextField>("dev-console__command");
            Assert.That(suggestionsRoot, Is.Not.Null);
            Assert.That(status, Is.Not.Null);
            Assert.That(commandField, Is.Not.Null);
            Assert.That(suggestionsRoot!.childCount, Is.GreaterThan(0));
            Assert.That(((Label)suggestionsRoot[0]).text, Is.EqualTo("item"));

            commandKeys.QueueSubmit();
            controller.Tick();

            Assert.That(binder.GetCommandText(), Is.EqualTo("give item"));
            Assert.That(status!.text, Is.EqualTo("Developer tools active"));

            UnityEngine.Object.DestroyImmediate(consoleGo);
        }

        [UnityTest]
        public IEnumerator OpenConsole_RendersReadableDarkTextAcrossConsoleSurface()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            using var runtime = new DevToolsRuntime();
            var binder = BuildBinder();
            controller.SetRuntime(runtime);
            controller.SetInputSource(input);
            controller.Configure(runtimeEvents);
            controller.SetViewBinder(binder);

            var prompt = binder.Root.Q<Label>("dev-console__prompt");
            var command = binder.Root.Q<TextField>("dev-console__command");
            var suggestions = binder.Root.Q<VisualElement>("dev-console__suggestions");
            var status = binder.Root.Q<Label>("dev-console__status");
            Assert.That(prompt, Is.Not.Null);
            Assert.That(command, Is.Not.Null);
            Assert.That(suggestions, Is.Not.Null);
            Assert.That(status, Is.Not.Null);

            var unreadableColor = new Color(0.9f, 1f, 0.95f, 1f);
            prompt!.style.color = unreadableColor;
            command!.style.color = unreadableColor;
            status!.style.color = unreadableColor;
            binder.SetCommandText("n");

            yield return null;

            input.QueueDevConsoleToggle();
            controller.Tick();

            Assert.That(prompt.style.color.value, Is.EqualTo(Color.black));
            Assert.That(command.style.color.value, Is.EqualTo(Color.black));
            Assert.That(status.style.color.value, Is.EqualTo(Color.black));
            Assert.That(suggestions.childCount, Is.GreaterThan(0));
            Assert.That(((Label)suggestions[0]).style.color.value, Is.EqualTo(Color.black));

            UnityEngine.Object.DestroyImmediate(consoleGo);
        }

        [Test]
        public void OpenConsole_FirstBackquoteKeyDown_IsSuppressed()
        {
            var binder = BuildBinder();
            var keyHandler = typeof(DevConsoleViewBinder).GetMethod("HandleCommandFieldKeyDown", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(keyHandler, Is.Not.Null);

            binder.Render(new DevConsoleUiState(
                isVisible: true,
                promptText: ">",
                inputText: string.Empty,
                statusText: string.Empty,
                suggestions: Array.Empty<DevConsoleSuggestion>(),
                highlightedSuggestionIndex: 0));

            binder.SetCommandText("`");
            using var keyDown = KeyDownEvent.GetPooled('`', KeyCode.BackQuote, EventModifiers.None);
            keyHandler!.Invoke(binder, new object[] { keyDown });

            Assert.That(binder.GetCommandText(), Is.EqualTo(string.Empty));
        }

        [UnityTest]
        public IEnumerator OpenConsole_SubmitAndCancelUseInjectedConsoleKeySource()
        {
            var runtimeEvents = new DefaultRuntimeEvents();
            var consoleGo = new GameObject("DevConsole");
            var controller = consoleGo.AddComponent<DevConsoleController>();
            var input = new TestInputSource();
            var commandKeys = new TestConsoleKeySource();
            using var runtime = new DevToolsRuntime();
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
            Assert.That(status.text, Does.Contain("Noclip enabled."));

            commandKeys.QueueCancel();
            controller.Tick();

            Assert.That(runtime.IsConsoleVisible, Is.False);
            Assert.That(runtimeEvents.IsDevConsoleVisible, Is.False);

            UnityEngine.Object.DestroyImmediate(consoleGo);
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
            using var runtime = new DevToolsRuntime();
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

            UnityEngine.Object.DestroyImmediate(consoleGo);
            UnityEngine.Object.DestroyImmediate(cursorLockGo);
            RuntimeKernelBootstrapper.Events = previousEvents;
        }

        [Test]
        public void BridgeBindDevConsole_ReusesSingleTraceRuntime_AndCleansUpOnDestroy()
        {
            var initialCount = CountTraceRuntimeRoots();
            var bridgeGo = new GameObject("UiBridge");

            try
            {
                var bridge = bridgeGo.AddComponent<UiToolkitScreenRuntimeBridge>();
                var bindMethod = typeof(UiToolkitScreenRuntimeBridge).GetMethod("BindDevConsole", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bindMethod, Is.Not.Null);

                bindMethod!.Invoke(bridge, new object[] { BuildBinder().Root, "DevConsoleController", new TestInputSource() });

                Assert.That(CountTraceRuntimeRoots(), Is.EqualTo(initialCount));

                UnityEngine.Object.DestroyImmediate(bridgeGo);
                bridgeGo = null;

                Assert.That(CountTraceRuntimeRoots(), Is.EqualTo(initialCount));
            }
            finally
            {
                if (bridgeGo != null)
                {
                    UnityEngine.Object.DestroyImmediate(bridgeGo);
                }

                DestroyAllTraceRuntimeRoots();
            }
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

        private static int CountTraceRuntimeRoots()
        {
            var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            var count = 0;
            for (var i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i] != null && gameObjects[i].name == "DevTraceRuntime")
                {
                    count++;
                }
            }

            return count;
        }

        private static void DestroyAllTraceRuntimeRoots()
        {
            var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (var i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i] != null && gameObjects[i].name == "DevTraceRuntime")
                {
                    UnityEngine.Object.DestroyImmediate(gameObjects[i]);
                }
            }
        }

        private static void AssertCaretAtEndOfLine(TextField field, string text)
        {
            var expectedIndex = text.Length;
            Assert.That(GetTextFieldIntProperty(field, "cursorIndex"), Is.EqualTo(expectedIndex));
            Assert.That(GetTextFieldIntProperty(field, "selectIndex"), Is.EqualTo(expectedIndex));
        }

        private static int GetTextFieldIntProperty(TextField field, string propertyName)
        {
            var property = typeof(TextField).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected TextField to expose '{propertyName}'.");
            Assert.That(property!.GetValue(field), Is.TypeOf<int>(), $"Expected '{propertyName}' to be an int.");
            return (int)property.GetValue(field);
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
