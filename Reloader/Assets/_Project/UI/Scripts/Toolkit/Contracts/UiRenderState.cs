using System;

namespace Reloader.UI.Toolkit.Contracts
{
    public class UiRenderState
    {
        public UiRenderState(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                throw new ArgumentException("Screen id is required.", nameof(screenId));
            }

            ScreenId = screenId;
        }

        public string ScreenId { get; }
    }
}
