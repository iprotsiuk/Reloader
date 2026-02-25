using System;

namespace Reloader.UI.Toolkit.Contracts
{
    public interface IUiViewBinder
    {
        event Action<UiIntent> IntentRaised;

        void Render(UiRenderState state);
    }
}
