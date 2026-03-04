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
        [SerializeField] private VisualTreeAsset _escMenuTree;
        [SerializeField] private VisualTreeAsset _chestInventoryTree;
        [SerializeField] private VisualTreeAsset _tradeTree;
        [SerializeField] private VisualTreeAsset _reloadingTree;
        [SerializeField] private VisualTreeAsset _interactionHintTree;

        [SerializeField] private bool _disableLegacyRuntimePresenters = true;

        private UiToolkitRuntimeRoot _runtimeRoot;

        public void ExecuteCutover()
        {
            ResolveDefaultReferencesIfNeeded();
            _runtimeRoot = EnsureRuntimeRoot();
            EnsureScreenDocument(UiRuntimeCompositionIds.ScreenIds.BeltHud, _beltHudTree);
            EnsureScreenDocument(UiRuntimeCompositionIds.ScreenIds.AmmoHud, _ammoHudTree);
            EnsureScreenDocument(UiRuntimeCompositionIds.ScreenIds.TabInventory, _tabInventoryTree);
            EnsureScreenDocument(UiRuntimeCompositionIds.ScreenIds.EscMenu, _escMenuTree);
            EnsureScreenDocument(UiRuntimeCompositionIds.ScreenIds.ChestInventory, _chestInventoryTree);
            EnsureScreenDocument(UiRuntimeCompositionIds.ScreenIds.Trade, _tradeTree);
            EnsureScreenDocument(UiRuntimeCompositionIds.ScreenIds.ReloadingWorkbench, _reloadingTree);
            EnsureScreenDocument(UiRuntimeCompositionIds.ScreenIds.InteractionHint, _interactionHintTree);

            if (_disableLegacyRuntimePresenters)
            {
                DisableLegacyPresenters();
            }

            EnsureRuntimeBridge();
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

        private void ResolveDefaultReferencesIfNeeded()
        {
#if UNITY_EDITOR
            _beltHudTree ??= LoadVisualTreeAssetAtPath("Assets/_Project/UI/Toolkit/UXML/BeltHud.uxml");
            _ammoHudTree ??= LoadVisualTreeAssetAtPath("Assets/_Project/UI/Toolkit/UXML/AmmoHud.uxml");
            _tabInventoryTree ??= LoadVisualTreeAssetAtPath("Assets/_Project/UI/Toolkit/UXML/TabInventory.uxml");
            _escMenuTree ??= LoadVisualTreeAssetAtPath("Assets/_Project/UI/Toolkit/UXML/EscMenu.uxml");
            _chestInventoryTree ??= LoadVisualTreeAssetAtPath("Assets/_Project/UI/Toolkit/UXML/ChestInventory.uxml");
            _tradeTree ??= LoadVisualTreeAssetAtPath("Assets/_Project/UI/Toolkit/UXML/TradeUi.uxml");
            _reloadingTree ??= LoadVisualTreeAssetAtPath("Assets/_Project/UI/Toolkit/UXML/ReloadingWorkbench.uxml");
            _interactionHintTree ??= LoadVisualTreeAssetAtPath("Assets/_Project/UI/Toolkit/UXML/InteractionHint.uxml");
#endif
        }

#if UNITY_EDITOR
        private static VisualTreeAsset LoadVisualTreeAssetAtPath(string path)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }
#endif

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

            if (_panelSettings != null)
            {
                document.panelSettings = _panelSettings;
            }

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

        private void EnsureRuntimeBridge()
        {
            if (_runtimeRoot == null)
            {
                return;
            }

            var bridge = _runtimeRoot.GetComponent<UiToolkitScreenRuntimeBridge>();
            if (bridge == null)
            {
                bridge = _runtimeRoot.gameObject.AddComponent<UiToolkitScreenRuntimeBridge>();
            }

            bridge.Initialize(_runtimeRoot);
        }
    }
}
