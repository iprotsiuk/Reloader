using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Startup.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public sealed class StartupMenuUiInteropPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            DestroyOwnersOfType<StartupMenuController>();
            DestroyOwnersOfType<EventSystem>();
            DestroyOwnersOfType<PanelRaycaster>();
        }

        [UnityTest]
        public IEnumerator StartupMenuController_WithEventSystem_CreatesPanelRaycaster()
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            var menuGo = new GameObject("StartupMenu");
            menuGo.SetActive(false);

            var controller = menuGo.AddComponent<StartupMenuController>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            SetPrivateField(controller, "_panelSettings", panelSettings);

            menuGo.SetActive(true);

            yield return null;
            yield return null;

            Assert.That(
                Object.FindFirstObjectByType<PanelRaycaster>(FindObjectsInactive.Include),
                Is.Not.Null,
                "Expected startup menu UI Toolkit document to create a PanelRaycaster bridge under the active EventSystem so front-door buttons can receive clicks.");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private static void DestroyOwnersOfType<T>() where T : Component
        {
            var components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && component.gameObject != null)
                {
                    Object.DestroyImmediate(component.gameObject);
                }
            }
        }
    }
}
