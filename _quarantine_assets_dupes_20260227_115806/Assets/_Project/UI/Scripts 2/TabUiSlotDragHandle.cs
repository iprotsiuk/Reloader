using Reloader.Core.Events;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Reloader.UI
{
    public sealed class TabUiSlotDragHandle : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private TabUiPresenter _presenter;
        [SerializeField] private InventoryArea _area;
        [SerializeField] private int _index;

        public void Configure(TabUiPresenter presenter, InventoryArea area, int index)
        {
            _presenter = presenter;
            _area = area;
            _index = index;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _presenter?.TryBeginDrag(_area, _index);
        }

        public void OnDrop(PointerEventData eventData)
        {
            _presenter?.TryDropDraggedItem(_area, _index);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _presenter?.EndDrag();
        }
    }
}
