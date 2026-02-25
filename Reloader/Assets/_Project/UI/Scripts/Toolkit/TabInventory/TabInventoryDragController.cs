using System;
using Reloader.UI.Toolkit.Contracts;

namespace Reloader.UI.Toolkit.TabInventory
{
    public sealed class TabInventoryDragController
    {
        private string _sourceContainer;
        private int _sourceIndex = -1;
        private string _sourceItemId;

        public event Action<UiIntent> IntentRaised;

        public void BeginDrag(string sourceContainer, int sourceIndex, string sourceItemId)
        {
            _sourceContainer = sourceContainer;
            _sourceIndex = sourceIndex;
            _sourceItemId = sourceItemId;
        }

        public bool TryDrop(string targetContainer, int targetIndex, string targetItemId)
        {
            if (_sourceIndex < 0)
            {
                return false;
            }

            var intentKey = !string.IsNullOrWhiteSpace(_sourceItemId) && _sourceItemId == targetItemId
                ? "inventory.drag.merge"
                : "inventory.drag.swap";

            var payload = new DragIntentPayload(_sourceContainer, _sourceIndex, targetContainer, targetIndex);
            IntentRaised?.Invoke(new UiIntent(intentKey, payload));
            _sourceContainer = null;
            _sourceIndex = -1;
            _sourceItemId = null;
            return true;
        }

        public readonly struct DragIntentPayload
        {
            public DragIntentPayload(string sourceContainer, int sourceIndex, string targetContainer, int targetIndex)
            {
                SourceContainer = sourceContainer;
                SourceIndex = sourceIndex;
                TargetContainer = targetContainer;
                TargetIndex = targetIndex;
            }

            public string SourceContainer { get; }
            public int SourceIndex { get; }
            public string TargetContainer { get; }
            public int TargetIndex { get; }
        }
    }
}
