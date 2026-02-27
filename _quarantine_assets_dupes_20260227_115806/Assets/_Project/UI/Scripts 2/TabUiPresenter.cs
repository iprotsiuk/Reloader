using System;
using Reloader.Core.Events;
using Reloader.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Reloader.UI
{
    public sealed class TabUiPresenter : MonoBehaviour
    {
        [Serializable]
        public sealed class TabSlotView
        {
            [SerializeField] private RectTransform _slotRoot;
            [SerializeField] private Image _slotFrameImage;
            [SerializeField] private Image _slotIconImage;

            public RectTransform SlotRoot => _slotRoot;
            public Image SlotFrameImage => _slotFrameImage;
            public Image SlotIconImage => _slotIconImage;

            public void SetReferences(RectTransform slotRoot, Image slotFrameImage, Image slotIconImage)
            {
                _slotRoot = slotRoot;
                _slotFrameImage = slotFrameImage;
                _slotIconImage = slotIconImage;
            }
        }

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private PlayerInventoryController _inventoryController;
        [SerializeField] private TabSlotView[] _beltSlotViews = new TabSlotView[PlayerInventoryRuntime.BeltSlotCount];
        [SerializeField] private TabSlotView[] _backpackSlotViews = new TabSlotView[24];
        [SerializeField] private Behaviour[] _disableBehavioursWhileOpen;
        [SerializeField] private Color _defaultFrameColor = Color.white;
        [SerializeField] private Color _occupiedFrameColor = new Color(0.85f, 0.9f, 1f, 1f);
        [SerializeField] private Color _selectedBeltFrameColor = new Color(1f, 0.96f, 0.82f, 1f);
        [SerializeField] private Color _iconTint = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private bool _toggleWithTab = true;
        [SerializeField] private bool _closeWithEscape = true;

        private bool[] _disabledBehavioursPreviousState;
        private bool _isOpen;
        private bool _dragActive;
        private InventoryArea _dragSourceArea;
        private int _dragSourceIndex;
        private CursorLockMode _cursorLockBeforeOpen;
        private bool _cursorVisibleBeforeOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            ResolveInventoryController();
            AutoResolveGameplayBehaviours();
            ApplyPanelVisibility();
            Refresh();
        }

        private void OnEnable()
        {
            ResolveInventoryController();
            GameEvents.OnInventoryChanged += HandleInventoryChanged;
            GameEvents.OnBeltSelectionChanged += HandleBeltSelectionChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryChanged -= HandleInventoryChanged;
            GameEvents.OnBeltSelectionChanged -= HandleBeltSelectionChanged;

            if (_isOpen)
            {
                SetOpen(false);
            }

            EndDrag();
        }

        private void Update()
        {
            ResolveInventoryController();

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (_toggleWithTab && keyboard.tabKey.wasPressedThisFrame)
            {
                SetOpen(!_isOpen);
                return;
            }

            if (_isOpen && _closeWithEscape && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetOpen(false);
            }
        }

        public void SetInventoryController(PlayerInventoryController inventoryController)
        {
            _inventoryController = inventoryController;
            Refresh();
        }

        public void ConfigureGeneratedSlotViews(
            GameObject panelRoot,
            RectTransform[] beltRoots,
            Image[] beltFrames,
            Image[] beltIcons,
            RectTransform[] backpackRoots,
            Image[] backpackFrames,
            Image[] backpackIcons)
        {
            _panelRoot = panelRoot;
            _beltSlotViews = BuildViews(beltRoots, beltFrames, beltIcons);
            _backpackSlotViews = BuildViews(backpackRoots, backpackFrames, backpackIcons);
            ApplyPanelVisibility();
            Refresh();
        }

        public void SetOpen(bool open)
        {
            if (_isOpen == open)
            {
                return;
            }

            _isOpen = open;

            if (_isOpen)
            {
                _cursorLockBeforeOpen = Cursor.lockState;
                _cursorVisibleBeforeOpen = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ApplyGameplayBehaviourState(true);
                EndDrag();
            }
            else
            {
                ApplyGameplayBehaviourState(false);
                Cursor.lockState = _cursorLockBeforeOpen;
                Cursor.visible = _cursorVisibleBeforeOpen;
                EndDrag();
            }

            ApplyPanelVisibility();
            Refresh();
        }

        public bool TryBeginDrag(InventoryArea sourceArea, int sourceIndex)
        {
            if (!_isOpen || !TryGetItemId(sourceArea, sourceIndex, out _))
            {
                return false;
            }

            _dragActive = true;
            _dragSourceArea = sourceArea;
            _dragSourceIndex = sourceIndex;
            return true;
        }

        public bool TryDropDraggedItem(InventoryArea targetArea, int targetIndex)
        {
            if (!_dragActive || _inventoryController == null)
            {
                return false;
            }

            var moved = _inventoryController.TryMoveItem(_dragSourceArea, _dragSourceIndex, targetArea, targetIndex);
            if (moved)
            {
                Refresh();
            }

            return moved;
        }

        public void EndDrag()
        {
            _dragActive = false;
            _dragSourceArea = InventoryArea.Belt;
            _dragSourceIndex = -1;
        }

        public void Refresh()
        {
            ApplyPanelVisibility();

            if (!IsReady())
            {
                SetIdleVisualState();
                return;
            }

            var runtime = _inventoryController.Runtime;
            for (var i = 0; i < _beltSlotViews.Length; i++)
            {
                var isSelected = runtime.SelectedBeltIndex == i;
                var itemId = i >= 0 && i < runtime.BeltSlotItemIds.Length ? runtime.BeltSlotItemIds[i] : null;
                ApplySlotVisual(_beltSlotViews[i], itemId, isSelected);
            }

            for (var i = 0; i < _backpackSlotViews.Length; i++)
            {
                var itemId = i >= 0 && i < runtime.BackpackItemIds.Count ? runtime.BackpackItemIds[i] : null;
                ApplySlotVisual(_backpackSlotViews[i], itemId, false);
            }
        }

        private bool IsReady()
        {
            return _inventoryController != null
                && _inventoryController.Runtime != null
                && _beltSlotViews != null
                && _beltSlotViews.Length == PlayerInventoryRuntime.BeltSlotCount
                && _backpackSlotViews != null;
        }

        private void ApplyPanelVisibility()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(_isOpen);
            }
        }

        private void ApplyGameplayBehaviourState(bool opening)
        {
            if (_disableBehavioursWhileOpen == null)
            {
                return;
            }

            if (opening)
            {
                _disabledBehavioursPreviousState = new bool[_disableBehavioursWhileOpen.Length];
                for (var i = 0; i < _disableBehavioursWhileOpen.Length; i++)
                {
                    var behaviour = _disableBehavioursWhileOpen[i];
                    if (behaviour == null)
                    {
                        continue;
                    }

                    _disabledBehavioursPreviousState[i] = behaviour.enabled;
                    behaviour.enabled = false;
                }

                return;
            }

            if (_disabledBehavioursPreviousState == null)
            {
                return;
            }

            for (var i = 0; i < _disableBehavioursWhileOpen.Length && i < _disabledBehavioursPreviousState.Length; i++)
            {
                var behaviour = _disableBehavioursWhileOpen[i];
                if (behaviour == null)
                {
                    continue;
                }

                behaviour.enabled = _disabledBehavioursPreviousState[i];
            }
        }

        private void AutoResolveGameplayBehaviours()
        {
            if (_disableBehavioursWhileOpen != null && _disableBehavioursWhileOpen.Length > 0)
            {
                return;
            }

            var allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var resolved = new System.Collections.Generic.List<Behaviour>();
            for (var i = 0; i < allBehaviours.Length; i++)
            {
                var behaviour = allBehaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                var name = behaviour.GetType().Name;
                if (name == "PlayerLookController" || name == "PlayerCursorLockController")
                {
                    resolved.Add(behaviour);
                }
            }

            _disableBehavioursWhileOpen = resolved.ToArray();
        }

        private void ApplySlotVisual(TabSlotView slotView, string itemId, bool isSelectedBeltSlot)
        {
            if (slotView == null)
            {
                return;
            }

            var occupied = !string.IsNullOrWhiteSpace(itemId);
            if (slotView.SlotFrameImage != null)
            {
                if (isSelectedBeltSlot)
                {
                    slotView.SlotFrameImage.color = _selectedBeltFrameColor;
                }
                else
                {
                    slotView.SlotFrameImage.color = occupied ? _occupiedFrameColor : _defaultFrameColor;
                }
            }

            if (slotView.SlotIconImage != null)
            {
                slotView.SlotIconImage.color = _iconTint;
                slotView.SlotIconImage.enabled = occupied;
            }
        }

        private void SetIdleVisualState()
        {
            if (_beltSlotViews != null)
            {
                for (var i = 0; i < _beltSlotViews.Length; i++)
                {
                    ApplySlotVisual(_beltSlotViews[i], null, false);
                }
            }

            if (_backpackSlotViews != null)
            {
                for (var i = 0; i < _backpackSlotViews.Length; i++)
                {
                    ApplySlotVisual(_backpackSlotViews[i], null, false);
                }
            }
        }

        private bool TryGetItemId(InventoryArea area, int index, out string itemId)
        {
            itemId = null;
            if (!IsReady())
            {
                return false;
            }

            var runtime = _inventoryController.Runtime;
            if (area == InventoryArea.Belt)
            {
                if (index < 0 || index >= runtime.BeltSlotItemIds.Length)
                {
                    return false;
                }

                itemId = runtime.BeltSlotItemIds[index];
                return !string.IsNullOrWhiteSpace(itemId);
            }

            if (area == InventoryArea.Backpack)
            {
                if (index < 0 || index >= runtime.BackpackItemIds.Count)
                {
                    return false;
                }

                itemId = runtime.BackpackItemIds[index];
                return !string.IsNullOrWhiteSpace(itemId);
            }

            return false;
        }

        private static TabSlotView[] BuildViews(RectTransform[] roots, Image[] frames, Image[] icons)
        {
            if (roots == null || frames == null || icons == null)
            {
                return Array.Empty<TabSlotView>();
            }

            var count = Math.Max(roots.Length, Math.Max(frames.Length, icons.Length));
            var views = new TabSlotView[count];
            for (var i = 0; i < count; i++)
            {
                var view = new TabSlotView();
                view.SetReferences(
                    i < roots.Length ? roots[i] : null,
                    i < frames.Length ? frames[i] : null,
                    i < icons.Length ? icons[i] : null);
                views[i] = view;
            }

            return views;
        }

        private void ResolveInventoryController()
        {
            if (_inventoryController != null)
            {
                return;
            }

            _inventoryController = FindAnyObjectByType<PlayerInventoryController>();
        }

        private void HandleInventoryChanged()
        {
            Refresh();
        }

        private void HandleBeltSelectionChanged(int _)
        {
            Refresh();
        }
    }
}
