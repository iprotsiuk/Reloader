using Reloader.UI.Toolkit.Contracts;
using UnityEngine;

namespace Reloader.UI.Toolkit.Trade
{
    public sealed class TradeController : MonoBehaviour, IUiController
    {
        private TradeViewBinder _viewBinder;

        public void SetViewBinder(TradeViewBinder binder)
        {
            _viewBinder = binder;
        }

        public void HandleIntent(UiIntent intent)
        {
        }

        public void Render(TradeUiState state)
        {
            _viewBinder?.Render(state);
        }
    }
}
