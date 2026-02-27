using System;
using TMPro;
using Reloader.Core.Events;
using Reloader.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Reloader.UI
{
    public sealed class BeltHudPresenter : MonoBehaviour
    {
        [Serializable]
        public sealed class BeltSlotView
        {
            [SerializeField] private RectTransform _slotRoot;
            [SerializeField] private Image _slotFrameImage;
            [SerializeField] private Image _placeholderIconImage;
            [SerializeField] private TMP_Text _slotIndexText;

            public RectTransform SlotRoot => _slotRoot;
            public Image SlotFrameImage => _slotFrameImage;
            public Image PlaceholderIconImage => _placeholderIconImage;
            public TMP_Text SlotIndexText => _slotIndexText;

            public void SetReferences(RectTransform slotRoot, Image slotFrameImage, Image placeholderIconImage, TMP_Text slotIndexText)
            {
                _slotRoot = slotRoot;
                _slotFrameImage = slotFrameImage;
                _placeholderIconImage = placeholderIconImage;
                _slotIndexText = slotIndexText;
            }
        }

        [SerializeField] private BeltSlotView[] _slotViews = new BeltSlotView[PlayerInventoryRuntime.BeltSlotCount];
        [SerializeField] private Color _defaultFrameColor = Color.white;
        [SerializeField] private Color _selectedFrameColor = new Color(1f, 0.96f, 0.82f, 1f);
        [SerializeField] private Color _placeholderIconTint = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private bool _showPlaceholderIconWhenOccupied;
        [SerializeField, Min(1f)] private float _selectedScale = 1.08f;

        public PlayerInventoryController InventoryController { get; private set; }

        private void OnEnable()
        {
            GameEvents.OnInventoryChanged += HandleInventoryChanged;
            GameEvents.OnBeltSelectionChanged += HandleBeltSelectionChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnInventoryChanged -= HandleInventoryChanged;
            GameEvents.OnBeltSelectionChanged -= HandleBeltSelectionChanged;
        }

        public void SetInventoryController(PlayerInventoryController inventoryController)
        {
            InventoryController = inventoryController;
            Refresh();
        }

        public void ConfigureGeneratedSlotViews(
            RectTransform[] slotRoots,
            Image[] slotFrames,
            Image[] placeholderIcons,
            TMP_Text[] slotIndexTexts)
        {
            if (slotRoots == null || slotFrames == null || placeholderIcons == null || slotIndexTexts == null)
            {
                return;
            }

            var count = PlayerInventoryRuntime.BeltSlotCount;
            _slotViews = new BeltSlotView[count];
            for (var i = 0; i < count; i++)
            {
                _slotViews[i] = new BeltSlotView();
                _slotViews[i].SetReferences(
                    i < slotRoots.Length ? slotRoots[i] : null,
                    i < slotFrames.Length ? slotFrames[i] : null,
                    i < placeholderIcons.Length ? placeholderIcons[i] : null,
                    i < slotIndexTexts.Length ? slotIndexTexts[i] : null);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (!IsReady())
            {
                SetIdleVisualState();
                return;
            }

            var runtime = InventoryController.Runtime;
            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                var slotView = _slotViews[i];
                var itemId = runtime.BeltSlotItemIds[i];
                var isOccupied = !string.IsNullOrWhiteSpace(itemId);
                var isSelected = runtime.SelectedBeltIndex == i;

                if (slotView.PlaceholderIconImage != null)
                {
                    slotView.PlaceholderIconImage.color = _placeholderIconTint;
                    slotView.PlaceholderIconImage.enabled = _showPlaceholderIconWhenOccupied && isOccupied;
                }

                if (slotView.SlotFrameImage != null)
                {
                    slotView.SlotFrameImage.color = isSelected ? _selectedFrameColor : _defaultFrameColor;
                }

                if (slotView.SlotRoot != null)
                {
                    var scale = isSelected ? _selectedScale : 1f;
                    slotView.SlotRoot.localScale = Vector3.one * scale;
                }

                if (slotView.SlotIndexText != null)
                {
                    slotView.SlotIndexText.text = (i + 1).ToString();
                }
            }
        }

        private bool IsReady()
        {
            return InventoryController != null
                && InventoryController.Runtime != null
                && _slotViews != null
                && _slotViews.Length == PlayerInventoryRuntime.BeltSlotCount;
        }

        private void HandleInventoryChanged()
        {
            Refresh();
        }

        private void HandleBeltSelectionChanged(int _)
        {
            Refresh();
        }

        private void SetIdleVisualState()
        {
            if (_slotViews == null)
            {
                return;
            }

            for (var i = 0; i < _slotViews.Length; i++)
            {
                var slotView = _slotViews[i];
                if (slotView == null)
                {
                    continue;
                }

                if (slotView.PlaceholderIconImage != null)
                {
                    slotView.PlaceholderIconImage.color = _placeholderIconTint;
                    slotView.PlaceholderIconImage.enabled = false;
                }

                if (slotView.SlotFrameImage != null)
                {
                    slotView.SlotFrameImage.color = _defaultFrameColor;
                }

                if (slotView.SlotRoot != null)
                {
                    slotView.SlotRoot.localScale = Vector3.one;
                }

                if (slotView.SlotIndexText != null)
                {
                    slotView.SlotIndexText.text = (i + 1).ToString();
                }
            }
        }
    }
}
