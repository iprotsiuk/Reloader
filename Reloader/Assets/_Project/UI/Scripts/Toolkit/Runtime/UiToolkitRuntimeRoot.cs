using UnityEngine;

namespace Reloader.UI.Toolkit.Runtime
{
    public sealed class UiToolkitRuntimeRoot : MonoBehaviour
    {
        private readonly UiScreenRegistry _screenRegistry = new();
        private readonly UiScreenCompositionConfig _compositionConfig = new();
        private readonly UiActionMapConfig _actionMapConfig = new();

        public UiScreenRegistry ScreenRegistry => _screenRegistry;
        public UiScreenCompositionConfig CompositionConfig => _compositionConfig;
        public UiActionMapConfig ActionMapConfig => _actionMapConfig;
    }
}
