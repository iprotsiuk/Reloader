using System.Collections;
using NUnit.Framework;
using Reloader.UI;
using Reloader.UI.Toolkit.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Reloader.UI.Tests.PlayMode
{
    public class UiRuntimeCutoverPlayModeTests
    {
        [UnityTest]
        public IEnumerator ExecuteCutover_DisablesLegacyPresenters_AndCreatesToolkitDocuments()
        {
            var installerGo = new GameObject("Installer");
            var installer = installerGo.AddComponent<UiToolkitRuntimeInstaller>();

            var beltGo = new GameObject("LegacyBelt");
            var ammoGo = new GameObject("LegacyAmmo");
            var tabGo = new GameObject("LegacyTab");
            beltGo.AddComponent<Reloader.UI.BeltHudPresenter>();
            ammoGo.AddComponent<Reloader.UI.AmmoHudPresenter>();
            tabGo.AddComponent<Reloader.UI.TabUiPresenter>();

            var bootstrapGo = new GameObject("Bootstrap");
            var bootstrap = bootstrapGo.AddComponent<BeltHudBootstrap>();
            bootstrap.ExecuteCutover();

            yield return null;

            Assert.That(installer.ActiveLegacyPresenterCountForTests(), Is.EqualTo(0));

            var runtimeRoot = Object.FindFirstObjectByType<UiToolkitRuntimeRoot>(FindObjectsInactive.Include);
            Assert.That(runtimeRoot, Is.Not.Null);
            var bridge = runtimeRoot.GetComponent<UiToolkitScreenRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null);
            Assert.That(bridge.ActiveBindingsForTests(), Is.GreaterThan(0));

            bridge.enabled = false;
            Assert.That(bridge.ActiveBindingsForTests(), Is.EqualTo(0));

            bridge.enabled = true;
            Assert.That(bridge.ActiveBindingsForTests(), Is.GreaterThan(0));

            Assert.That(runtimeRoot.GetComponentsInChildren<UIDocument>(true).Length, Is.GreaterThanOrEqualTo(5));

            Object.DestroyImmediate(bootstrapGo);
            Object.DestroyImmediate(installerGo);
            Object.DestroyImmediate(beltGo);
            Object.DestroyImmediate(ammoGo);
            Object.DestroyImmediate(tabGo);
            if (runtimeRoot != null)
            {
                Object.DestroyImmediate(runtimeRoot.gameObject);
            }
        }
    }
}

namespace Reloader.UI
{
    // Legacy marker stubs for cutover verification after legacy presenter deletion.
    public sealed class BeltHudPresenter : MonoBehaviour { }
    public sealed class AmmoHudPresenter : MonoBehaviour { }
    public sealed class TabUiPresenter : MonoBehaviour { }
}
