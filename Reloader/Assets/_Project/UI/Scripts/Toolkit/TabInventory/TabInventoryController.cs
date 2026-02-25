using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryController : MonoBehaviour, IUiController
    {
        private TabInventoryViewBinder _viewBinder;
        private TabInventoryDragController _dragController;

        public void Configure(TabInventoryViewBinder viewBinder, TabInventoryDragController dragController)
        {
            _viewBinder = viewBinder;
            _dragController = dragController;
            if (_dragController != null)
            {
                _dragController.IntentRaised += HandleIntent;
            }
        }

        public void HandleIntent(UiIntent intent)
        {
        }

        private void OnDisable()
        {
            if (_dragController != null)
            {
                _dragController.IntentRaised -= HandleIntent;
            }
        }
    }
}
