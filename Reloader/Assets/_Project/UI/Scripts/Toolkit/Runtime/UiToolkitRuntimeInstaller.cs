using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Reloader.UI.Toolkit.Runtime
{
    public sealed class UiToolkitRuntimeInstaller : MonoBehaviour
    {
        private static readonly HashSet<string> LegacyRuntimePresenterTypes = new(StringComparer.Ordinal)
        {
            "Reloader.UI.BeltHudPresenter",
            "Reloader.UI.AmmoHudPresenter",
            "Reloader.UI.TabUiPresenter",
            "Reloader.UI.InventoryTooltipPresenter",
            "Reloader.Economy.TradeUiPresenter",
            "Reloader.Reloading.UI.ReloadingWorkbenchPresenter"
        };

        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private VisualTreeAsset _beltHudTree;
        [SerializeField] private VisualTreeAsset _ammoHudTree;
        [SerializeField] private VisualTreeAsset _tabInventoryTree;
        [SerializeField] private VisualTreeAsset _tradeTree;
        [SerializeField] private VisualTreeAsset _reloadingTree;

        [SerializeField] private bool _disableLegacyRuntimePresenters = true;

        private UiToolkitRuntimeRoot _runtimeRoot;

        public void ExecuteCutover()
        {
            _runtimeRoot = EnsureRuntimeRoot();
            EnsureScreenDocument("belt-hud", _beltHudTree);
            EnsureScreenDocument("ammo-hud", _ammoHudTree);
            EnsureScreenDocument("tab-inventory", _tabInventoryTree);
            EnsureScreenDocument("trade-ui", _tradeTree);
            EnsureScreenDocument("reloading-workbench", _reloadingTree);

            if (_disableLegacyRuntimePresenters)
            {
                DisableLegacyPresenters();
            }
        }

        public int ActiveLegacyPresenterCountForTests()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var count = 0;
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                var typeName = behaviour.GetType().FullName;
                if (typeName != null && LegacyRuntimePresenterTypes.Contains(typeName) && behaviour.enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private UiToolkitRuntimeRoot EnsureRuntimeRoot()
        {
            var existing = FindFirstObjectByType<UiToolkitRuntimeRoot>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            var rootGo = new GameObject("UiToolkitRuntimeRoot");
            return rootGo.AddComponent<UiToolkitRuntimeRoot>();
        }

        private void EnsureScreenDocument(string screenId, VisualTreeAsset treeAsset)
        {
            var rootTransform = _runtimeRoot.transform;
            var screenTransform = rootTransform.Find(screenId);
            UIDocument document;
            if (screenTransform == null)
            {
                var screenGo = new GameObject(screenId);
                screenGo.transform.SetParent(rootTransform, false);
                document = screenGo.AddComponent<UIDocument>();
            }
            else
            {
                document = screenTransform.GetComponent<UIDocument>();
                if (document == null)
                {
                    document = screenTransform.gameObject.AddComponent<UIDocument>();
                }
            }

            document.panelSettings = _panelSettings;
            if (treeAsset != null)
            {
                document.visualTreeAsset = treeAsset;
            }
        }

        private void DisableLegacyPresenters()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour == this)
                {
                    continue;
                }

                var typeName = behaviour.GetType().FullName;
                if (typeName != null && LegacyRuntimePresenterTypes.Contains(typeName))
                {
                    behaviour.enabled = false;
                }
            }
        }
    }
}
